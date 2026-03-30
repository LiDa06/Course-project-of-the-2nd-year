using System;
using System.Collections.Generic;
using System.Text;
using Balda.Features.Game.Domain;
using Balda.Features.Game.Rules;
using Balda.Features.Game.SaveLoad;
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

        private GameSession currentSession;
        private TurnDraft currentDraft;

        private GameRules gameRules;
        private ScoreCalculator scoreCalculator;
        private WordDictionaryService wordDictionaryService;
        private StartWordProvider startWordProvider;
        private WordValidationService wordValidationService;

        public GameSession CurrentSession => currentSession;
        public bool HasActiveDraft => currentDraft != null && currentDraft.IsActive;
        public string CurrentCandidateWord => currentDraft != null ? currentDraft.CandidateWord : "";

        public Action<bool> DraftStateChanged;
        public Action<string> CandidateWordChanged;
        public Action SavedGameChoiceRequested;

        private void Awake()
        {
            gameRules = new GameRules();
            scoreCalculator = new ScoreCalculator();

            wordDictionaryService = new WordDictionaryService();
            startWordProvider = new StartWordProvider(wordDictionaryService);
            wordValidationService = new WordValidationService(wordDictionaryService);

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
            currentDraft.Clear();

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
            GameSaveService.DeleteSave();

            int boardSize = GetBoardSizeFromSettings();

            currentSession = CreateNewSession(boardSize);
            currentDraft.Clear();

            RenderCurrentSession();
            NotifyDraftStateChanged();
            NotifyCandidateWordChanged();
            SaveNow();
        }

        public void SaveNow()
        {
            if (currentSession == null)
                return;

            if (currentDraft != null && currentDraft.IsActive)
            {
                RevertDraftInternal();

                if (boardManager != null && currentSession.Board != null)
                    boardManager.Render(currentSession.Board);

                UpdateSelectionVisuals();
                NotifyDraftStateChanged();
                NotifyCandidateWordChanged();
            }

            LocalGameSave save = GameSessionSaveMapper.ToSave(currentSession);
            GameSaveService.Save(save);
        }

        public bool TrySubmitCurrentSelection()
        {
            if (currentSession == null)
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
            if (currentDraft == null || !currentDraft.IsActive)
                return;

            RevertDraftInternal();

            if (boardManager != null && currentSession != null && currentSession.Board != null)
                boardManager.Render(currentSession.Board);

            UpdateSelectionVisuals();
            NotifyDraftStateChanged();
            NotifyCandidateWordChanged();
        }

        public void BeginSelectionAt(int row, int col)
        {
            if (!CanUseCellForSelection(row, col))
                return;

            currentDraft.ClearSelection();
            currentDraft.SelectedPath.Add(new BoardPosition(row, col));

            RefreshCandidateWordFromSelection();
        }

        public void ContinueSelectionAt(int row, int col)
        {
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
            UpdateSelectionVisuals();
            NotifyCandidateWordChanged();
        }

        private bool TryLoadSavedGame()
        {
            if (!GameSaveService.HasSave())
                return false;

            LocalGameSave save = GameSaveService.Load();
            if (save == null)
                return false;

            currentSession = GameSessionSaveMapper.FromSave(save);
            currentDraft.Clear();

            if (currentSession == null || currentSession.Board == null)
            {
                Debug.LogWarning("GameController: saved game is invalid.");
                return false;
            }

            RenderCurrentSession();
            NotifyDraftStateChanged();
            NotifyCandidateWordChanged();
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
                CurrentPlayerIndex = 0,
                PlayerOneScore = 0,
                PlayerTwoScore = 0,
                IsFinished = false,
                UsedWords = new List<string>()
            };

            string startWord = startWordProvider.GetStartWord(boardSize);
            session.Board.PlaceStartWord(startWord);

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

        private void OnBoardCellClicked(int row, int col)
        {
            if (currentSession == null || currentSession.Board == null)
                return;

            if (currentDraft != null && currentDraft.IsActive)
            {
                Debug.Log("Сначала подтверди слово или отмени текущий ход.");
                return;
            }

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
                cancelCallback: () => Debug.Log("Выбор буквы отменён.")
            );
        }

        private void ConfirmLetterPlacement(int row, int col, string letter)
        {
            if (currentSession == null || currentSession.Board == null)
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

            if (boardManager != null)
                boardManager.Render(currentSession.Board);

            UpdateSelectionVisuals();
            NotifyDraftStateChanged();
            NotifyCandidateWordChanged();

            Debug.Log("Буква поставлена. Теперь проведи пальцем по буквам и собери слово.");
        }

        private void ApplyConfirmedWord(string normalizedWord)
        {
            currentSession.UsedWords ??= new List<string>();
            currentSession.UsedWords.Add(normalizedWord);

            int score = scoreCalculator.CalculateWordScore(normalizedWord);

            if (currentSession.CurrentPlayerIndex == 0)
                currentSession.PlayerOneScore += score;
            else
                currentSession.PlayerTwoScore += score;

            currentDraft.Clear();

            if (boardManager != null)
                boardManager.Render(currentSession.Board);

            UpdateSelectionVisuals();
            NotifyDraftStateChanged();
            NotifyCandidateWordChanged();
            SaveNow();

            Debug.Log($"Слово принято: {normalizedWord}. Очки: {score}");
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

        private int GetBoardSizeFromSettings()
        {
            if (LocalSettings.Instance == null)
                LocalSettings.Load();

            return Mathf.Clamp(LocalSettings.Instance.BoardSize, 5, 10);
        }
    }
}