using UnityEngine;
using TMPro;
using Assets.Scripts.Core;

namespace Assets.Scripts.UI.Screens
{
    public class LoginScreen : ScreenBase
    {
        public TMP_InputField nameInput;
        public TMP_InputField emailInput;

        public void OnBack()
        {
            ScreenRouter.Instance.Show<WelcomeScreen>();
        }
    }
}
