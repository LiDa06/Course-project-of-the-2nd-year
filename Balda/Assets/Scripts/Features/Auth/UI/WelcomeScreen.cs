using Balda.Core.Navigation;
using Balda.Features.MainMenu.UI;
using Balda.Infrastructure.LocalStorage;
using Balda.UI.Common;
using UnityEngine;

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
            if (LocalPlayerData.Instance == null)
                LocalPlayerData.Load();

            LocalPlayerData.Instance.SetGuest();
            ScreenRouter.Instance.Show<MainScreen>();
        }
    }
}
