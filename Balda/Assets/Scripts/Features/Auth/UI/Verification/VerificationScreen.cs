using System;
using Balda.UI.Common;
using Balda.Core.Navigation;
using Balda.Infrastructure.LocalStorage;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Balda.Infrastructure.Server.Auth;
using Balda.Features.MainMenu.UI;
using Balda.Features.Settings.UI;

namespace Balda.Features.Auth.UI
{
    public enum VerificationPurpose
    {
        Registration,
        Login,
        ChangeEmail,
        DeleteAccount,
        ResetStatistic
    }

    public class VerificationScreen : ScreenBase
    {
        [SerializeField] private TMP_Text emailText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button resendButton;
        [SerializeField] private VerificationCodeInput codeInput;
        [SerializeField] private ResendTimer resendTimer;

        private VerificationPurpose _purpose;
        private Type _previousScreen;
        private string _pendingEmail;
        private string _pendingUsername;

        public void Setup(
            VerificationPurpose purpose,
            Type previousScreen,
            string email,
            string username = null)
        {
            _purpose = purpose;
            _previousScreen = previousScreen;
            _pendingEmail = email;
            _pendingUsername = username;
        }

        private void OnEnable()
        {
            if (LocalPlayerData.Instance == null)
                LocalPlayerData.Load();

            if (emailText != null)
                emailText.text = _pendingEmail;

            if (confirmButton != null)
                confirmButton.interactable = false;

            if (resendButton != null)
                resendButton.interactable = false;

            if (resendTimer != null)
                resendTimer.StartTimer();
        }

        private void Update()
        {
            if (confirmButton != null && codeInput != null)
                confirmButton.interactable = codeInput.IsCodeLengthCorrect();

            if (resendButton != null && resendTimer != null)
                resendButton.interactable = resendTimer.IsFinished;
        }

        public async void OnConfirmClick()
        {
            if (AuthServiceProvider.Auth == null)
            {
                Debug.LogError("AuthServiceProvider.Auth ещё не инициализирован.");
                return;
            }

            string otpCode = codeInput.GetCode();
            AuthResult result = null;

            switch (_purpose)
            {
                case VerificationPurpose.Registration:
                    result = LocalPlayerData.Instance != null && LocalPlayerData.Instance.IsGuest
                        ? await AuthServiceProvider.Auth.VerifyGuestUpgradeAsync(_pendingEmail, otpCode, _pendingUsername)
                        : await AuthServiceProvider.Auth.VerifyRegistrationAsync(_pendingEmail, otpCode, _pendingUsername);

                    if (!result.Success)
                    {
                        Debug.LogError(result.Message);
                        return;
                    }

                    if (LocalPlayerData.Instance != null)
                    {
                        LocalPlayerData.Instance.IsFirstLaunch = false;
                        LocalPlayerData.Save();
                    }

                    ScreenRouter.Instance.Show<MainScreen>();
                    break;

                case VerificationPurpose.Login:
                    result = await AuthServiceProvider.Auth.VerifyLoginAsync(_pendingEmail, otpCode);

                    if (!result.Success)
                    {
                        Debug.LogError(result.Message);
                        return;
                    }

                    if (LocalPlayerData.Instance != null)
                    {
                        LocalPlayerData.Instance.IsFirstLaunch = false;
                        LocalPlayerData.Save();
                    }

                    ScreenRouter.Instance.Show<MainScreen>();
                    break;

                case VerificationPurpose.ChangeEmail:
                    result = await AuthServiceProvider.Auth.ConfirmEmailChangeAsync(_pendingEmail, otpCode);

                    if (!result.Success)
                    {
                        Debug.LogError(result.Message);
                        return;
                    }

                    ScreenRouter.Instance.Show<SettingsScreen>();
                    break;

                case VerificationPurpose.ResetStatistic:
                    result = await AuthServiceProvider.Auth.ConfirmResetStatisticAsync(otpCode);

                    if (!result.Success)
                    {
                        Debug.LogError(result.Message);
                        return;
                    }

                    ScreenRouter.Instance.Show<SettingsScreen>();
                    break;

                case VerificationPurpose.DeleteAccount:
                    result = await AuthServiceProvider.Auth.ConfirmDeleteAccountAsync(otpCode);

                    if (!result.Success)
                    {
                        Debug.LogError(result.Message);
                        return;
                    }

                    ScreenRouter.Instance.Show<WelcomeScreen>();
                    break;
            }
        }

        public async void OnResendClicked()
        {
            if (AuthServiceProvider.Auth == null)
            {
                Debug.LogError("AuthServiceProvider.Auth ещё не инициализирован.");
                return;
            }

            if (resendTimer == null || !resendTimer.IsFinished)
                return;

            if (resendButton != null)
                resendButton.interactable = false;

            resendTimer.StartTimer();

            AuthResult result = null;

            switch (_purpose)
            {
                case VerificationPurpose.Registration:
                    result = LocalPlayerData.Instance != null && LocalPlayerData.Instance.IsGuest
                        ? await AuthServiceProvider.Auth.BeginGuestUpgradeAsync(_pendingEmail, _pendingUsername)
                        : await AuthServiceProvider.Auth.BeginRegistrationAsync(_pendingEmail, _pendingUsername);
                    break;

                case VerificationPurpose.Login:
                    result = await AuthServiceProvider.Auth.BeginLoginAsync(_pendingEmail);
                    break;

                case VerificationPurpose.ChangeEmail:
                    result = await AuthServiceProvider.Auth.BeginEmailChangeAsync(_pendingEmail);
                    break;

                case VerificationPurpose.ResetStatistic:
                    result = await AuthServiceProvider.Auth.BeginResetStatisticAsync();
                    break;

                case VerificationPurpose.DeleteAccount:
                    result = await AuthServiceProvider.Auth.BeginDeleteAccountAsync();
                    break;
            }

            if (result != null && !result.Success)
                Debug.LogError(result.Message);
        }

        public void OnBack()
        {
            if (_previousScreen != null)
                ScreenRouter.Instance.Show(_previousScreen);
        }
    }
}