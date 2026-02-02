using UnityEngine;
using Assets.Scripts.App;
using Assets.Scripts.UI.Screens.Main;
using System;

namespace Assets.Scripts.UI.Screens
{
    public class WelcomeScreen : ScreenBase
    {
        public void OnLoginClick()
        {
            ScreenRouter.Instance.Show<LoginScreen>();
        }
        public void OnGuestClick()
        {
            ScreenRouter.Instance.Show<MainScreen>();
        }
    }
}
