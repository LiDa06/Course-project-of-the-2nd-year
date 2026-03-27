using Balda.Core.Navigation;
using Balda.Features.FriendGame.UI;
using Balda.Features.Game.UI;
using Balda.Features.Profile.UI;
using Balda.Features.Settings.UI;
using Balda.Infrastructure.LocalStorage;
using Balda.UI.Common;
using TMPro;
using UnityEngine;

namespace Balda.Features.MainMenu.UI
{
    public class MainScreen : ScreenBase
    {
        [SerializeField] private SliderLabelsAligner slider;
        [SerializeField] private TMP_Text wins;
        [SerializeField] private TMP_Text losses;
        [SerializeField] private TMP_Text persent;

        private void OnEnable()
        {
            if (LocalSettings.Instance == null)
                LocalSettings.Load();

            slider.RefreshFromSettings();

            wins.text = LocalPlayerData.Instance.Wins.ToString();
            losses.text = LocalPlayerData.Instance.Losses.ToString();
            persent.text = LocalPlayerData.Instance.GamePlayed == 0
                ? "0"
                : $"{LocalPlayerData.Instance.Wins * 100 / LocalPlayerData.Instance.GamePlayed}%";
        }

        public void UpdateFieldSize()
        {
            if (LocalSettings.Instance == null)
                LocalSettings.Load();

            LocalSettings.Instance.BoardSize = slider.GetFieldSize();
            LocalSettings.Save();

            Debug.Log($"Saved board size = {LocalSettings.Instance.BoardSize}");
        }

        public void OnPlayClick()
        {
            UpdateFieldSize();
            ScreenRouter.Instance.Show<GameScreen>();
        }

        public void OnPlayWithFriendClick()
        {
            UpdateFieldSize();
            ScreenRouter.Instance.Show<PlayWithFriendScreen>();
        }

        public void OnRulesClick()
        {
            ScreenRouter.Instance.Show<RulesScreen>();
        }

        public void OnProfileButtonClick()
        {
            ScreenRouter.Instance.Show<ProfileScreen>();
        }

        public void OnSettingsButtonClick()
        {
            ScreenRouter.Instance.Show<SettingsScreen>();
        }
    }
}