using System;
using Assets.Scripts.Core;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.UI.Screens
{
    public class LoginScreen : ScreenBase
    {
        [SerializeField] private TMP_InputField nameInput;
        [SerializeField] private TMP_InputField emailInput;

        public void OnLoginClick()
        {
            SessionData.Name = nameInput.text;
            SessionData.Email = emailInput.text;

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
