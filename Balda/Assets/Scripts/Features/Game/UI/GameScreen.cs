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

            if (historyButton != null)
            {
                historyButton.onClick.RemoveListener(OnOpenHistory);
                historyButton.onClick.AddListener(OnOpenHistory);
                historyButton.gameObject.SetActive(false);
            }

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

            if (gameController != null)
            {
                gameController.DraftStateChanged -= OnDraftStateChanged;
                gameController.CandidateWordChanged -= OnCandidateWordChanged;
                gameController.SavedGameChoiceRequested -= OnSavedGameChoiceRequested;
                gameController.SessionChanged -= OnSessionChanged;
                gameController.GameFinished -= OnGameFinished;
                gameController.HistoryEntryChanged -= OnHistoryEntryChanged;
                gameController.HistoryViewClosed -= OnHistoryViewClosed;
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
        }

        private void OnGameFinished(GameSession session)
        {
            RefreshSessionInfo(session);
            RefreshHistoryButton(session);
            ShowFinishPanel(session);
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
                    lastWordText.text = $"Последнее слово: { session.LastAcceptedWord} (+{ session.LastAcceptedScore})";
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

        private static string GetSafeName(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }
}
