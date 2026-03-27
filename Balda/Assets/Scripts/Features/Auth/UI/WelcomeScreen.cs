using UnityEngine;
using Balda.UI.Common;
using Balda.Core.Navigation;
using Balda.Features.MainMenu.UI;

namespace Balda.Features.Auth.UI
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
