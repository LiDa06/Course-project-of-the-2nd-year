using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Balda.Features.Game.Bot;
using Balda.Features.Game.Domain;
using Balda.Features.Game.Rules;
using Balda.Features.Game.SaveLoad;
using Balda.Features.Game.Services;
using Balda.Features.Game.UI;
using Balda.Infrastructure.LocalStorage;
using UnityEngine;

namespace Balda.Features.Game.Flow
{
    public class GameController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BoardManager boardManager;
        [SerializeField] private LetterInputPopup letterInputPopup;

        [Header("Start flow")]
        [SerializeField] private bool askAboutSavedGameBeforeStart = true;

        [Header("Bot")]
        [SerializeField] private float easyBotTurnDelay = 0.7f;
        [SerializeField] private float botLetterPreviewDelay = 0.18f;
        [SerializeField] private float botWordPreviewDelay = 0.45f;

        private GameSession currentSession;
        private TurnDraft currentDraft;
        private Coroutine botTurnRoutine;
        private int historyViewIndex = -1;

        private GameRules gameRules;
        private ScoreCalculator scoreCalculator;
        private WordDictionaryService wordDictionaryService;
        private StartWordProvider startWordProvider;
        private WordValidationService wordValidationService;
        private GameEndService gameEndService;
        private LocalMatchStatsService localMatchStatsService;
        private IBotMoveProvider easyBotMoveProvider;

        public GameSession CurrentSession => currentSession;
        public bool HasActiveDraft => currentDraft != null && currentDraft.IsActive;
        public string CurrentCandidateWord => currentDraft != null ? currentDraft.CandidateWord : "";

        public Action<bool> DraftStateChanged;
        public Action<string> CandidateWordChanged;
        public Action SavedGameChoiceRequested;
        public Action<GameSession> SessionChanged;
        public Action<GameSession> GameFinished;
        public Action<GameMoveRecord, int, int> HistoryEntryChanged;
        public Action HistoryViewClosed;

        private void Awake()
        {
            gameRules = new GameRules();
            scoreCalculator = new ScoreCalculator();
            wordDictionaryService = new WordDictionaryService();
            startWordProvider = new StartWordProvider(wordDictionaryService);
            wordValidationService = new WordValidationService(wordDictionaryService);
            gameEndService = new GameEndService();
            localMatchStatsService = new LocalMatchStatsService();
            easyBotMoveProvider = new EasyBotMoveProvider(wordDictionaryService);

            currentDraft = new TurnDraft();
        }

        private void OnEnable()
        {
            if (boardManager == null)
            {
                Debug.LogError("GameController: BoardManager reference is missing.");
                return;
            }

            boardManager.CellClicked -= OnBoardCellClicked;
            boardManager.CellClicked += OnBoardCellClicked;
        }

        private void OnDisable()
        {
            if (boardManager != null)
                boardManager.CellClicked -= OnBoardCellClicked;

            StopBotTurnRoutine();
            SaveNow();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                SaveNow();
        }

        private void OnApplicationQuit()
        {
            SaveNow();
        }

        public void InitializeForScreen()
        {
            StopBotTurnRoutine();
            currentDraft.Clear();
            CloseHistoryViewInternal(notify: true, restoreBoard: false);

            if (askAboutSavedGameBeforeStart && GameSaveService.HasSave())
            {
                SavedGameChoiceRequested?.Invoke();
                return;
            }

            LoadOrStartGame();
        }

        public void ContinueSavedGame()
        {
            if (TryLoadSavedGame())
                return;

            Debug.LogWarning("Сохранённая игра не найдена или повреждена. Начинаю новую.");
            StartFreshGame();
        }

        public void LoadOrStartGame()
        {
            if (TryLoadSavedGame())
                return;

            StartFreshGame();
        }

        public void StartFreshGame()
        {
            StopBotTurnRoutine();
            GameSaveService.DeleteSave();
            CloseHistoryViewInternal(notify: true, restoreBoard: false);

            int boardSize = GetBoardSizeFromSettings();

            currentSession = CreateNewSession(boardSize);
            currentDraft.Clear();

            RenderCurrentSession();
            NotifyDraftStateChanged();
            NotifyCandidateWordChanged();
            NotifySessionChanged();
            SaveNow();
            TryStartBotTurnIfNeeded();
        }

        public void SaveNow()
        {
            if (currentSession == null)
                return;

            if (currentSession.IsFinished)
            {
                GameSaveService.DeleteSave();
                return;
            }

            if (currentDraft != null && currentDraft.IsActive)
            {
                RevertDraftInternal();

                if (boardManager != null && currentSession.Board != null)
                    boardManager.Render(currentSession.Board);

                UpdateSelectionVisuals();
                NotifyDraftStateChanged();
                NotifyCandidateWordChanged();
                NotifySessionChanged();
            }

            LocalGameSave save = GameSessionSaveMapper.ToSave(currentSession);
            GameSaveService.Save(save);
        }

        public bool TrySubmitCurrentSelection()
        {
            if (currentSession == null || currentSession.IsFinished || currentSession.Phase == GamePhase.Finished || currentSession.Phase == GamePhase.BotTurn)
                return false;

            if (currentDraft == null || !currentDraft.IsActive)
            {
                Debug.LogWarning("Сначала поставьте новую букву.");
                return false;
            }

            if (currentDraft.SelectedPath == null || currentDraft.SelectedPath.Count == 0)
            {
                Debug.LogWarning("Проведи пальцем по буквам, чтобы собрать слово.");
                return false;
            }

            if (!currentDraft.ContainsPosition(currentDraft.Row, currentDraft.Col))
            {
                Debug.LogWarning("Слово должно проходить через новую букву.");
                return false;
            }

            var validation = wordValidationService.ValidateBasic(
                currentSession,
                currentDraft,
                currentDraft.CandidateWord);

            if (!validation.IsValid)
            {
                Debug.LogWarning(validation.Message);
                return false;
            }

            ApplyConfirmedWord(validation.NormalizedWord);
            return true;
        }

        public void CancelCurrentDraft()
        {
            if (currentSession == null || currentSession.IsFinished)
                return;

            if (currentDraft == null || !currentDraft.IsActive)
                return;

            RevertDraftInternal();

            if (boardManager != null && currentSession.Board != null)
                boardManager.Render(currentSession.Board);

            UpdateSelectionVisuals();
            NotifyDraftStateChanged();
            NotifyCandidateWordChanged();
            NotifySessionChanged();
        }

        public void BeginSelectionAt(int row, int col)
        {
            if (currentSession == null || currentSession.IsFinished || currentSession.Phase != GamePhase.BuildingWord)
                return;

            if (!CanUseCellForSelection(row, col))
                return;

            currentDraft.ClearSelection();
            currentDraft.SelectedPath.Add(new BoardPosition(row, col));

            RefreshCandidateWordFromSelection();
        }

        public void ContinueSelectionAt(int row, int col)
        {
            if (currentSession == null || currentSession.IsFinished || currentSession.Phase != GamePhase.BuildingWord)
                return;

            if (!CanUseCellForSelection(row, col))
                return;

            if (currentDraft.SelectedPath == null || currentDraft.SelectedPath.Count == 0)
            {
                BeginSelectionAt(row, col);
                return;
            }

            BoardPosition next = new BoardPosition(row, col);
            BoardPosition last = currentDraft.SelectedPath[currentDraft.SelectedPath.Count - 1];

            if (last.Equals(next))
                return;

            if (currentDraft.SelectedPath.Count > 1)
            {
                BoardPosition previous = currentDraft.SelectedPath[currentDraft.SelectedPath.Count - 2];
                if (previous.Equals(next))
                {
                    currentDraft.SelectedPath.RemoveAt(currentDraft.SelectedPath.Count - 1);
                    RefreshCandidateWordFromSelection();
                    return;
                }
            }

            if (currentDraft.ContainsPosition(row, col))
                return;

            if (!AreOrthogonallyAdjacent(last.Row, last.Col, row, col))
                return;

            currentDraft.SelectedPath.Add(next);
            RefreshCandidateWordFromSelection();
        }

        public void EndSelection()
        {
            if (currentSession == null || currentSession.IsFinished)
                return;

            UpdateSelectionVisuals();
            NotifyCandidateWordChanged();
        }

        public void OpenHistoryAtLatest()
        {
            if (currentSession?.MoveHistory == null || currentSession.MoveHistory.Count == 0)
                return;

            ShowHistoryEntryInternal(currentSession.MoveHistory.Count - 1);
        }

        public void ShowPreviousHistoryEntry()
        {
            if (historyViewIndex <= 0)
                return;

            ShowHistoryEntryInternal(historyViewIndex - 1);
        }

        public void ShowNextHistoryEntry()
        {
            if (currentSession?.MoveHistory == null)
                return;

            if (historyViewIndex < 0 || historyViewIndex >= currentSession.MoveHistory.Count - 1)
                return;

            ShowHistoryEntryInternal(historyViewIndex + 1);
        }

        public void CloseHistoryView()
        {
            CloseHistoryViewInternal(notify: true, restoreBoard: true);
        }

        private void ShowHistoryEntryInternal(int index)
        {
            if (currentSession?.MoveHistory == null || currentSession.MoveHistory.Count == 0)
                return;

            if (index < 0 || index >= currentSession.MoveHistory.Count)
                return;

            historyViewIndex = index;
            GameMoveRecord record = currentSession.MoveHistory[index];
            RenderHistoryRecord(record);
            HistoryEntryChanged?.Invoke(record, index, currentSession.MoveHistory.Count);
        }

        private void RenderHistoryRecord(GameMoveRecord record)
        {
            if (boardManager == null || record == null)
                return;

            BoardState snapshot = DeserializeBoard(record.BoardJson, currentSession != null ? currentSession.BoardSize : 5);
            if (snapshot == null)
                snapshot = currentSession?.Board;

            if (snapshot == null)
                return;

            boardManager.Render(snapshot);

            if (record.WordPath != null && record.WordPath.Count > 0)
                boardManager.RefreshSelection(record.WordPath, record.PlacedRow, record.PlacedCol, true);
            else
                boardManager.RefreshSelection(null, -1, -1, false);
        }

        private void CloseHistoryViewInternal(bool notify, bool restoreBoard)
        {
            bool hadHistoryView = historyViewIndex >= 0;
            historyViewIndex = -1;

            if (restoreBoard && currentSession?.Board != null && boardManager != null)
            {
                boardManager.Render(currentSession.Board);
                UpdateSelectionVisuals();
            }

            if (notify && hadHistoryView)
                HistoryViewClosed?.Invoke();
        }

        private bool TryLoadSavedGame()
        {
            StopBotTurnRoutine();
            CloseHistoryViewInternal(notify: true, restoreBoard: false);

            if (!GameSaveService.HasSave())
                return false;

            LocalGameSave save = GameSaveService.Load();
            if (save == null)
                return false;

            if (save.IsFinished)
            {
                GameSaveService.DeleteSave();
                return false;
            }

            currentSession = GameSessionSaveMapper.FromSave(save);
            currentDraft.Clear();

            if (currentSession == null || currentSession.Board == null)
                return false;

            EnsureParticipantData(currentSession);
            EnsureStartWordInUsedWords(currentSession);
            EnsureMoveHistoryInitialized(currentSession);
            RenderCurrentSession();
            NotifyDraftStateChanged();
            NotifyCandidateWordChanged();
            NotifySessionChanged();
            TryStartBotTurnIfNeeded();
            return true;
        }

        private GameSession CreateNewSession(int boardSize)
        {
            var session = new GameSession
            {
                SessionId = Guid.NewGuid().ToString(),
                BoardSize = boardSize,
                Board = new BoardState(boardSize),
                Mode = GameMode.Solo,
                Difficulty = "easy",
                PlayerOneDisplayName = GetLocalPlayerDisplayName(),
                PlayerTwoDisplayName = BuildSecondPlayerDisplayName(GameMode.Solo, "easy"),
                PlayerOneType = ParticipantType.Human,
                PlayerTwoType = ParticipantType.Bot,
                CurrentPlayerIndex = 0,
                PlayerOneScore = 0,
                PlayerTwoScore = 0,
                TurnNumber = 1,
                UsedWords = new List<string>(),
                MoveHistory = new List<GameMoveRecord>(),
                IsFinished = false,
                WinnerIndex = -2,
                StartedAtTicks = DateTime.UtcNow.Ticks,
                FinishedAtTicks = 0,
                LastAcceptedWord = "",
                LastAcceptedScore = 0,
                ResultApplied = false,
                Phase = GamePhase.WaitingForLetter
            };

            string startWord = wordDictionaryService.Normalize(startWordProvider.GetStartWord(boardSize));
            session.StartWord = startWord;
            session.Board.PlaceStartWord(startWord);
            EnsureStartWordInUsedWords(session);
            EnsureMoveHistoryInitialized(session);

            Debug.Log($"Новая игра создана. Размер поля: {boardSize}. Стартовое слово: {startWord}");

            return session;
        }

        private void RenderCurrentSession()
        {
            if (currentSession == null || currentSession.Board == null || boardManager == null)
                return;

            boardManager.BuildBoard(currentSession.Board);
            UpdateSelectionVisuals();
        }

        private void EnsureParticipantData(GameSession session)
        {
            if (session == null)
                return;

            if (string.IsNullOrWhiteSpace(session.PlayerOneDisplayName))
                session.PlayerOneDisplayName = GetLocalPlayerDisplayName();

            if (string.IsNullOrWhiteSpace(session.PlayerTwoDisplayName))
                session.PlayerTwoDisplayName = BuildSecondPlayerDisplayName(session.Mode, session.Difficulty);

            if (session.Mode == GameMode.Solo)
            {
                session.PlayerOneType = ParticipantType.Human;
                session.PlayerTwoType = ParticipantType.Bot;
            }
            else if (session.PlayerOneType == ParticipantType.Bot)
            {
                session.PlayerOneType = ParticipantType.Human;
            }
        }

        private void EnsureStartWordInUsedWords(GameSession session)
        {
            if (session == null)
                return;

            session.UsedWords ??= new List<string>();

            string startWord = wordDictionaryService.Normalize(session.StartWord);
            if (string.IsNullOrWhiteSpace(startWord))
            {
                startWord = ExtractStartWordFromBoard(session.Board);
                session.StartWord = startWord;
            }

            if (string.IsNullOrWhiteSpace(startWord))
                return;

            for (int i = 0; i < session.UsedWords.Count; i++)
            {
                if (string.Equals(session.UsedWords[i], startWord, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            session.UsedWords.Insert(0, startWord);
        }

        private void EnsureMoveHistoryInitialized(GameSession session)
        {
            if (session == null)
                return;

            session.MoveHistory ??= new List<GameMoveRecord>();
            if (session.MoveHistory.Count > 0)
                return;

            string startWord = wordDictionaryService.Normalize(session.StartWord);
            if (string.IsNullOrWhiteSpace(startWord))
                startWord = ExtractStartWordFromBoard(session.Board);

            BoardState initialBoard = BuildInitialBoard(session.BoardSize, startWord);
            if (initialBoard == null)
                return;

            session.MoveHistory.Add(new GameMoveRecord
            {
                TurnNumber = 0,
                PlayerIndex = -1,
                PlayerDisplayName = "Старт",
                PlacedRow = -1,
                PlacedCol = -1,
                PlacedLetter = "",
                Word = startWord,
                Score = 0,
                IsStartRecord = true,
                BoardJson = SerializeBoard(initialBoard),
                WordPath = BuildStartWordPath(session.BoardSize, startWord)
            });
        }

        private string GetLocalPlayerDisplayName()
        {
            if (LocalPlayerData.Instance == null)
                LocalPlayerData.Load();

            string value = LocalPlayerData.Instance != null
                ? LocalPlayerData.Instance.LocalDisplayName
                : string.Empty;

            return string.IsNullOrWhiteSpace(value) ? "Гость" : value.Trim();
        }

        private static string BuildSecondPlayerDisplayName(GameMode mode, string difficulty)
        {
            return mode switch
            {
                GameMode.Solo => $"Бот ({GetDifficultyLabel(difficulty)})",
                GameMode.LocalVersus => "Игрок 2",
                GameMode.Online => "Соперник",
                _ => "Игрок 2"
            };
        }

        private static string GetDifficultyLabel(string difficulty)
        {
            if (string.IsNullOrWhiteSpace(difficulty))
                return "лёгкий";

            return difficulty.Trim().ToLowerInvariant() switch
            {
                "easy" => "лёгкий",
                "medium" => "средний",
                "hard" => "сложный",
                _ => "лёгкий"
            };
        }

        private void OnBoardCellClicked(int row, int col)
        {
            if (currentSession == null || currentSession.Board == null)
                return;

            if (currentSession.IsFinished || currentSession.Phase == GamePhase.Finished || currentSession.Phase == GamePhase.BotTurn)
                return;

            if (currentDraft != null && currentDraft.IsActive)
            {
                Debug.Log("Сначала подтверди слово или отмени текущий ход.");
                return;
            }

            if (currentSession.Phase != GamePhase.WaitingForLetter)
                return;

            if (!gameRules.CanPlaceNewLetter(currentSession, row, col))
            {
                Debug.Log($"Нельзя поставить букву в [{row}, {col}]");
                return;
            }

            if (letterInputPopup == null)
            {
                Debug.LogError("GameController: LetterInputPopup reference is missing.");
                return;
            }

            letterInputPopup.Show(
                confirmCallback: letter => ConfirmLetterPlacement(row, col, letter),
                cancelCallback: () => Debug.Log("Выбор буквы отменён."));
        }

        private void ConfirmLetterPlacement(int row, int col, string letter)
        {
            if (currentSession == null || currentSession.Board == null)
                return;

            if (currentSession.IsFinished || currentSession.Phase != GamePhase.WaitingForLetter)
                return;

            if (!gameRules.CanPlaceNewLetter(currentSession, row, col))
                return;

            string normalizedLetter = wordDictionaryService.Normalize(letter);
            if (string.IsNullOrWhiteSpace(normalizedLetter) || normalizedLetter.Length != 1)
            {
                Debug.LogWarning("Некорректная буква.");
                return;
            }

            currentDraft.Start(row, col, normalizedLetter);
            currentSession.Board.SetLetter(row, col, normalizedLetter);
            currentSession.Phase = GamePhase.BuildingWord;

            if (boardManager != null)
                boardManager.Render(currentSession.Board);

            UpdateSelectionVisuals();
            NotifyDraftStateChanged();
            NotifyCandidateWordChanged();
            NotifySessionChanged();

            Debug.Log("Буква поставлена. Теперь проведи пальцем по буквам и собери слово.");
        }

        private void ApplyConfirmedWord(string normalizedWord)
        {
            var path = currentDraft != null && currentDraft.SelectedPath != null
                ? new List<BoardPosition>(currentDraft.SelectedPath)
                : new List<BoardPosition>();

            string placedLetter = currentDraft != null ? currentDraft.Letter : "";
            int placedRow = currentDraft != null ? currentDraft.Row : -1;
            int placedCol = currentDraft != null ? currentDraft.Col : -1;

            CompleteAcceptedWord(normalizedWord, path, placedRow, placedCol, placedLetter);
        }

        private void CompleteAcceptedWord(string normalizedWord, List<BoardPosition> wordPath, int placedRow, int placedCol, string placedLetter)
        {
            if (currentSession == null)
                return;

            currentSession.UsedWords ??= new List<string>();
            currentSession.UsedWords.Add(normalizedWord);

            int score = scoreCalculator.CalculateWordScore(normalizedWord);

            if (currentSession.CurrentPlayerIndex == 0)
                currentSession.PlayerOneScore += score;
            else
                currentSession.PlayerTwoScore += score;

            currentSession.LastAcceptedWord = normalizedWord;
            currentSession.LastAcceptedScore = score;
            currentSession.Phase = GamePhase.TurnResolved;

            RecordCompletedMove(currentSession, normalizedWord, score, placedRow, placedCol, placedLetter, wordPath);

            currentDraft.Clear();

            if (boardManager != null)
                boardManager.Render(currentSession.Board);

            UpdateSelectionVisuals();
            NotifyDraftStateChanged();
            NotifyCandidateWordChanged();
            NotifySessionChanged();

            Debug.Log($"Слово принято: {normalizedWord}. Очки: {score}");

            if (gameEndService.ShouldFinish(currentSession))
            {
                FinishGame();
                return;
            }

            currentSession.TurnNumber++;
            AdvanceTurn();
        }

        private void AdvanceTurn()
        {
            if (currentSession == null || currentSession.IsFinished)
                return;

            currentSession.CurrentPlayerIndex = currentSession.CurrentPlayerIndex == 0 ? 1 : 0;
            currentSession.Phase = currentSession.IsCurrentTurnBot
                ? GamePhase.BotTurn
                : GamePhase.WaitingForLetter;

            NotifySessionChanged();
            SaveNow();
            TryStartBotTurnIfNeeded();
        }

        private void TryStartBotTurnIfNeeded()
        {
            if (currentSession == null || currentSession.IsFinished)
                return;

            if (!currentSession.IsCurrentTurnBot)
            {
                if (currentSession.Phase != GamePhase.BuildingWord)
                    currentSession.Phase = GamePhase.WaitingForLetter;

                NotifySessionChanged();
                return;
            }

            StopBotTurnRoutine();
            currentSession.Phase = GamePhase.BotTurn;
            NotifySessionChanged();
            SaveNow();
            botTurnRoutine = StartCoroutine(RunBotTurnRoutine());
        }

        private IEnumerator RunBotTurnRoutine()
        {
            if (easyBotTurnDelay > 0f)
                yield return new WaitForSeconds(easyBotTurnDelay);

            botTurnRoutine = null;

            if (currentSession == null || currentSession.IsFinished || !currentSession.IsCurrentTurnBot)
                yield break;

            if (easyBotMoveProvider != null && easyBotMoveProvider.TryFindMove(currentSession, out BotMove move))
            {
                yield return PlayBotMovePreviewRoutine(move);
                ApplyBotMove(move);
            }
            else
            {
                Debug.Log("Бот не нашёл допустимый ход. Игра завершается.");
                FinishGame();
            }
        }

        private IEnumerator PlayBotMovePreviewRoutine(BotMove move)
        {
            if (currentSession == null || currentSession.Board == null || move == null)
                yield break;

            string normalizedLetter = wordDictionaryService.Normalize(move.Letter);
            if (string.IsNullOrWhiteSpace(normalizedLetter))
                yield break;

            currentSession.Board.SetLetter(move.Row, move.Col, normalizedLetter);

            if (boardManager != null)
            {
                boardManager.Render(currentSession.Board);
                boardManager.RefreshSelection(new List<BoardPosition> { new BoardPosition(move.Row, move.Col) }, move.Row, move.Col, true);
            }

            if (botLetterPreviewDelay > 0f)
                yield return new WaitForSeconds(botLetterPreviewDelay);

            if (boardManager != null)
                boardManager.RefreshSelection(move.Path, move.Row, move.Col, true);

            if (botWordPreviewDelay > 0f)
                yield return new WaitForSeconds(botWordPreviewDelay);
        }

        private void ApplyBotMove(BotMove move)
        {
            if (currentSession == null || currentSession.Board == null || move == null)
                return;

            if (currentSession.IsFinished || !currentSession.IsCurrentTurnBot)
                return;

            if (!currentSession.Board.CanPlaceNewLetter(move.Row, move.Col) && currentSession.Board.GetCell(move.Row, move.Col).Letter != wordDictionaryService.Normalize(move.Letter))
            {
                Debug.LogWarning("EasyBot: выбрана недопустимая клетка. Игра завершается.");
                FinishGame();
                return;
            }

            string normalizedLetter = wordDictionaryService.Normalize(move.Letter);
            string normalizedWord = wordDictionaryService.Normalize(move.Word);

            if (string.IsNullOrWhiteSpace(normalizedLetter) || normalizedLetter.Length != 1 || string.IsNullOrWhiteSpace(normalizedWord))
            {
                Debug.LogWarning("EasyBot: найден некорректный ход. Игра завершается.");
                FinishGame();
                return;
            }

            currentSession.Board.SetLetter(move.Row, move.Col, normalizedLetter);
            CompleteAcceptedWord(normalizedWord, move.Path != null ? new List<BoardPosition>(move.Path) : new List<BoardPosition>(), move.Row, move.Col, normalizedLetter);
        }

        private void StopBotTurnRoutine()
        {
            if (botTurnRoutine == null)
                return;

            StopCoroutine(botTurnRoutine);
            botTurnRoutine = null;
        }

        private void FinishGame()
        {
            StopBotTurnRoutine();
            CloseHistoryViewInternal(notify: true, restoreBoard: false);

            if (currentSession == null)
                return;

            currentSession.IsFinished = true;
            currentSession.Phase = GamePhase.Finished;
            currentSession.FinishedAtTicks = DateTime.UtcNow.Ticks;
            currentSession.WinnerIndex = gameEndService.ResolveWinnerIndex(currentSession);

            if (!currentSession.ResultApplied)
            {
                localMatchStatsService.ApplyFinishedMatch(currentSession);
                currentSession.ResultApplied = true;
            }

            GameSaveService.DeleteSave();

            if (boardManager != null && currentSession.Board != null)
                boardManager.Render(currentSession.Board);

            UpdateSelectionVisuals();
            NotifyDraftStateChanged();
            NotifyCandidateWordChanged();
            NotifySessionChanged();
            GameFinished?.Invoke(currentSession);
        }

        private void RefreshCandidateWordFromSelection()
        {
            if (currentDraft == null || currentSession == null || currentSession.Board == null)
                return;

            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < currentDraft.SelectedPath.Count; i++)
            {
                BoardPosition pos = currentDraft.SelectedPath[i];
                var cell = currentSession.Board.GetCell(pos.Row, pos.Col);

                if (cell == null || string.IsNullOrWhiteSpace(cell.Letter))
                    continue;

                builder.Append(wordDictionaryService.Normalize(cell.Letter));
            }

            currentDraft.CandidateWord = builder.ToString();
            UpdateSelectionVisuals();
            NotifyCandidateWordChanged();
        }

        private void UpdateSelectionVisuals()
        {
            if (boardManager == null)
                return;

            if (currentDraft != null && currentDraft.IsActive)
            {
                boardManager.RefreshSelection(
                    currentDraft.SelectedPath,
                    currentDraft.Row,
                    currentDraft.Col,
                    true);
            }
            else
            {
                boardManager.RefreshSelection(null, -1, -1, false);
            }
        }

        private void RecordCompletedMove(GameSession session, string word, int score, int placedRow, int placedCol, string placedLetter, List<BoardPosition> wordPath)
        {
            if (session == null || session.Board == null)
                return;

            session.MoveHistory ??= new List<GameMoveRecord>();

            session.MoveHistory.Add(new GameMoveRecord
            {
                TurnNumber = session.TurnNumber,
                PlayerIndex = session.CurrentPlayerIndex,
                PlayerDisplayName = GetCurrentPlayerDisplayName(session),
                PlacedRow = placedRow,
                PlacedCol = placedCol,
                PlacedLetter = placedLetter ?? "",
                Word = word ?? "",
                Score = score,
                IsStartRecord = false,
                BoardJson = SerializeBoard(session.Board),
                WordPath = wordPath != null ? new List<BoardPosition>(wordPath) : new List<BoardPosition>()
            });
        }

        private string GetCurrentPlayerDisplayName(GameSession session)
        {
            if (session == null)
                return "Игрок";

            return session.CurrentPlayerIndex == 0
                ? GetSafeName(session.PlayerOneDisplayName, "Игрок 1")
                : GetSafeName(session.PlayerTwoDisplayName, "Игрок 2");
        }

        private static string GetSafeName(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private static string SerializeBoard(BoardState board)
        {
            return board == null ? "" : JsonUtility.ToJson(board);
        }

        private static BoardState DeserializeBoard(string json, int fallbackSize)
        {
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    return JsonUtility.FromJson<BoardState>(json);
                }
                catch
                {
                }
            }

            return fallbackSize >= 5 ? new BoardState(fallbackSize) : null;
        }

        private BoardState BuildInitialBoard(int boardSize, string startWord)
        {
            if (boardSize < 5 || string.IsNullOrWhiteSpace(startWord))
                return null;

            var board = new BoardState(boardSize);
            board.PlaceStartWord(startWord);
            return board;
        }

        private string ExtractStartWordFromBoard(BoardState board)
        {
            if (board == null || board.Size < 5)
                return "";

            int centerRow = board.Size / 2;
            StringBuilder builder = new StringBuilder();

            for (int col = 0; col < board.Size; col++)
            {
                var cell = board.GetCell(centerRow, col);
                if (cell != null && cell.IsStartLetter && !string.IsNullOrWhiteSpace(cell.Letter))
                    builder.Append(wordDictionaryService.Normalize(cell.Letter));
            }

            return builder.ToString();
        }

        private List<BoardPosition> BuildStartWordPath(int boardSize, string startWord)
        {
            var result = new List<BoardPosition>();
            if (boardSize < 5 || string.IsNullOrWhiteSpace(startWord))
                return result;

            int centerRow = boardSize / 2;
            int startCol = (boardSize - startWord.Length) / 2;
            for (int i = 0; i < startWord.Length; i++)
                result.Add(new BoardPosition(centerRow, startCol + i));

            return result;
        }

        private bool CanUseCellForSelection(int row, int col)
        {
            if (currentSession == null || currentSession.Board == null)
                return false;

            if (currentDraft == null || !currentDraft.IsActive)
                return false;

            if (!currentSession.Board.IsInside(row, col))
                return false;

            if (currentSession.Board.IsEmpty(row, col))
                return false;

            return true;
        }

        private bool AreOrthogonallyAdjacent(int rowA, int colA, int rowB, int colB)
        {
            int dx = Mathf.Abs(rowA - rowB);
            int dy = Mathf.Abs(colA - colB);
            return dx + dy == 1;
        }

        private void RevertDraftInternal()
        {
            if (currentDraft == null || !currentDraft.IsActive)
                return;

            if (currentSession != null && currentSession.Board != null)
                currentSession.Board.ClearCell(currentDraft.Row, currentDraft.Col);

            if (currentSession != null && !currentSession.IsFinished)
                currentSession.Phase = GamePhase.WaitingForLetter;

            currentDraft.Clear();
        }

        private void NotifyDraftStateChanged()
        {
            DraftStateChanged?.Invoke(HasActiveDraft);
        }

        private void NotifyCandidateWordChanged()
        {
            CandidateWordChanged?.Invoke(CurrentCandidateWord);
        }

        private void NotifySessionChanged()
        {
            SessionChanged?.Invoke(currentSession);
        }

        private int GetBoardSizeFromSettings()
        {
            if (LocalSettings.Instance == null)
                LocalSettings.Load();

            return Mathf.Clamp(LocalSettings.Instance.BoardSize, 5, 10);
        }
    }
}
