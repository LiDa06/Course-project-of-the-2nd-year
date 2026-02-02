using System;
using Assets.Scripts.Data;
using Assets.Scripts.App;
using TMPro;
using UnityEngine;
using Assets.Scripts.UI.Screens.Verification;

namespace Assets.Scripts.UI.Screens
{
    public class LoginScreen : ScreenBase
    {
        [SerializeField] private TMP_InputField nameInput;
        [SerializeField] private TMP_InputField emailInput;

        public void OnLoginClick()
        {
            UserData.Instance.Name = nameInput.text;
            UserData.Instance.Email = emailInput.text;

            ScreenRouter.Instance.Show<VerificationScreen>();
        }
        public void OnBack()
        {
            nameInput.text = "";
            emailInput.text = "";
            ScreenRouter.Instance.Show<WelcomeScreen>();
        }
    }
}
