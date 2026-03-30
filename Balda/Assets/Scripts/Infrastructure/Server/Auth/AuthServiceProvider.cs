using UnityEngine;

namespace Balda.Infrastructure.Server.Auth
{
    public class AuthServiceProvider : MonoBehaviour
    {
        public static SupabaseAuthService Auth { get; private set; }
        public static bool IsReady => Auth != null;

        private async void Awake()
        {
            bool ready = await SupabaseManager.WaitUntilInitialized(15f);

            if (!ready || SupabaseManager.Instance == null)
            {
                Debug.LogWarning(
                    "AuthServiceProvider: Supabase не инициализирован. " +
                    "Авторизация временно недоступна."
                );
                return;
            }

            Auth ??= new SupabaseAuthService(SupabaseManager.Instance);
        }
    }
}