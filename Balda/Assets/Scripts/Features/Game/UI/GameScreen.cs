using System.Collections;
using Balda.Core.Navigation;
using Balda.Features.Game.Domain;
using Balda.Features.Game.Flow;
using Balda.Features.MainMenu.UI;
using Balda.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Balda.Features.Game.UI
{
    public class GameScreen : ScreenBase
    {
        [SerializeField] private GameController gameController;
        [SerializeField] private GameObject wordInputPanel;
        [SerializeField] private TMP_Text candidateWordText;
        [SerializeField] private TMP_Text playerOneNameText;
        [SerializeField] private TMP_Text playerTwoNameText;
        [SerializeField] private TMP_Text playerOneScoreText;
        [SerializeField] private TMP_Text playerTwoScoreText;
        [SerializeField] private TMP_Text currentTurnText;
        [SerializeField] private TMP_Text lastWordText;
        [SerializeField] private Button historyButton;
        [SerializeField] private SavedGamePromptPopup savedGamePromptPopup;
        [SerializeField] private GameResultPopup gameResultPopup;
        [SerializeField] private GameHistoryPopup gameHistoryPopup;
        [SerializeField] private Button usedWordsButton;
        [SerializeField] private UsedWordsPopup usedWordsPopup;
        [SerializeField] private SuggestWordPopup suggestWordPopup;
        [SerializeField] private TMP_Text gameMessageText;
        [SerializeField] private float gameMessageVisibleSeconds = 3.5f;

        private Coroutine clearGameMessageRoutine;

        private void OnEnable()
        {
            if (gameController == null)
            {
                Debug.LogError("GameScreen: GameController reference is missing.");
                return;
            }

            gameController.DraftStateChanged -= OnDraftStateChanged;
            gameController.DraftStateChanged += OnDraftStateChanged;

            gameController.CandidateWordChanged -= OnCandidateWordChanged;
            gameController.CandidateWordChanged += OnCandidateWordChanged;

            gameController.SavedGameChoiceRequested -= OnSavedGameChoiceRequested;
            gameController.SavedGameChoiceRequested += OnSavedGameChoiceRequested;

            gameController.SessionChanged -= OnSessionChanged;
            gameController.SessionChanged += OnSessionChanged;

            gameController.GameFinished -= OnGameFinished;
            gameController.GameFinished += OnGameFinished;

            gameController.HistoryEntryChanged -= OnHistoryEntryChanged;
            gameController.HistoryEntryChanged += OnHistoryEntryChanged;

            gameController.HistoryViewClosed -= OnHistoryViewClosed;
            gameController.HistoryViewClosed += OnHistoryViewClosed;

            gameController.WordSuggestionRequested -= OnWordSuggestionRequested;
            gameController.WordSuggestionRequested += OnWordSuggestionRequested;

            gameController.GameMessageRequested -= OnGameMessageRequested;
            gameController.GameMessageRequested += OnGameMessageRequested;

            ClearGameMessage();

            if (suggestWordPopup != null)
                suggestWordPopup.Hide();

            if (historyButton != null)
            {
                historyButton.onClick.RemoveListener(OnOpenHistory);
                historyButton.onClick.AddListener(OnOpenHistory);
                historyButton.gameObject.SetActive(false);
            }

            if (usedWordsButton != null)
            {
                usedWordsButton.onClick.RemoveListener(OnOpenUsedWords);
                usedWordsButton.onClick.AddListener(OnOpenUsedWords);
                usedWordsButton.gameObject.SetActive(false);
            }

            if (usedWordsPopup != null)
                usedWordsPopup.Hide();

            if (gameResultPopup != null)
                gameResultPopup.Hide();

            if (gameHistoryPopup != null)
                gameHistoryPopup.Hide();

            SetWordInputVisible(false);
            OnCandidateWordChanged(string.Empty);
            RefreshSessionInfo(null);

            gameController.InitializeForScreen();
        }

        private void OnDisable()
        {
            if (historyButton != null)
                historyButton.onClick.RemoveListener(OnOpenHistory);

            if (usedWordsButton != null)
                usedWordsButton.onClick.RemoveListener(OnOpenUsedWords);

            if (gameController != null)
            {
                gameController.DraftStateChanged -= OnDraftStateChanged;
                gameController.CandidateWordChanged -= OnCandidateWordChanged;
                gameController.SavedGameChoiceRequested -= OnSavedGameChoiceRequested;
                gameController.SessionChanged -= OnSessionChanged;
                gameController.GameFinished -= OnGameFinished;
                gameController.HistoryEntryChanged -= OnHistoryEntryChanged;
                gameController.HistoryViewClosed -= OnHistoryViewClosed;
                gameController.WordSuggestionRequested -= OnWordSuggestionRequested;
                gameController.GameMessageRequested -= OnGameMessageRequested;
            }
        }

        private void OnDraftStateChanged(bool hasDraft)
        {
            SetWordInputVisible(hasDraft);
        }

        private void OnCandidateWordChanged(string word)
        {
            if (candidateWordText != null)
                candidateWordText.text = string.IsNullOrWhiteSpace(word) ? "-" : word;
        }

        private void OnSavedGameChoiceRequested()
        {
            if (savedGamePromptPopup == null)
            {
                Debug.LogWarning("SavedGamePromptPopup is not assigned. Continue saved game by default.");
                gameController.ContinueSavedGame();
                return;
            }

            savedGamePromptPopup.Show(
                continueCallback: () => gameController.ContinueSavedGame(),
                newGameCallback: () => gameController.StartFreshGame(),
                closeCallback: OnBack
            );
        }

        private void OnSessionChanged(GameSession session)
        {
            RefreshSessionInfo(session);
            RefreshHistoryButton(session);
            RefreshUsedWordsButton(session);
        }

        private void OnGameFinished(GameSession session)
        {
            RefreshSessionInfo(session);
            RefreshHistoryButton(session);
            ShowFinishPanel(session);
            RefreshUsedWordsButton(session);
        }

        private void OnHistoryEntryChanged(GameMoveRecord entry, int index, int totalCount)
        {
            if (gameHistoryPopup == null)
                return;

            gameHistoryPopup.Show(
                entry,
                index,
                totalCount,
                previousCallback: () => gameController.ShowPreviousHistoryEntry(),
                nextCallback: () => gameController.ShowNextHistoryEntry(),
                closeCallback: () => gameController.CloseHistoryView());
        }

        private void OnHistoryViewClosed()
        {
            if (gameHistoryPopup != null)
                gameHistoryPopup.Hide();
        }

        private void SetWordInputVisible(bool visible)
        {
            if (wordInputPanel != null)
                wordInputPanel.SetActive(visible);

            if (!visible)
                OnCandidateWordChanged(string.Empty);
        }

        private void RefreshSessionInfo(GameSession session)
        {
            if (session == null)
            {
                if (playerOneNameText != null)
                    playerOneNameText.text = "Игрок 1";

                if (playerTwoNameText != null)
                    playerTwoNameText.text = "Игрок 2";

                if (playerOneScoreText != null)
                    playerOneScoreText.text = "0";

                if (playerTwoScoreText != null)
                    playerTwoScoreText.text = "0";

                if (currentTurnText != null)
                    currentTurnText.text = "-";

                if (lastWordText != null)
                    lastWordText.text = "Последнее слово: - ";

                return;
            }

            string playerOneName = GetSafeName(session.PlayerOneDisplayName, "Игрок 1");
            string playerTwoName = GetSafeName(session.PlayerTwoDisplayName, "Игрок 2");

            if (playerOneNameText != null)
                playerOneNameText.text = playerOneName;

            if (playerTwoNameText != null)
                playerTwoNameText.text = playerTwoName;

            if (playerOneScoreText != null)
                playerOneScoreText.text = session.PlayerOneScore.ToString();

            if (playerTwoScoreText != null)
                playerTwoScoreText.text = session.PlayerTwoScore.ToString();

            if (currentTurnText != null)
            {
                if (session.IsFinished)
                {
                    currentTurnText.text = "Игра завершена";
                }
                else
                {
                    string currentName = session.CurrentPlayerIndex == 0 ? playerOneName : playerTwoName;
                    currentTurnText.text = session.Phase == GamePhase.BotTurn
                        ? $"Ход: {currentName}..."
                        : $"Ход: {currentName}";
                }
            }

            if (lastWordText != null)
            {
                if (string.IsNullOrWhiteSpace(session.LastAcceptedWord))
                    lastWordText.text = "Последнее слово: - ";
                else
                    lastWordText.text = $"Последнее слово: {session.LastAcceptedWord} (+{session.LastAcceptedScore})";
            }
        }

        private void RefreshHistoryButton(GameSession session)
        {
            if (historyButton == null)
                return;

            bool shouldShow = session != null
                              && session.IsFinished
                              && session.MoveHistory != null
                              && session.MoveHistory.Count > 0;

            historyButton.gameObject.SetActive(shouldShow);
            historyButton.interactable = shouldShow;
        }

        private void ShowFinishPanel(GameSession session)
        {
            if (gameResultPopup == null)
                return;

            gameResultPopup.Show(
                session,
                onNewGameCallback: () => gameController.StartFreshGame(),
                onBackToMenuCallback: OnBack
            );
        }

        private void OnOpenHistory()
        {
            gameResultPopup?.Hide();
            gameController?.OpenHistoryAtLatest();
        }

        public void OnBack()
        {
            if (savedGamePromptPopup != null)
                savedGamePromptPopup.Hide();

            if (gameResultPopup != null)
                gameResultPopup.Hide();

            if (gameHistoryPopup != null)
                gameHistoryPopup.Hide();

            if (usedWordsPopup != null)
                usedWordsPopup.Hide();

            if (suggestWordPopup != null)
                suggestWordPopup.Hide();

            ClearGameMessage();

            gameController?.CloseHistoryView();
            gameController?.SaveNow();
            ScreenRouter.Instance.Show<MainScreen>();
        }

        public void OnStartNewGame()
        {
            if (gameResultPopup != null)
                gameResultPopup.Hide();

            if (gameHistoryPopup != null)
                gameHistoryPopup.Hide();

            if (usedWordsPopup != null)
                usedWordsPopup.Hide();

            if (suggestWordPopup != null)
                suggestWordPopup.Hide();

            ClearGameMessage();

            gameController?.CloseHistoryView();
            gameController?.StartFreshGame();
        }

        public void OnSubmitWord()
        {
            gameController?.TrySubmitCurrentSelection();
        }

        public void OnCancelTurn()
        {
            gameController?.CancelCurrentDraft();
        }

        private void OnGameMessageRequested(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                ClearGameMessage();
                return;
            }

            if (gameMessageText == null)
            {
                Debug.Log(message);
                return;
            }

            gameMessageText.gameObject.SetActive(true);
            gameMessageText.text = message;

            if (clearGameMessageRoutine != null)
                StopCoroutine(clearGameMessageRoutine);

            clearGameMessageRoutine = StartCoroutine(ClearGameMessageAfterDelay());
        }

        private IEnumerator ClearGameMessageAfterDelay()
        {
            float delay = Mathf.Max(0.5f, gameMessageVisibleSeconds);
            yield return new WaitForSeconds(delay);
            clearGameMessageRoutine = null;
            ClearGameMessage();
        }

        private void ClearGameMessage()
        {
            if (clearGameMessageRoutine != null)
            {
                StopCoroutine(clearGameMessageRoutine);
                clearGameMessageRoutine = null;
            }

            if (gameMessageText != null)
            {
                gameMessageText.text = string.Empty;
                gameMessageText.gameObject.SetActive(false);
            }
        }

        private static string GetSafeName(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private void RefreshUsedWordsButton(GameSession session)
        {
            if (usedWordsButton == null)
                return;

            bool shouldShow = session != null
                              && session.UsedWords != null
                              && session.UsedWords.Count > 0;

            usedWordsButton.gameObject.SetActive(shouldShow);
            usedWordsButton.interactable = shouldShow;
        }

        private void OnOpenUsedWords()
        {
            if (usedWordsPopup == null || gameController == null)
                return;

            var words = gameController.GetUsedWordsSnapshot();
            usedWordsPopup.Show(words);
        }

        private void OnWordSuggestionRequested(string word)
        {
            if (suggestWordPopup == null || gameController == null)
                return;

            suggestWordPopup.Show(
                word,
                onSubmit: suggestedWord =>
                {
                    gameController.SaveWordSuggestion(suggestedWord);
                    suggestWordPopup.Hide();
                },
                onCancel: () =>
                {
                    suggestWordPopup.Hide();
                });
        }
    }
}
