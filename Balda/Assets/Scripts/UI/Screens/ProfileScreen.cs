using Assets.Scripts.Data;
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
            email.text = UserData.Instance.Email;
            dateOfRegistration.text = UserData.Instance.RegistrationDate.ToString();
            timeInGame.text = ""; //////////////////////////
        }
        public void OnBack()
        {
            ScreenRouter.Instance.Show<MainScreen>();
        }
    }
}
