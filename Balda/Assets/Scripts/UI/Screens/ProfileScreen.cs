using Assets.Scripts.LocalData;
using Assets.Scripts.App;
using TMPro;
using UnityEngine;
using Assets.Scripts.UI.Screens.Main;

namespace Assets.Scripts.UI.Screens
{
    public class ProfileScreen : ScreenBase
    {
        [SerializeField] private TMP_Text email;
        [SerializeField] private TMP_Text dateOfRegistration;
        [SerializeField] private TMP_Text timeInGame;

        private void OnEnable()
        {
            email.text = LocalPlayerData.Instance.Email;
            dateOfRegistration.text = LocalPlayerData.Instance.CreatedAtTicks.ToString();
            timeInGame.text = ""; //////////////////////////
        }
        public void OnBack()
        {
            ScreenRouter.Instance.Show<MainScreen>();
        }
    }
}
