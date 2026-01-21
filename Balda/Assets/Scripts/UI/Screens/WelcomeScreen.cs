using UnityEngine;
using Assets.Scripts.Core;
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
            throw new NotImplementedException();
        }
    }
}
