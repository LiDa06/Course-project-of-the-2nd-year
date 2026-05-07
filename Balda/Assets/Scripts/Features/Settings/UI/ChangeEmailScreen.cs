using System.Net.Mail;
using System.Threading.Tasks;
using Balda.Core.Navigation;
using Balda.Features.Auth.UI;
using Balda.Infrastructure.LocalStorage;
using Balda.Infrastructure.Server.Auth;
using Balda.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Balda.Features.Settings.UI
{
    public class ChangeEmailScreen : ScreenBase
    {
        [Header("Input")]
        [SerializeField] private TMP_InputField newEmailInput;

        [Header("Inline status")]
        [SerializeField] private TMP_Text emailStatusText;

        [Header("Overlay / popup")]
        [SerializeField] private GameObject busyOverlay;
        [SerializeField] private MessagePopup errorPopup;

        [Header("Buttons")]
        [SerializeField] private Button continueButton;
        [SerializeField] private Button cancelButton;

        private void OnEnable()
        {
            if (LocalPlayerData.Instance == null)
                LocalPlayerData.Load();

            if (newEmailInput != null)
            {
                if (PendingAccountAction.HasChangeEmail && !string.IsNullOrWhiteSpace(PendingAccountAction.NewEmail))
                {
                    newEmailInput.text = PendingAccountAction.NewEmail;
                }
                else if (LocalPlayerData.Instance != null)
                {
                    newEmailInput.text = LocalPlayerData.Instance.Email;
                }
            }

            SetBusy(false);
            ClearInlineStatus();

            if (errorPopup != null)
                errorPopup.Hide();
        }

        public async void OnContinueClick()
        {
            ClearInlineStatus();

            string newEmail = newEmailInput != null
                ? NormalizeEmail(newEmailInput.text)
                : string.Empty;

            if (!ValidateEmail(newEmail))
                return;

            string currentEmail = GetAccountEmailForVerification();
            if (!string.IsNullOrWhiteSpace(currentEmail) &&
                string.Equals(currentEmail, newEmail, System.StringComparison.OrdinalIgnoreCase))
            {
                SetInlineStatus("Это уже текущая почта аккаунта.", true);
                return;
            }

            if (LocalPlayerData.Instance == null)
                LocalPlayerData.Load();

            if (!HasLocalRegisteredAccount())
            {
                ShowGlobalError("Смена почты доступна только для зарегистрированного аккаунта.");
                return;
            }

            if (AuthServiceProvider.Auth == null)
            {
                ShowGlobalError("Сервис авторизации временно недоступен.");
                return;
            }

            SetInlineStatus("Email выглядит корректно.", false);
            SetBusy(true);

            try
            {
                if (!AuthServiceProvider.Auth.IsSignedIn)
                {
                    await StartLoginConfirmationForPendingEmailAsync(newEmail);
                    return;
                }

                await BeginEmailChangeAsync(newEmail);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                ShowGlobalError(Errors.FromException(ex, "Не удалось отправить код подтверждения. Попробуй ещё раз."));
            }
            finally
            {
                SetBusy(false);
            }
        }

        public void OnCancelClick()
        {
            PendingAccountAction.Clear();
            ScreenRouter.Instance.Show<SettingsScreen>();
        }

        private bool ValidateEmail(string newEmail)
        {
            if (string.IsNullOrWhiteSpace(newEmail))
            {
                SetInlineStatus("Введите новый email.", true);
                return false;
            }

            if (!IsValidEmail(newEmail))
            {
                SetInlineStatus("Некорректный формат email.", true);
                return false;
            }

            return true;
        }

        private async Task StartLoginConfirmationForPendingEmailAsync(string newEmail)
        {
            string currentEmail = GetAccountEmailForVerification();
            if (string.IsNullOrWhiteSpace(currentEmail))
            {
                ShowGlobalError("Не удалось определить текущий email аккаунта. Войди в аккаунт заново.");
                return;
            }

            PendingAccountAction.SetChangeEmail(newEmail);

            var result = await AuthServiceProvider.Auth.BeginLoginAsync(currentEmail);
            if (!result.Success)
            {
                ShowGlobalError(Errors.ForPopup(result.Message));
                return;
            }

            var screen = ScreenRouter.Instance.GetScreen<VerificationScreen>();
            if (screen == null)
            {
                ShowGlobalError("Не найден экран подтверждения кода.");
                return;
            }

            screen.Setup(
                VerificationPurpose.Login,
                typeof(ChangeEmailScreen),
                currentEmail
            );

            ScreenRouter.Instance.Show<VerificationScreen>();
        }

        private async Task BeginEmailChangeAsync(string newEmail)
        {
            PendingAccountAction.SetChangeEmail(newEmail);

            var result = await AuthServiceProvider.Auth.BeginEmailChangeAsync(newEmail);
            if (!result.Success)
            {
                ShowGlobalError(Errors.ForPopup(result.Message));
                return;
            }

            var verificationScreen = ScreenRouter.Instance.GetScreen<VerificationScreen>();
            if (verificationScreen == null)
            {
                ShowGlobalError("Не найден экран подтверждения кода.");
                return;
            }

            verificationScreen.Setup(
                VerificationPurpose.ChangeEmail,
                typeof(ChangeEmailScreen),
                newEmail
            );

            ScreenRouter.Instance.Show<VerificationScreen>();
        }

        private static bool HasLocalRegisteredAccount()
        {
            var local = LocalPlayerData.Instance;

            bool hasLocalAccount =
                local != null &&
                !string.IsNullOrWhiteSpace(local.CloudUserId) &&
                !string.IsNullOrWhiteSpace(local.Email);

            bool hasAuthSession =
                AuthServiceProvider.Auth != null &&
                AuthServiceProvider.Auth.IsSignedIn;

            return hasLocalAccount || hasAuthSession;
        }

        private static string GetAccountEmailForVerification()
        {
            if (LocalPlayerData.Instance == null)
                LocalPlayerData.Load();

            string email = LocalPlayerData.Instance != null
                ? LocalPlayerData.Instance.Email
                : string.Empty;

            if (string.IsNullOrWhiteSpace(email) && AuthServiceProvider.Auth != null)
                email = AuthServiceProvider.Auth.CurrentAccountEmail;

            return NormalizeEmail(email);
        }

        private static string NormalizeEmail(string email)
        {
            return string.IsNullOrWhiteSpace(email)
                ? string.Empty
                : email.Trim().ToLowerInvariant();
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                _ = new MailAddress(email);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void SetInlineStatus(string message, bool isError)
        {
            if (emailStatusText == null)
                return;

            emailStatusText.gameObject.SetActive(true);
            emailStatusText.text = message;
            emailStatusText.color = isError
                ? new Color(0.85f, 0.2f, 0.2f)
                : new Color(0.2f, 0.7f, 0.3f);
        }

        private void ClearInlineStatus()
        {
            if (emailStatusText == null)
                return;

            emailStatusText.text = string.Empty;
            emailStatusText.gameObject.SetActive(false);
        }

        private void ShowGlobalError(string message)
        {
            string friendlyMessage = Errors.ForPopup(message);

            if (errorPopup != null)
                errorPopup.Show("Ошибка", friendlyMessage);
            else
                Debug.LogError(friendlyMessage);
        }

        private void SetBusy(bool value)
        {
            if (busyOverlay != null)
                busyOverlay.SetActive(value);

            if (newEmailInput != null)
                newEmailInput.interactable = !value;

            if (continueButton != null)
                continueButton.interactable = !value;

            if (cancelButton != null)
                cancelButton.interactable = !value;
        }
    }
}
