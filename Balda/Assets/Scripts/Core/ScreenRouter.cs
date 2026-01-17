using UnityEngine;
using System;
using System.Collections.Generic;
using Assets.Scripts.UI.Screens;

namespace Assets.Scripts.Core
{
    public class ScreenRouter : MonoBehaviour
    {
        public static ScreenRouter Instance;

        [SerializeField] Transform screensRoot;

        Dictionary<Type, ScreenBase> screens = new();

        void Awake()
        {
            Instance = this;

            foreach (var screen in screensRoot.GetComponentsInChildren<ScreenBase>(true))
            {
                screens[screen.GetType()] = screen;
                screen.gameObject.SetActive(false);
            }

            Show<WelcomeScreen>();
        }

        public void Show<T>() where T : ScreenBase
        {
            foreach (var s in screens.Values)
                s.gameObject.SetActive(false);

            screens[typeof(T)].gameObject.SetActive(true);
            screens[typeof(T)].OnShow();
        }
    }
}