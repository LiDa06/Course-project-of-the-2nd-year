using Balda.UI.Common;
using Balda.Core.Navigation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Balda.Infrastructure.LocalStorage;
using Balda.Infrastructure.Audio;
using Balda.Infrastructure.Theme;
using AudioType = Balda.Infrastructure.Audio.AudioType;
using ThemeType = Balda.Infrastructure.Theme.ThemeType;
using Balda.Infrastructure.Server.Auth;
using Balda.Features.Auth.UI;
using Balda.Features.MainMenu.UI;

namespace Balda.Features.Settings.UI
{
    public class SettingsScreen : ScreenBase
    {
        [Header("Common settings")]
        [SerializeField] private SwitchThemeModeBox themeModeBox;
        [SerializeField] private SwitchVolumeBox volumeBox;
        [SerializeField] private TMP_Dropdown botDifficultyDropdown;

        [Header("Account info")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text accountHintText;

        [Header("Account buttons")]
        [SerializeField] private Button changeNameButton;
        [SerializeField] private Button changeEmailButton;
        [SerializeField] private Button resetStatisticButton;
        [SerializeField] private Button deleteAccountButton;

        [Header("Overlay / popups")]
        [SerializeField] private GameObject busyOverlay;
        [SerializeField] private MessagePopup messagePopup;
        [SerializeField] private ConfirmationPopup confirmationPopup;

        private bool _busy;

        private void OnEnable()
        {
            if (LocalPlayerData.Instance == null)
                LocalPlayerData.Load();

            if (LocalSettings.Instance == null)
                LocalSettings.Load();

            if (messagePopup != null)
                messagePopup.Hide();

            if (confirmationPopup != null)
                confirmationPopup.Hide();

            SetBusy(false);
            RefreshAccountView();
            SetupDifficultyDropdown();
        }

        private void RefreshAccountView()
        {
            var local = LocalPlayerData.Instance;
            bool registered = HasLocalRegisteredAccount();

            if (nameText != null)
            {
                string displayName = local != null && !string.IsNullOrWhiteSpace(local.LocalDisplayName)
                    ? local.LocalDisplayName
                    : "Guest";

                nameText.text = displayName;
            }

            if (accountHintText != null)
            {
                accountHintText.gameObject.SetActive(!registered);
                accountHintText.text = "В гостевом режиме доступны игра и локальная статистика. " +
                                       "Смена имени, почты и удаление аккаунта доступны после регистрации.";
            }

            ApplyButtonAvailability();
        }

        private void ApplyButtonAvailability()
        {
            bool registered = HasLocalRegisteredAccount();

            if (changeNameButton != null)
                changeNameButton.interactable = !_busy /*&& registered*/;

            if (changeEmailButton != null)
                changeEmailButton.interactable = !_busy /*&& registered*/;

            // Сброс статистики доступен и гостю, и зарегистрированному пользователю:
            // у гостя сбрасываются локальные данные, у аккаунта — локальные + серверные через код.
            if (resetStatisticButton != null)
                resetStatisticButton.interactable = !_busy;

            if (deleteAccountButton != null)
                deleteAccountButton.interactable = !_busy /*&& registered*/;
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

        private void SetupDifficultyDropdown()
        {
            if (botDifficultyDropdown == null)
                return;

            botDifficultyDropdown.onValueChanged.RemoveListener(OnBotDifficultyChanged);

            if (botDifficultyDropdown.options == null || botDifficultyDropdown.options.Count == 0)
            {
                botDifficultyDropdown.ClearOptions();
                botDifficultyDropdown.AddOptions(new System.Collections.Generic.List<string>
                {
                    "Лёгкий",
                    "Средний",
                    "Сложный"
                });
            }

            int index = GetDifficultyIndex(LocalSettings.Instance != null
                ? LocalSettings.Instance.BotDifficulty
                : "easy");

            botDifficultyDropdown.SetValueWithoutNotify(index);
            botDifficultyDropdown.onValueChanged.AddListener(OnBotDifficultyChanged);
        }

        public void ThemeModeClick()
        {
            if (LocalSettings.Instance == null)
                LocalSettings.Load();

            themeModeBox?.OnThemeChanged();

            ThemeType newTheme = LocalSettings.Instance.Theme == ThemeType.Light
                ? ThemeType.Dark
                : ThemeType.Light;

            LocalSettings.Instance.Theme = newTheme;
            LocalSettings.Save();

            if (ThemeManager.Instance != null)
                ThemeManager.Instance.Apply(newTheme);
        }

        public void VolumeClick()
        {
            if (LocalSettings.Instance == null)
                LocalSettings.Load();

            volumeBox?.OnAudioChanged();

            AudioType newAudio = LocalSettings.Instance.Audio == AudioType.On
                ? AudioType.Off
                : AudioType.On;

            LocalSettings.Instance.Audio = newAudio;
            LocalSettings.Save();

            if (AudioManager.Instance != null)
                AudioManager.Instance.Apply(newAudio);
        }

        public void OnBotDifficultyChanged(int index)
        {
            if (LocalSettings.Instance == null)
                LocalSettings.Load();

            LocalSettings.Instance.BotDifficulty = GetDifficultyKey(index);
            LocalSettings.Save();

            Debug.Log($"Bot difficulty changed to: {LocalSettings.Instance.BotDifficulty}");
        }

        public void OnChangeNameClick()
        {
            if (!HasLocalRegisteredAccount())
            {
                ShowMessage("Недоступно", "Смена имени доступна только для зарегистрированного аккаунта.");
                return;
            }

            ScreenRouter.Instance.Show<ChangeNameScreen>();
        }

        public void OnChangeEmailClick()
        {
            if (!HasLocalRegisteredAccount())
            {
                ShowMessage("Недоступно", "Смена почты доступна только для зарегистрированного аккаунта.");
                return;
            }

            ScreenRouter.Instance.Show<ChangeEmailScreen>();
        }

        public void OnResetStatisticClick()
        {
            if (LocalPlayerData.Instance == null)
                LocalPlayerData.Load();

            if (LocalPlayerData.Instance == null)
            {
                ShowError("Локальные данные игрока не найдены.");
                return;
            }

            if (!HasLocalRegisteredAccount())
            {
                ShowConfirmation(
                    "Сброс статистики",
                    "Сбросить локальную статистику гостевого профиля? Это действие нельзя отменить.",
                    ResetGuestStatistic,
                    "Сбросить");
                return;
            }

            ShowConfirmation(
                "Сброс статистики",
                "На почту аккаунта будет отправлен код подтверждения. После подтверждения статистика и список последних игр будут сброшены локально и на сервере.",
                BeginRegisteredStatisticReset,
                "Отправить код");
        }

        public void OnDeleteAccountClick()
        {
            if (!HasLocalRegisteredAccount())
            {
                ShowMessage("Недоступно", "Удаление аккаунта доступно только для зарегистрированного пользователя.");
                return;
            }

            ShowConfirmation(
                "Удаление аккаунта",
                "На почту аккаунта будет отправлен код подтверждения. После подтверждения аккаунт будет удалён, а приложение вернётся к стартовому экрану.",
                BeginRegisteredAccountDelete,
                "Отправить код");
        }

        private void ResetGuestStatistic()
        {
            if (LocalPlayerData.Instance == null)
                LocalPlayerData.Load();

            if (LocalPlayerData.Instance == null)
            {
                ShowError("Локальные данные игрока не найдены.");
                return;
            }

            LocalPlayerData.Instance.ResetStats();
            RefreshAccountView();
            ShowMessage("Готово", "Локальная статистика гостевого профиля сброшена.");
        }

        private async void BeginRegisteredStatisticReset()
        {
            if (AuthServiceProvider.Auth == null)
            {
                ShowError("Сервис авторизации ещё не инициализирован.");
                return;
            }

            if (!HasLocalRegisteredAccount())
            {
                ShowMessage("Недоступно", "Сброс серверной статистики доступен только для зарегистрированного аккаунта.");
                return;
            }

            SetBusy(true);

            try
            {
                var result = await AuthServiceProvider.Auth.BeginResetStatisticAsync();
                if (!result.Success)
                {
                    ShowError(Errors.ForPopup(result.Message));
                    return;
                }

                var screen = ScreenRouter.Instance.GetScreen<VerificationScreen>();
                if (screen == null)
                {
                    ShowError("Экран подтверждения кода не найден.");
                    return;
                }

                screen.Setup(
                    VerificationPurpose.ResetStatistic,
                    typeof(SettingsScreen),
                    GetAccountEmailForVerification()
                );

                ScreenRouter.Instance.Show<VerificationScreen>();
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                ShowError(Errors.FromException(ex, "Не удалось отправить код для сброса статистики. Попробуй ещё раз."));
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void BeginRegisteredAccountDelete()
        {
            if (AuthServiceProvider.Auth == null)
            {
                ShowError("Сервис авторизации ещё не инициализирован.");
                return;
            }

            if (!HasLocalRegisteredAccount())
            {
                ShowMessage("Недоступно", "Удаление аккаунта доступно только для зарегистрированного пользователя.");
                return;
            }

            SetBusy(true);

            try
            {
                var result = await AuthServiceProvider.Auth.BeginDeleteAccountAsync();
                if (!result.Success)
                {
                    ShowError(Errors.ForPopup(result.Message));
                    return;
                }

                var screen = ScreenRouter.Instance.GetScreen<VerificationScreen>();
                if (screen == null)
                {
                    ShowError("Экран подтверждения кода не найден.");
                    return;
                }

                screen.Setup(
                    VerificationPurpose.DeleteAccount,
                    typeof(SettingsScreen),
                    GetAccountEmailForVerification()
                );

                ScreenRouter.Instance.Show<VerificationScreen>();
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                ShowError(Errors.FromException(ex, "Не удалось отправить код для удаления аккаунта. Попробуй ещё раз."));
            }
            finally
            {
                SetBusy(false);
            }
        }

        public void OnBack()
        {
            ScreenRouter.Instance.Show<MainScreen>();
        }

        private string GetAccountEmailForVerification()
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

        private void SetBusy(bool value)
        {
            _busy = value;

            if (busyOverlay != null)
                busyOverlay.SetActive(value);

            if (botDifficultyDropdown != null)
                botDifficultyDropdown.interactable = !value;

            ApplyButtonAvailability();
        }

        private void ShowConfirmation(string title, string message, System.Action onConfirm, string confirmText)
        {
            if (confirmationPopup != null)
            {
                confirmationPopup.Show(title, message, onConfirm, null, confirmText, "Отмена");
                return;
            }

            Debug.LogError("ConfirmationPopup не назначен в SettingsScreen. Опасное действие отменено: " + title);
            ShowError("Окно подтверждения не настроено. Действие отменено.");
        }

        private void ShowMessage(string title, string message)
        {
            string friendlyMessage = Errors.ForPopup(message, message);

            if (messagePopup != null)
                messagePopup.Show(title, friendlyMessage);
            else
                Debug.Log(friendlyMessage);
        }

        private void ShowError(string message)
        {
            string friendlyMessage = Errors.ForPopup(message);

            if (messagePopup != null)
                messagePopup.Show("Ошибка", friendlyMessage);
            else
                Debug.LogError(friendlyMessage);
        }

        private static int GetDifficultyIndex(string difficultyKey)
        {
            if (string.IsNullOrWhiteSpace(difficultyKey))
                return 0;

            switch (difficultyKey.Trim().ToLowerInvariant())
            {
                case "medium":
                    return 1;

                case "hard":
                    return 2;

                default:
                    return 0;
            }
        }

        private static string GetDifficultyKey(int index)
        {
            switch (index)
            {
                case 1:
                    return "medium";

                case 2:
                    return "hard";

                default:
                    return "easy";
            }
        }
    }
}
