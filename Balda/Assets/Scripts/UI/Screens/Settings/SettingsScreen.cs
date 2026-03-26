using Assets.Scripts.App;
using Assets.Scripts.Data;
using Assets.Scripts.LocalData;
using Assets.Scripts.Server.Services;
using Assets.Scripts.UI.Screens.Main;
using Assets.Scripts.UI.Screens.Verification;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.UI.Screens.Settings
{
    public class SettingsScreen : ScreenBase
    {
        [SerializeField] private SwitchThemeModeBox themeModeBox;
        [SerializeField] private SwitchVolumeBox volumeBox;
        [SerializeField] private TMP_Text nameText;

        private void OnEnable()
        {
            if (LocalPlayerData.Instance == null)
                LocalPlayerData.Load();

            if (LocalSettings.Instance == null)
                LocalSettings.Load();

            if (nameText != null)
                nameText.text = LocalPlayerData.Instance.LocalDisplayName;
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

        public void OnChangeNameClick()
        {
            ScreenRouter.Instance.Show<ChangeNameScreen>();
        }

        public void OnChangeEmailClick()
        {
            ScreenRouter.Instance.Show<ChangeEmailScreen>();
        }

        public async void OnResetStatisticClick()
        {
            if (AuthServiceProvider.Auth == null)
            {
                Debug.LogError("Сервис авторизации ещё не инициализирован.");
                return;
            }

            if (LocalPlayerData.Instance == null)
                LocalPlayerData.Load();

            var result = await AuthServiceProvider.Auth.BeginResetStatisticAsync();
            if (!result.Success)
            {
                Debug.LogError(result.Message);
                return;
            }

            var screen = ScreenRouter.Instance.GetScreen<VerificationScreen>();
            if (screen == null)
            {
                Debug.LogError("VerificationScreen не найден в ScreenRouter.");
                return;
            }

            screen.Setup(
                VerificationPurpose.ResetStatistic,
                typeof(SettingsScreen),
                LocalPlayerData.Instance.Email
            );

            ScreenRouter.Instance.Show<VerificationScreen>();
        }

        public async void OnDeleteAccountClick()
        {
            if (AuthServiceProvider.Auth == null)
            {
                Debug.LogError("Сервис авторизации ещё не инициализирован.");
                return;
            }

            if (LocalPlayerData.Instance == null)
                LocalPlayerData.Load();

            var result = await AuthServiceProvider.Auth.BeginDeleteAccountAsync();
            if (!result.Success)
            {
                Debug.LogError(result.Message);
                return;
            }

            var screen = ScreenRouter.Instance.GetScreen<VerificationScreen>();
            if (screen == null)
            {
                Debug.LogError("VerificationScreen не найден в ScreenRouter.");
                return;
            }

            screen.Setup(
                VerificationPurpose.DeleteAccount,
                typeof(SettingsScreen),
                LocalPlayerData.Instance.Email
            );

            ScreenRouter.Instance.Show<VerificationScreen>();
        }

        public void OnBack()
        {
            ScreenRouter.Instance.Show<MainScreen>();
        }
    }
}