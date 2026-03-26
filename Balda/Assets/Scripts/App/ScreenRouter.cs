using UnityEngine;
using System;
using System.Collections.Generic;
using Assets.Scripts.UI.Screens;

namespace Assets.Scripts.App
{
    public class ScreenRouter : MonoBehaviour
    {
        public static ScreenRouter Instance;

        [SerializeField] private Transform screensRoot;

        private readonly Dictionary<Type, ScreenBase> screens = new();
        private readonly Stack<Type> history = new();

        private Type currentScreenType;
        private Type previousScreenType;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            foreach (var screen in screensRoot.GetComponentsInChildren<ScreenBase>(true))
            {
                screens[screen.GetType()] = screen;
                screen.gameObject.SetActive(false);
            }
        }

        public void Show<T>(bool rememberInHistory = true) where T : ScreenBase
        {
            Show(typeof(T), rememberInHistory);
        }

        public void Show(Type screenType, bool rememberInHistory = true)
        {
            if (!screens.ContainsKey(screenType))
            {
                Debug.LogError($"ScreenRouter: экран типа {screenType.Name} не найден.");
                return;
            }

            if (currentScreenType != null && rememberInHistory)
            {
                history.Push(currentScreenType);
                previousScreenType = currentScreenType;
            }

            foreach (var s in screens.Values)
                s.gameObject.SetActive(false);

            currentScreenType = screenType;

            screens[screenType].gameObject.SetActive(true);
            screens[screenType].OnShow();
        }

        public void ShowWithoutHistory<T>() where T : ScreenBase
        {
            Show(typeof(T), false);
        }

        public void ShowWithoutHistory(Type screenType)
        {
            Show(screenType, false);
        }

        public bool CanGoBack()
        {
            return history.Count > 0;
        }

        public void Back()
        {
            if (history.Count == 0)
            {
                Debug.LogWarning("ScreenRouter: история экранов пуста.");
                return;
            }

            var target = history.Pop();

            foreach (var s in screens.Values)
                s.gameObject.SetActive(false);

            previousScreenType = currentScreenType;
            currentScreenType = target;

            screens[target].gameObject.SetActive(true);
            screens[target].OnShow();
        }

        public T GetScreen<T>() where T : ScreenBase
        {
            if (!screens.TryGetValue(typeof(T), out var screen))
                return null;

            return screen as T;
        }

        public ScreenBase GetCurrentScreen()
        {
            if (currentScreenType == null)
                return null;

            return screens[currentScreenType];
        }

        public Type GetCurrentScreenType()
        {
            return currentScreenType;
        }

        public Type GetPreviousScreenType()
        {
            return previousScreenType;
        }

        public bool IsCurrent<T>() where T : ScreenBase
        {
            return currentScreenType == typeof(T);
        }

        public void ClearHistory()
        {
            history.Clear();
            previousScreenType = null;
        }
    }
}