using UnityEngine;
using Assets.Scripts.UI.Screens;

namespace Assets.Scripts.Core
{
    public class AppBootstrap : MonoBehaviour
    {
        void Start()
        {
            ScreenRouter.Instance.Show<WelcomeScreen>();
        }
    }
}