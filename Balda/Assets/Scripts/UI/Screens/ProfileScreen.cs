using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Scripts.Core;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.UI.Screens
{
    public class ProfileScreen : ScreenBase
    {
        [SerializeField] private TMP_Text email;
        [SerializeField] private TMP_Text dateOfRegistration;
        [SerializeField] private TMP_Text timeInGame;

        private void OnEnable()
        {
            email.text = SessionData.Email;
            dateOfRegistration.text = SessionData.RegistrationDate.ToString();
            timeInGame.text = SessionData.TimeInGame.ToString();
        }
        public void OnBack()
        {
            ScreenRouter.Instance.Show<MainScreen>();
        }
    }
}
