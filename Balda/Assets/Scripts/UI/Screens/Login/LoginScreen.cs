using Assets.Scripts.App;
using Assets.Scripts.LocalData;
using Assets.Scripts.Server.Services;
using Assets.Scripts.UI.Screens.Verification;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.UI.Screens.Login
{
    public class LoginScreen : ScreenBase
    {
        [SerializeField] private TMP_InputField nameInput;
        [SerializeField] private TMP_InputField emailInput;

        public async void OnLoginClick()
        {
            if (AuthServiceProvider.Auth == null)
            {
                Debug.LogError("Сервис авторизации ещё не инициализирован.");
                return;
            }

            string username = nameInput != null ? nameInput.text.Trim() : string.Empty;
            string email = emailInput != null ? emailInput.text.Trim() : string.Empty;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email))
            {
                Debug.LogError("Введите логин и email");
                return;
            }

            if (LocalPlayerData.Instance == null)
                LocalPlayerData.Load();

            LocalPlayerData.Instance.LocalDisplayName = username;
            LocalPlayerData.Instance.Email = email;
            LocalPlayerData.Save();

            var result = await AuthServiceProvider.Auth.BeginEmailAuthAsync(email, username);

            if (!result.Success)
            {
                Debug.LogError(result.Message);
                return;
            }

            var screen = ScreenRouter.Instance.GetScreen<VerificationScreen>();
            if (screen == null)
            {
                Debug.LogError("VerificationScreen не найден.");
                return;
            }

            screen.Setup(
                VerificationPurpose.Registration,
                typeof(LoginScreen),
                email,
                username
            );

            ScreenRouter.Instance.Show<VerificationScreen>();
        }

        public void OnBack()
        {
            if (LocalPlayerData.Instance == null)
                LocalPlayerData.Load();

            LocalPlayerData.Instance.LocalDisplayName = "Guest";
            LocalPlayerData.Instance.Email = "";
            LocalPlayerData.Save();
            ScreenRouter.Instance.Show<WelcomeScreen>();
        }
    }
}