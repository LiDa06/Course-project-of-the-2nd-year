using System.Drawing;
using Assets.Scripts.LocalData;
using Assets.Scripts.App;
using TMPro;
using UnityEngine;
using Assets.Scripts.UI.Screens.Settings;
using UnityEngine.UI;

namespace Assets.Scripts.UI.Screens.Main
{
    public class MainScreen : ScreenBase
    {
        [SerializeField] private SliderLabelsAligner slider;
        [SerializeField] private TMP_Text wins;
        [SerializeField] private TMP_Text losses;
        [SerializeField] private TMP_Text persent;

        public void OnEnable()
        {
            slider.Start();
            wins.text = LocalPlayerData.Instance.Wins.ToString();
            losses.text = LocalPlayerData.Instance.Losses.ToString();
            persent.text = LocalPlayerData.Instance.GamePlayed == 0 ? "0" : 
                $"{LocalPlayerData.Instance.Wins * 100 / (LocalPlayerData.Instance.GamePlayed)}%";
        }

        public void UpdateFieldSize()
        {
            //SettingsData.SetBoardSize(slider.GetFieldSize());
        }
        public void OnPlayWithFriendClick()
        {
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
