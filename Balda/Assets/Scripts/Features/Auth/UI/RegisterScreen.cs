using Balda.Core.Navigation;
using Balda.Infrastructure.LocalStorage;
using Balda.Infrastructure.Server.Auth;
using Balda.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Balda.Features.Auth.UI
{
    public class RegisterScreen : ScreenBase
    {
        [Header("Inputs")]
        [SerializeField] private TMP_InputField nameInput;
        [SerializeField] private TMP_InputField emailInput;

        [Header("Inline statuses")]
        [SerializeField] private TMP_Text nameStatusText;
        [SerializeField] private TMP_Text emailStatusText;

        [Header("Overlay / popup")]
        [SerializeField] private GameObject busyOverlay;
        [SerializeField] private MessagePopup errorPopup;

        [Header("Buttons")]
        [SerializeField] private Button registerButton;
        [SerializeField] private Button backButton;

        private void OnEnable()
        {
            SetBusy(false);
            ClearInlineStatuses();

            if (errorPopup != null)
                errorPopup.Hide();

            if (LocalPlayerData.Instance == null)
                LocalPlayerData.Load();

            if (nameInput != null &&
                LocalPlayerData.Instance != null &&
                !string.IsNullOrWhiteSpace(LocalPlayerData.Instance.LocalDisplayName))
            {
                nameInput.text = LocalPlayerData.Instance.LocalDisplayName;
            }

            if (emailInput != null &&
                LocalPlayerData.Instance != null &&
                !string.IsNullOrWhiteSpace(LocalPlayerData.Instance.Email))
            {
                emailInput.text = LocalPlayerData.Instance.Email;
            }
        }

        public async void OnRegisterClick()
        {
            ClearInlineStatuses();

            if (AuthServiceProvider.Auth == null)
            {
                ShowGlobalError("Сервис авторизации ещё не инициализирован.");
                return;
            }

            string username = nameInput != null ? nameInput.text.Trim() : string.Empty;
            string email = emailInput != null ? emailInput.text.Trim() : string.Empty;

            bool isValid = true;

            if (string.IsNullOrWhiteSpace(username))
            {
                SetInlineStatus(nameStatusText, "Введите имя пользователя.", true);
                isValid = false;
            }
            else if (username.Length < 3)
            {
                SetInlineStatus(nameStatusText, "Минимум 3 символа.", true);
                isValid = false;
            }
            else
            {
                SetInlineStatus(nameStatusText, "Имя выглядит корректно.", false);
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                SetInlineStatus(emailStatusText, "Введите email.", true);
                isValid = false;
            }
            else if (!IsEmailValid(email))
            {
                SetInlineStatus(emailStatusText, "Некорректный формат email.", true);
                isValid = false;
            }
            else
            {
                SetInlineStatus(emailStatusText, "Email выглядит корректно.", false);
            }

            if (!isValid)
                return;

            if (LocalPlayerData.Instance == null)
                LocalPlayerData.Load();

            SetBusy(true);

            try
            {
                AuthResult result;

                if (LocalPlayerData.Instance != null && LocalPlayerData.Instance.IsGuest)
                    result = await AuthServiceProvider.Auth.BeginGuestUpgradeAsync(email, username);
                else
                    result = await AuthServiceProvider.Auth.BeginRegistrationAsync(email, username);

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
                    VerificationPurpose.Registration,
                    typeof(RegisterScreen),
                    email,
                    username
                );

                ScreenRouter.Instance.Show<VerificationScreen>();
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                ShowGlobalError(Errors.FromException(ex, "Не удалось начать регистрацию. Проверь подключение к интернету и попробуй ещё раз."));
            }
            finally
            {
                SetBusy(false);
            }
        }

        public void OnBack()
        {
            ScreenRouter.Instance.Show<WelcomeScreen>();
        }

        private void SetInlineStatus(TMP_Text target, string message, bool isError)
        {
            if (target == null)
                return;

            target.gameObject.SetActive(true);
            target.text = message;
            target.color = isError
                ? new Color(0.85f, 0.2f, 0.2f)
                : new Color(0.2f, 0.7f, 0.3f);
        }

        private void ClearInlineStatuses()
        {
            ClearInlineStatus(nameStatusText);
            ClearInlineStatus(emailStatusText);
        }

        private void ClearInlineStatus(TMP_Text target)
        {
            if (target == null)
                return;

            target.text = string.Empty;
            target.gameObject.SetActive(false);
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

            if (nameInput != null)
                nameInput.interactable = !value;

            if (emailInput != null)
                emailInput.interactable = !value;

            if (registerButton != null)
                registerButton.interactable = !value;

            if (backButton != null)
                backButton.interactable = !value;
        }

        private bool IsEmailValid(string email)
        {
            try
            {
                var _ = new System.Net.Mail.MailAddress(email);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}