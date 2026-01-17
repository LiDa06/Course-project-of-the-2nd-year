using UnityEngine;
using Assets.Scripts.Core;

namespace Assets.Scripts.UI.Screens
{
    public class WelcomeScreen : ScreenBase
    {
        public void OnLoginClick()
        {
            ScreenRouter.Instance.Show<LoginScreen>();
        }
    }
}
