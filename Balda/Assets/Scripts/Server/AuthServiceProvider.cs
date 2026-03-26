using Assets.Scripts.App;
using UnityEngine;

namespace Assets.Scripts.Server.Services
{
    public class AuthServiceProvider : MonoBehaviour
    {
        public static SupabaseAuthService Auth { get; private set; }

        private async void Awake()
        {
            await SupabaseManager.WaitUntilInitialized();
            Auth = new SupabaseAuthService(SupabaseManager.Instance);
        }
    }
}