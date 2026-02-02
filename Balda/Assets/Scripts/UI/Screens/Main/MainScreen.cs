using System.Drawing;
using Assets.Scripts.Data;
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
            wins.text = UserData.Instance.Wins.ToString();
            losses.text = UserData.Instance.Losses.ToString();
            persent.text = UserData.Instance.GamePlayed == 0 ? "0" : 
                $"{UserData.Instance.Wins * 100 / (UserData.Instance.GamePlayed)}%";
        }

        public void UpdateFieldSize()
        {
            SettingsData.SetBoardSize(slider.GetFieldSize());
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
