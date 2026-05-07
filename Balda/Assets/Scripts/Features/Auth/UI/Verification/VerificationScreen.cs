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
    public class VerificationScreen : ScreenBase
    {
        [Header("Main")]
        [SerializeField] private TMP_Text emailText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button resendButton;
        [SerializeField] private VerificationCodeInput codeInput;
        [SerializeField] private ResendTimer resendTimer;

        [Header("Inline status")]
        [SerializeField] private TMP_Text statusText;

        [Header("Overlay / popup")]
        [SerializeField] private GameObject busyOverlay;
        [SerializeField] private TMP_Text busyLabelText;
        [SerializeField] private MessagePopup errorPopup;
        [SerializeField] private MessagePopup infoPopup;

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
            _pendingEmail = email ?? string.Empty;
            _pendingUsername = username ?? string.Empty;
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

            SetBusy(false, string.Empty);
            SetStatus(string.Empty, false);

            if (errorPopup != null)
                errorPopup.Hide();

            if (infoPopup != null)
                infoPopup.Hide();
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
                ShowError("Сервис авторизации ещё не инициализирован.");
                return;
            }

            if (codeInput == null)
            {
                ShowError("Поле ввода кода не найдено.");
                return;
            }

            string otpCode = codeInput.GetCode();

            if (string.IsNullOrWhiteSpace(otpCode))
            {
                SetStatus("Введите код из письма.", true);
                return;
            }

            AuthResult result = null;
            SetStatus(string.Empty, false);

            try
            {
                switch (_purpose)
                {
                    case VerificationPurpose.Registration:
                        SetBusy(true, "Подтверждаем регистрацию...");

                        result = LocalPlayerData.Instance != null && LocalPlayerData.Instance.IsGuest
                            ? await AuthServiceProvider.Auth.VerifyGuestUpgradeAsync(_pendingEmail, otpCode, _pendingUsername)
                            : await AuthServiceProvider.Auth.VerifyRegistrationAsync(_pendingEmail, otpCode, _pendingUsername);

                        if (!result.Success)
                        {
                            ShowError(Errors.ForPopup(result.Message));
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
                        SetBusy(true, "Подтверждаем вход...");

                        result = await AuthServiceProvider.Auth.VerifyLoginAsync(_pendingEmail, otpCode);

                        if (!result.Success)
                        {
                            ShowError(Errors.ForPopup(result.Message));
                            return;
                        }

                        if (LocalPlayerData.Instance != null)
                        {
                            LocalPlayerData.Instance.IsFirstLaunch = false;
                            LocalPlayerData.Save();
                        }

                        if (await TryCompletePendingAccountActionAfterLoginAsync())
                            return;

                        OpenAfterLogin();
                        break;

                    case VerificationPurpose.ChangeEmail:
                        SetBusy(true, "Подтверждаем смену почты...");

                        result = await AuthServiceProvider.Auth.ConfirmEmailChangeAsync(_pendingEmail, otpCode);

                        if (!result.Success)
                        {
                            ShowError(Errors.ForPopup(result.Message));
                            return;
                        }

                        PendingAccountAction.Clear();
                        ScreenRouter.Instance.Show<SettingsScreen>();
                        break;

                    case VerificationPurpose.ResetStatistic:
                        SetBusy(true, "Подтверждаем сброс статистики...");

                        result = await AuthServiceProvider.Auth.ConfirmResetStatisticAsync(otpCode);

                        if (!result.Success)
                        {
                            ShowError(Errors.ForPopup(result.Message));
                            return;
                        }

                        ScreenRouter.Instance.Show<SettingsScreen>();
                        break;

                    case VerificationPurpose.DeleteAccount:
                        SetBusy(true, "Подтверждаем удаление аккаунта...");

                        result = await AuthServiceProvider.Auth.ConfirmDeleteAccountAsync(otpCode);

                        if (!result.Success)
                        {
                            ShowError(Errors.ForPopup(result.Message));
                            return;
                        }

                        ScreenRouter.Instance.Show<WelcomeScreen>();
                        break;

                    default:
                        ShowError("Неизвестный тип подтверждения.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("VerificationScreen.OnConfirmClick exception:");
                Debug.LogError(ex.ToString());

                if (ex.InnerException != null)
                    Debug.LogError("Inner 1: " + ex.InnerException);

                if (ex.InnerException?.InnerException != null)
                    Debug.LogError("Inner 2: " + ex.InnerException.InnerException);

                ShowError(Errors.FromException(ex, "Ошибка во время подтверждения. Попробуй ещё раз."));
            }
            finally
            {
                SetBusy(false, string.Empty);
            }
        }

        public async void OnResendClicked()
        {
            if (AuthServiceProvider.Auth == null)
            {
                ShowError("Сервис авторизации ещё не инициализирован.");
                return;
            }

            if (resendTimer == null || !resendTimer.IsFinished)
                return;

            if (resendButton != null)
                resendButton.interactable = false;

            resendTimer.StartTimer();
            SetStatus(string.Empty, false);

            AuthResult result = null;

            try
            {
                SetBusy(true, "Отправляем код повторно...");

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
                {
                    ShowError(Errors.ForPopup(result.Message));
                    return;
                }

                ShowInfo("Код отправлен повторно.");
            }
            catch (Exception ex)
            {
                Debug.LogError("VerificationScreen.OnResendClicked exception:");
                Debug.LogError(ex.ToString());

                if (ex.InnerException != null)
                    Debug.LogError("Inner 1: " + ex.InnerException);

                if (ex.InnerException?.InnerException != null)
                    Debug.LogError("Inner 2: " + ex.InnerException.InnerException);

                ShowError(Errors.FromException(ex, "Не удалось повторно отправить код. Попробуй ещё раз."));
            }
            finally
            {
                SetBusy(false, string.Empty);
            }
        }

        public void OnBack()
        {
            if (_previousScreen != null)
                ScreenRouter.Instance.Show(_previousScreen);
        }

        private async System.Threading.Tasks.Task<bool> TryCompletePendingAccountActionAfterLoginAsync()
        {
            if (!PendingAccountAction.HasPending)
                return false;

            if (AuthServiceProvider.Auth == null)
            {
                ShowError("Сервис авторизации ещё не инициализирован.");
                return true;
            }

            if (PendingAccountAction.HasChangeName)
            {
                string newName = PendingAccountAction.NewUsername;

                if (string.IsNullOrWhiteSpace(newName))
                {
                    PendingAccountAction.Clear();
                    ShowError("Новое имя не найдено. Попробуй изменить имя ещё раз.");
                    ScreenRouter.Instance.Show<ChangeNameScreen>();
                    return true;
                }

                SetBusy(true, "Сохраняем новое имя...");

                var changeResult = await AuthServiceProvider.Auth.ChangeUsernameAsync(newName);
                if (!changeResult.Success)
                {
                    ShowError(Errors.ForPopup(changeResult.Message));
                    ScreenRouter.Instance.Show<ChangeNameScreen>();
                    return true;
                }

                if (LocalPlayerData.Instance != null)
                {
                    LocalPlayerData.Instance.LocalDisplayName = newName;
                    LocalPlayerData.Instance.IsGuest = false;
                    LocalPlayerData.Save();
                }

                PendingAccountAction.Clear();
                ScreenRouter.Instance.Show<SettingsScreen>();
                return true;
            }

            if (PendingAccountAction.HasChangeEmail)
            {
                string newEmail = PendingAccountAction.NewEmail;

                if (string.IsNullOrWhiteSpace(newEmail))
                {
                    PendingAccountAction.Clear();
                    ShowError("Новая почта не найдена. Попробуй изменить почту ещё раз.");
                    ScreenRouter.Instance.Show<ChangeEmailScreen>();
                    return true;
                }

                SetBusy(true, "Отправляем код на новую почту...");

                var emailResult = await AuthServiceProvider.Auth.BeginEmailChangeAsync(newEmail);
                if (!emailResult.Success)
                {
                    ShowError(Errors.ForPopup(emailResult.Message));
                    ScreenRouter.Instance.Show<ChangeEmailScreen>();
                    return true;
                }

                Setup(
                    VerificationPurpose.ChangeEmail,
                    typeof(ChangeEmailScreen),
                    newEmail
                );

                if (codeInput != null)
                    codeInput.Clear();

                ScreenRouter.Instance.Show<VerificationScreen>();
                return true;
            }

            return false;
        }

        private void OpenAfterLogin()
        {
            // Обычный вход с LoginScreen ведет на главный экран.
            // Если код подтверждения был запрошен из настроек/смены имени/почты
            // для восстановления Supabase-сессии, возвращаем пользователя туда,
            // откуда он начал действие.
            if (_previousScreen != null &&
                _previousScreen != typeof(LoginScreen) &&
                _previousScreen != typeof(WelcomeScreen))
            {
                ScreenRouter.Instance.Show(_previousScreen);
                return;
            }

            ScreenRouter.Instance.Show<MainScreen>();
        }

        private void SetStatus(string message, bool isError)
        {
            if (statusText == null)
                return;

            bool hasMessage = !string.IsNullOrWhiteSpace(message);
            statusText.gameObject.SetActive(hasMessage);
            statusText.text = message ?? string.Empty;
            statusText.color = isError
                ? new Color(0.85f, 0.2f, 0.2f)
                : new Color(0.2f, 0.7f, 0.3f);
        }

        private void SetBusy(bool value, string message)
        {
            if (busyOverlay != null)
                busyOverlay.SetActive(value);

            if (busyLabelText != null)
                busyLabelText.text = message ?? string.Empty;

            if (confirmButton != null)
                confirmButton.interactable = !value && codeInput != null && codeInput.IsCodeLengthCorrect();

            if (resendButton != null)
                resendButton.interactable = !value && resendTimer != null && resendTimer.IsFinished;
        }

        private void ShowError(string message)
        {
            string friendlyMessage = Errors.ForPopup(message);
            Debug.LogError(friendlyMessage);
            SetStatus(friendlyMessage, true);

            if (errorPopup != null)
                errorPopup.Show("Ошибка", friendlyMessage);
        }

        private void ShowInfo(string message)
        {
            string friendlyMessage = Errors.ForPopup(message, "Операция выполнена.");
            Debug.Log(friendlyMessage);
            SetStatus(friendlyMessage, false);

            if (infoPopup != null)
                infoPopup.Show("Информация", friendlyMessage);
        }
    }
}