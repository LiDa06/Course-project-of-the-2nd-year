using UnityEngine;
using Assets.Scripts.UI.Screens;
using Assets.Scripts.Data;
using Assets.Scripts.UI.Screens.Main;

namespace Assets.Scripts.App
{
    public class AppBootstrap : MonoBehaviour
    {
        void Start()
        {
            SettingsData.Load();
            UserData.Load();

            if (UserData.Instance.IsFirstLaunch)
            {
                ScreenRouter.Instance.Show<WelcomeScreen>();
            } else
            {
                ScreenRouter.Instance.Show<MainScreen>();
            }
        }
    }
}