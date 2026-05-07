using Balda.Core.Navigation;
using Balda.Infrastructure.Server.Auth;
using Balda.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Balda.Features.Auth.UI
{
    public class LoginScreen : ScreenBase
    {
        [Header("Input")]
        [SerializeField] private TMP_InputField emailInput;

        [Header("Inline status")]
        [SerializeField] private TMP_Text emailStatusText;

        [Header("Overlay / popup")]
        [SerializeField] private GameObject busyOverlay;
        [SerializeField] private MessagePopup errorPopup;

        [Header("Buttons")]
        [SerializeField] private Button loginButton;
        [SerializeField] private Button registerButton;
        [SerializeField] private Button backButton;

        private void OnEnable()
        {
            SetBusy(false);
            ClearInlineStatus();

            if (errorPopup != null)
                errorPopup.Hide();
        }

        public void OnRegisterClick()
        {
            ScreenRouter.Instance.Show<RegisterScreen>();
        }

        public async void OnLoginClick()
        {
            ClearInlineStatus();

            if (AuthServiceProvider.Auth == null)
            {
                ShowGlobalError("Сервис авторизации ещё не инициализирован.");
                return;
            }

            string email = emailInput != null ? emailInput.text.Trim() : string.Empty;

            if (string.IsNullOrWhiteSpace(email))
            {
                SetInlineStatus("Введите email.", true);
                return;
            }

            if (!IsEmailValid(email))
            {
                SetInlineStatus("Некорректный формат email.", true);
                return;
            }

            SetInlineStatus("Email выглядит корректно.", false);
            SetBusy(true);

            try
            {
                var result = await AuthServiceProvider.Auth.BeginLoginAsync(email);

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
                    typeof(LoginScreen),
                    email
                );

                ScreenRouter.Instance.Show<VerificationScreen>();
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                ShowGlobalError(Errors.FromException(ex, "Не удалось начать вход. Проверь интернет и попробуй ещё раз."));
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

            if (emailInput != null)
                emailInput.interactable = !value;

            if (loginButton != null)
                loginButton.interactable = !value;

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