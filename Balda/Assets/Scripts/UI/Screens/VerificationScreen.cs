using Assets.Scripts.Core;
using TMPro;
using UnityEngine;
using Assets.Scripts.UI.Screens.VerificationTools;
using UnityEngine.UI;

namespace Assets.Scripts.UI.Screens
{
    public class VerificationScreen : ScreenBase
    {
        [SerializeField] private TMP_Text email;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button resendButton;
        [SerializeField] private VerificationCodeInput code;
        [SerializeField] private ResendTimer resendTimer;

        void OnEnable()
        {
            email.text = SessionData.Email;

            confirmButton.interactable = false;
            resendButton.interactable = false;

            resendTimer.StartTimer();
        }

        void Update()
        {
            confirmButton.interactable = code.IsCodeLengthCorrect();
            resendButton.interactable = resendTimer.IsFinished;
        }

        public void OnConfirmClick()
        {
            //Реализовать логику проверки кода с сервера

            ScreenRouter.Instance.Show<MainScreen>();
        }
        public void OnResendClicked()
        {
            if (!resendTimer.IsFinished)
                return;

            resendButton.interactable = false;
            resendTimer.StartTimer();

            // Реализовать логику взаимодействия с сервером
        }
    }
}
