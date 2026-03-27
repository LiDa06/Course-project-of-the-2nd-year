using System.Threading.Tasks;
using Balda.Features.Auth.UI;
using Balda.Core.Navigation;
using Balda.Infrastructure.Server;
using Balda.Infrastructure.LocalStorage;
using Balda.Infrastructure.Audio;
using Balda.Infrastructure.Theme;
using UnityEngine;

namespace Balda.Core.Bootstrap
{
    public class AppBootstrap : MonoBehaviour
    {
        private async void Start()
        {
            LocalSettings.Load();
            ThemeManager.Instance.Apply(LocalSettings.Instance.Theme);
            AudioManager.Instance.Apply(LocalSettings.Instance.Audio);
            LocalPlayerData.Load();

            await SupabaseManager.WaitUntilInitialized();

            ScreenRouter.Instance.Show<WelcomeScreen>();

            /*
            if (LocalPlayerData.Instance.IsFirstLaunch)
            {
                ScreenRouter.Instance.Show<WelcomeScreen>();
            }
            else
            {
                ScreenRouter.Instance.Show<MainScreen>();
            }
            */
        }
    }
}