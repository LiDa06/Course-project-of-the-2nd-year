using Balda.Core.Navigation;
using Balda.Features.Game.Flow;
using Balda.Features.MainMenu.UI;
using Balda.UI.Common;
using TMPro;
using UnityEngine;

namespace Balda.Features.Game.UI
{
    public class GameScreen : ScreenBase
    {
        [SerializeField] private GameController gameController;
        [SerializeField] private GameObject wordInputPanel;
        [SerializeField] private TMP_Text candidateWordText;
        [SerializeField] private SavedGamePromptPopup savedGamePromptPopup;

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

            SetWordInputVisible(gameController.HasActiveDraft);
            OnCandidateWordChanged(gameController.CurrentCandidateWord);

            gameController.InitializeForScreen();
        }

        private void OnDisable()
        {
            if (gameController != null)
            {
                gameController.DraftStateChanged -= OnDraftStateChanged;
                gameController.CandidateWordChanged -= OnCandidateWordChanged;
                gameController.SavedGameChoiceRequested -= OnSavedGameChoiceRequested;
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

        private void SetWordInputVisible(bool visible)
        {
            if (wordInputPanel != null)
                wordInputPanel.SetActive(visible);

            if (!visible)
                OnCandidateWordChanged("");
        }

        public void OnBack()
        {
            if (savedGamePromptPopup != null)
                savedGamePromptPopup.Hide();

            gameController?.SaveNow();
            ScreenRouter.Instance.Show<MainScreen>();
        }

        public void OnStartNewGame()
        {
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
    }
}
