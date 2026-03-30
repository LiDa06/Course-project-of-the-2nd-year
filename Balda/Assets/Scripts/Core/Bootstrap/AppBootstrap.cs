using Balda.Core.Navigation;
using Balda.Features.Auth.UI;
using Balda.Infrastructure.Audio;
using Balda.Infrastructure.LocalStorage;
using Balda.Infrastructure.Theme;
using Balda.Infrastructure.Server;
using UnityEngine;

namespace Balda.Core.Bootstrap
{
    public class AppBootstrap : MonoBehaviour
    {
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

            ScreenRouter.Instance.Show<WelcomeScreen>();
        }
    }
}