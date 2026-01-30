using System.Drawing;
using Assets.Scripts.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI.Screens
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
            wins.text = SessionData.numberOfWins.ToString();
            losses.text = SessionData.numberOfLoss.ToString();
            persent.text = SessionData.numberOfGames == 0 ? "0" : 
                $"{SessionData.numberOfWins * 100 / (SessionData.numberOfGames)}%";
        }

        public void UpdateFieldSize()
        {
            SessionData.FieldSize = slider.GetFieldSize();
        }

        public void ClickOnProfileButton()
        {
            ScreenRouter.Instance.Show<ProfileScreen>();
        }
    }
}
