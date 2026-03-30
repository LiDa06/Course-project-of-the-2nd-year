using System;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;

namespace Balda.Infrastructure.Server
{
    public class SupabaseManager : MonoBehaviour
    {
        public static Supabase.Client Instance { get; private set; }
        public static bool IsInitialized { get; private set; }
        public static bool IsFailed { get; private set; }
        public static string InitializationError { get; private set; }

        public static bool IsReady => IsInitialized && Instance != null;

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
                IsFailed = false;
                InitializationError = null;
                return;
            }

            IsInitialized = false;
            IsFailed = false;
            InitializationError = null;

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            try
            {
                Debug.Log("SupabaseManager: creating client...");
                Instance = new Supabase.Client(supabaseUrl.Trim(), supabaseAnonKey.Trim());

                Debug.Log("SupabaseManager: initializing...");
                await Instance.InitializeAsync();

                IsInitialized = true;
                IsFailed = false;
                InitializationError = null;

                Debug.Log("Supabase initialized");
            }
            catch (Exception ex)
            {
                IsInitialized = false;
                IsFailed = true;
                InitializationError = ex.ToString();

                Debug.LogError("Supabase initialization failed:");
                Debug.LogError(ex);

                if (ex.InnerException != null)
                    Debug.LogError("Inner 1: " + ex.InnerException);

                if (ex.InnerException?.InnerException != null)
                    Debug.LogError("Inner 2: " + ex.InnerException.InnerException);
            }
        }

        public static async Task<bool> WaitUntilInitialized(float timeoutSeconds = 15f)
        {
            float startTime = Time.realtimeSinceStartup;

            while (!IsInitialized && !IsFailed)
            {
                if (Time.realtimeSinceStartup - startTime >= timeoutSeconds)
                {
                    InitializationError ??= $"Supabase initialization timeout after {timeoutSeconds} seconds.";
                    return false;
                }

                await Task.Yield();
            }

            return IsReady;
        }
    }
}