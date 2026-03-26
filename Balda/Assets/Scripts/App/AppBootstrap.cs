using System.Threading.Tasks;
using Assets.Scripts.Data;
using Assets.Scripts.LocalData;
using Assets.Scripts.UI.Screens;
using Assets.Scripts.UI.Screens.Main;
using UnityEngine;

namespace Assets.Scripts.App
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