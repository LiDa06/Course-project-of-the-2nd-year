using System.Net;
using System.Threading.Tasks;
using UnityEngine;

namespace Balda.Infrastructure.Server
{
    public class SupabaseManager : MonoBehaviour
    {
        public static Supabase.Client Instance { get; private set; }
        public static bool IsInitialized { get; private set; }

        [SerializeField] private string supabaseUrl;
        [SerializeField] private string supabaseAnonKey;

        private static SupabaseManager _self;

        private async void Awake()
        {
            if (_self != null && _self != this)
            {
                Destroy(gameObject);
                return;
            }

            _self = this;
            DontDestroyOnLoad(gameObject);

            if (Instance != null)
            {
                IsInitialized = true;
                return;
            }

            // Важно для старых/капризных TLS-сценариев в Unity
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            try
            {
                Debug.Log("SupabaseManager: creating client...");
                Instance = new Supabase.Client(supabaseUrl.Trim(), supabaseAnonKey.Trim());

                Debug.Log("SupabaseManager: initializing...");
                await Instance.InitializeAsync();

                IsInitialized = true;
                Debug.Log("Supabase initialized");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Supabase initialization failed:");
                Debug.LogError(ex.ToString());

                if (ex.InnerException != null)
                    Debug.LogError("Inner 1: " + ex.InnerException);

                if (ex.InnerException?.InnerException != null)
                    Debug.LogError("Inner 2: " + ex.InnerException.InnerException);
            }
        }

        public static async Task WaitUntilInitialized()
        {
            while (!IsInitialized)
                await Task.Yield();
        }
    }
}