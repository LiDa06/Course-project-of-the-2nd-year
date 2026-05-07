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
    public class ChangeNameScreen : ScreenBase
    {
        [Header("Input")]
        [SerializeField] private TMP_InputField nameInput;

        [Header("Inline status")]
        [SerializeField] private TMP_Text nameStatusText;

        [Header("Overlay / popup")]
        [SerializeField] private GameObject busyOverlay;
        [SerializeField] private MessagePopup errorPopup;

        [Header("Buttons")]
        [SerializeField] private Button saveButton;
        [SerializeField] private Button cancelButton;

        private void OnEnable()
        {
            if (LocalPlayerData.Instance == null)
                LocalPlayerData.Load();

            if (nameInput != null)
            {
                if (PendingAccountAction.HasChangeName && !string.IsNullOrWhiteSpace(PendingAccountAction.NewUsername))
                {
                    nameInput.text = PendingAccountAction.NewUsername;
                }
                else
                {
                    nameInput.text = LocalPlayerData.Instance != null
                        ? LocalPlayerData.Instance.LocalDisplayName
                        : string.Empty;
                }
            }

            SetBusy(false);
            ClearInlineStatus();

            if (errorPopup != null)
                errorPopup.Hide();
        }

        public async void OnSaveClick()
        {
            ClearInlineStatus();

            string newName = nameInput != null
                ? nameInput.text.Trim()
                : string.Empty;

            if (!ValidateName(newName))
                return;

            if (LocalPlayerData.Instance == null)
                LocalPlayerData.Load();

            if (!HasLocalRegisteredAccount())
            {
                ShowGlobalError("Смена имени доступна только для зарегистрированного аккаунта.");
                return;
            }

            if (AuthServiceProvider.Auth == null)
            {
                ShowGlobalError("Сервис авторизации временно недоступен.");
                return;
            }

            SetInlineStatus("Имя выглядит корректно.", false);
            SetBusy(true);

            try
            {
                if (!AuthServiceProvider.Auth.IsSignedIn)
                {
                    await StartLoginConfirmationForPendingNameAsync(newName);
                    return;
                }

                await SaveNameToServerAsync(newName);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                ShowGlobalError(Errors.FromException(ex, "Не удалось изменить имя. Попробуй ещё раз."));
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

        private bool ValidateName(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
            {
                SetInlineStatus("Введите имя пользователя.", true);
                return false;
            }

            if (newName.Length < 3)
            {
                SetInlineStatus("Минимум 3 символа.", true);
                return false;
            }

            if (newName.Length > 24)
            {
                SetInlineStatus("Максимум 24 символа.", true);
                return false;
            }

            return true;
        }

        private async Task StartLoginConfirmationForPendingNameAsync(string newName)
        {
            string email = GetAccountEmailForVerification();
            if (string.IsNullOrWhiteSpace(email))
            {
                ShowGlobalError("Не удалось определить email аккаунта. Войди в аккаунт заново.");
                return;
            }

            PendingAccountAction.SetChangeName(newName);

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
                typeof(ChangeNameScreen),
                email
            );

            ScreenRouter.Instance.Show<VerificationScreen>();
        }

        private async Task SaveNameToServerAsync(string newName)
        {
            var result = await AuthServiceProvider.Auth.ChangeUsernameAsync(newName);
            if (!result.Success)
            {
                ShowGlobalError(Errors.ForPopup(result.Message));
                return;
            }

            PendingAccountAction.Clear();

            if (LocalPlayerData.Instance != null)
            {
                LocalPlayerData.Instance.IsGuest = false;
                LocalPlayerData.Instance.LocalDisplayName = newName;
                LocalPlayerData.Save();
            }

            await Task.Delay(200);
            ScreenRouter.Instance.Show<SettingsScreen>();
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

            return email ?? string.Empty;
        }

        private void SetInlineStatus(string message, bool isError)
        {
            if (nameStatusText == null)
                return;

            nameStatusText.gameObject.SetActive(true);
            nameStatusText.text = message;
            nameStatusText.color = isError
                ? new Color(0.85f, 0.2f, 0.2f)
                : new Color(0.2f, 0.7f, 0.3f);
        }

        private void ClearInlineStatus()
        {
            if (nameStatusText == null)
                return;

            nameStatusText.text = string.Empty;
            nameStatusText.gameObject.SetActive(false);
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

            if (saveButton != null)
                saveButton.interactable = !value;

            if (cancelButton != null)
                cancelButton.interactable = !value;
        }
    }
}
