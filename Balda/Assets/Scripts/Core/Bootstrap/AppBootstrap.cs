using System;
using System.Threading.Tasks;
using Balda.Core.Navigation;
using Balda.Features.Auth.UI;
using Balda.Features.MainMenu.UI;
using Balda.Infrastructure.Audio;
using Balda.Infrastructure.LocalStorage;
using Balda.Infrastructure.Theme;
using Balda.Infrastructure.Server;
using UnityEngine;

namespace Balda.Core.Bootstrap
{
    public class AppBootstrap : MonoBehaviour
    {
        [Header("Startup routing")]
        [Tooltip("Если зарегистрированный пользователь сохранён локально, но Supabase-сессия не восстановилась или сервер недоступен, открыть главное меню по локальным данным.")]
        [SerializeField] private bool allowRegisteredUserOfflineStart = true;

        [Tooltip("Сколько секунд ждать появления восстановленной Supabase-сессии после инициализации клиента.")]
        [SerializeField, Min(0f)] private float authSessionWaitSeconds = 2f;

        private async void Start()
        {
            LocalSettings.Load();
            LocalPlayerData.Load();

            ThemeManager.Instance.Apply(LocalSettings.Instance.Theme);
            AudioManager.Instance.Apply(LocalSettings.Instance.Audio);

            bool supabaseReady = await SupabaseManager.WaitUntilInitialized(15f);

            if (!supabaseReady)
            {
                Debug.LogWarning(
                    "Supabase недоступен на старте. " +
                    "Приложение продолжит работу с локальными данными. " +
                    $"Причина: {SupabaseManager.InitializationError}"
                );
            }

            await OpenStartScreenAsync(supabaseReady);
        }

        private async Task OpenStartScreenAsync(bool supabaseReady)
        {
            if (LocalPlayerData.Instance == null)
                LocalPlayerData.Load();

            LocalPlayerData local = LocalPlayerData.Instance;

            // Гость всегда начинает со стартового экрана. Поэтому если пользователь
            // в прошлом запуске выбрал "Войти как гость", при новом запуске он снова
            // увидит WelcomeScreen, а не главное меню.
            if (local == null || local.IsGuest || string.IsNullOrWhiteSpace(local.CloudUserId))
            {
                ShowWelcome();
                return;
            }

            // Локально сохранён зарегистрированный пользователь. Если сервер недоступен,
            // открываем главное меню в офлайн-режиме: одиночная игра и локальная статистика
            // остаются доступны, а серверные действия покажут ошибку/дождутся синхронизации.
            if (!supabaseReady || SupabaseManager.Instance == null)
            {
                if (allowRegisteredUserOfflineStart)
                {
                    Debug.LogWarning("AppBootstrap: сервер недоступен, сохранённый зарегистрированный пользователь открыт в офлайн-режиме.");
                    ShowMain();
                }
                else
                {
                    ShowWelcome();
                }

                return;
            }

            // Supabase SDK обычно восстанавливает текущую сессию при InitializeAsync(),
            // но на некоторых устройствах CurrentUser появляется на кадр/несколько кадров позже.
            await WaitForRestoredAuthSessionAsync(authSessionWaitSeconds);

            string currentUserId = SupabaseManager.Instance.Auth?.CurrentUser?.Id;

            // Если Supabase-сессия восстановилась и относится к тому же пользователю,
            // сразу открываем главное меню.
            if (!string.IsNullOrWhiteSpace(currentUserId) &&
                string.Equals(currentUserId, local.CloudUserId, StringComparison.OrdinalIgnoreCase))
            {
                ShowMain();
                return;
            }

            // Если локально есть зарегистрированный пользователь, но Supabase не восстановил
            // сессию, локальные данные не стираем. Открываем главное меню, чтобы выполнить
            // требование: зарегистрированный пользователь при следующем запуске сразу
            // попадает в приложение. Серверные действия при отсутствии активной сессии
            // покажут ошибку авторизации и попросят войти заново.
            Debug.LogWarning(
                "AppBootstrap: локально найден зарегистрированный пользователь, " +
                "но активная Supabase-сессия не восстановлена. Главное меню открыто по локальным данным."
            );

            if (allowRegisteredUserOfflineStart)
                ShowMain();
            else
                ShowWelcome();
        }

        private static async Task WaitForRestoredAuthSessionAsync(float timeoutSeconds)
        {
            if (SupabaseManager.Instance == null || timeoutSeconds <= 0f)
                return;

            float startTime = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup - startTime < timeoutSeconds)
            {
                if (SupabaseManager.Instance.Auth?.CurrentUser != null)
                    return;

                await Task.Yield();
            }
        }

        private static void ShowWelcome()
        {
            if (ScreenRouter.Instance == null)
                return;

            ScreenRouter.Instance.ClearHistory();
            ScreenRouter.Instance.ShowWithoutHistory<WelcomeScreen>();
        }

        private static void ShowMain()
        {
            if (ScreenRouter.Instance == null)
                return;

            ScreenRouter.Instance.ClearHistory();
            ScreenRouter.Instance.ShowWithoutHistory<MainScreen>();
        }
    }
}
