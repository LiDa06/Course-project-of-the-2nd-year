using System;
using System.Threading.Tasks;
using Supabase.Gotrue;
using UnityEngine;
using Balda.Infrastructure.Server.Models;
using Balda.Infrastructure.Server.Profile;
using Balda.Infrastructure.Server.Stats;
using Balda.Infrastructure.LocalStorage;
using static Supabase.Gotrue.Constants;

namespace Balda.Infrastructure.Server.Auth
{
    public class SupabaseAuthService
    {
        private readonly Supabase.Client _client;
        private readonly ProfileService _profileService;
        private readonly UserStatsService _statsService;

        public SupabaseAuthService(Supabase.Client client)
        {
            _client = client;
            _profileService = new ProfileService(client);
            _statsService = new UserStatsService(client);
        }

        public User CurrentUser => _client.Auth.CurrentUser;
        public bool IsSignedIn => CurrentUser != null;

        // Единый flow:
        // 1) отправляем OTP
        // 2) после VerifyOTP либо создаём профиль/статы, либо просто входим в существующий аккаунт
        public async Task<AuthResult> BeginRegistrationAsync(string email, string username)
        {
            try
            {
                email = NormalizeEmail(email);
                username = NormalizeUsername(username);

                if (string.IsNullOrWhiteSpace(email))
                    return AuthResult.Fail("Email пустой.");

                if (string.IsNullOrWhiteSpace(username))
                    return AuthResult.Fail("Логин пустой.");

                var usernameAvailable = await _profileService.IsUsernameAvailablePublicAsync(username);
                if (!usernameAvailable)
                    return AuthResult.Fail("Этот логин уже занят.");

                await _client.Auth.SignInWithOtp(
                    new SignInWithPasswordlessEmailOptions(email)
                    {
                        ShouldCreateUser = true
                    });

                return AuthResult.Ok("Код отправлен на почту.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"BeginRegistrationAsync: {ex}");
                return AuthResult.Fail(MapSupabaseError(ex.Message));
            }
        }

        public async Task<AuthResult> VerifyRegistrationAsync(string email, string code, string username)
        {
            try
            {
                email = NormalizeEmail(email);
                code = NormalizeOtp(code);
                username = NormalizeUsername(username);

                if (string.IsNullOrWhiteSpace(email))
                    return AuthResult.Fail("Email пустой.");

                if (string.IsNullOrWhiteSpace(code))
                    return AuthResult.Fail("Код пустой.");

                if (string.IsNullOrWhiteSpace(username))
                    return AuthResult.Fail("Логин пустой.");

                // Оставляем один verification type для этого сценария
                var session = await _client.Auth.VerifyOTP(email, code, EmailOtpType.Signup);

                if (session == null || session.User == null)
                    return AuthResult.Fail("Не удалось подтвердить код.");

                var userId = Guid.Parse(session.User.Id);

                await WaitForSessionAsync();

                var profileBefore = await _profileService.GetByIdAsync(userId);
                bool wasCreated = profileBefore == null;

                var ensured = await _profileService.EnsureProfileAndStatsAsync(username);
                if (!ensured)
                    return AuthResult.Fail("Не удалось инициализировать профиль.");

                var profile = await _profileService.GetByIdAsync(userId);
                if (profile == null)
                    return AuthResult.Fail("Профиль не найден после подтверждения.");

                if (profile.IsDeleted)
                {
                    await _client.Auth.SignOut();
                    return AuthResult.Fail("Этот аккаунт удалён.");
                }

                var stats = await _statsService.GetByUserIdAsync(userId);
                if (stats == null)
                {
                    var createdStats = await _statsService.CreateDefaultAsync(userId);
                    if (createdStats == null)
                        return AuthResult.Fail("Не удалось создать статистику игрока.");
                }

                if (LocalPlayerData.Instance == null)
                    LocalPlayerData.Load();

                if (LocalPlayerData.Instance != null && LocalPlayerData.Instance.IsGuest)
                {
                    await _statsService.MergeGuestProgressAsync(userId, LocalPlayerData.Instance);
                }

                LocalPlayerData.Instance.MarkAsCloudUser(userId, profile.Username, profile.Email);
                LocalPlayerData.Instance.IsFirstLaunch = false;
                LocalPlayerData.Save();

                return AuthResult.Ok(wasCreated
                    ? "Аккаунт создан, вход выполнен."
                    : "Вход выполнен.");
            }
            catch (Exception ex)
            {
                Debug.LogError("VerifyRegistrationAsync EXCEPTION:");
                Debug.LogError(ex.ToString());

                if (ex.InnerException != null)
                    Debug.LogError("Inner 1: " + ex.InnerException);

                if (ex.InnerException?.InnerException != null)
                    Debug.LogError("Inner 2: " + ex.InnerException.InnerException);

                return AuthResult.Fail(MapSupabaseError(ex.Message));
            }
        }

        // Оставлено для совместимости с текущим проектом
        public async Task<AuthResult> BeginLoginAsync(string email)
        {
            try
            {
                email = NormalizeEmail(email);

                if (string.IsNullOrWhiteSpace(email))
                    return AuthResult.Fail("Email пустой.");

                await _client.Auth.SignInWithOtp(
                    new SignInWithPasswordlessEmailOptions(email)
                    {
                        ShouldCreateUser = false
                    });

                return AuthResult.Ok("Код отправлен на почту.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"BeginLoginAsync: {ex}");
                return AuthResult.Fail(MapSupabaseError(ex.Message));
            }
        }

        // Оставлено для совместимости с текущим проектом
        public async Task<AuthResult> VerifyLoginAsync(string email, string code)
        {
            try
            {
                email = NormalizeEmail(email);
                code = NormalizeOtp(code);

                var session = await _client.Auth.VerifyOTP(email, code, EmailOtpType.MagicLink);
                if (session == null || session.User == null)
                    return AuthResult.Fail("Не удалось войти.");

                await WaitForSessionAsync();

                var userId = Guid.Parse(session.User.Id);
                var profile = await _profileService.GetByIdAsync(userId);

                if (profile == null)
                    return AuthResult.Fail("Профиль не найден.");

                if (profile.IsDeleted)
                {
                    await _client.Auth.SignOut();
                    return AuthResult.Fail("Этот аккаунт удалён.");
                }

                var stats = await _statsService.GetByUserIdAsync(userId);
                if (stats == null)
                    await _statsService.CreateDefaultAsync(userId);

                if (LocalPlayerData.Instance == null)
                    LocalPlayerData.Load();

                LocalPlayerData.Instance.MarkAsCloudUser(userId, profile.Username, profile.Email);
                LocalPlayerData.Instance.IsFirstLaunch = false;
                LocalPlayerData.Save();

                return AuthResult.Ok("Вход выполнен.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"VerifyLoginAsync: {ex}");
                return AuthResult.Fail(MapSupabaseError(ex.Message));
            }
        }

        public async Task<AuthResult> ContinueAsGuestAsync(string guestName = "Guest")
        {
            LocalPlayerData.Load();
            LocalPlayerData.Instance.IsGuest = true;
            LocalPlayerData.Instance.LocalDisplayName = string.IsNullOrWhiteSpace(guestName) ? "Guest" : guestName;
            LocalPlayerData.Save();

            return await Task.FromResult(AuthResult.Ok("Гостевой режим активирован."));
        }

        public async Task<AuthResult> BeginGuestUpgradeAsync(string email, string username)
        {
            if (LocalPlayerData.Instance == null)
                LocalPlayerData.Load();

            if (!LocalPlayerData.Instance.IsGuest)
                return AuthResult.Fail("Пользователь уже не является гостем.");

            return await BeginRegistrationAsync(email, username);
        }

        public async Task<AuthResult> VerifyGuestUpgradeAsync(string email, string code, string username)
        {
            if (LocalPlayerData.Instance == null)
                LocalPlayerData.Load();

            if (!LocalPlayerData.Instance.IsGuest)
                return AuthResult.Fail("Пользователь уже не является гостем.");

            return await VerifyRegistrationAsync(email, code, username);
        }

        public async Task<AuthResult> ChangeUsernameAsync(string newUsername)
        {
            try
            {
                var user = CurrentUser;
                if (user == null)
                    return AuthResult.Fail("Пользователь не авторизован.");

                var userId = Guid.Parse(user.Id);
                newUsername = NormalizeUsername(newUsername);

                if (string.IsNullOrWhiteSpace(newUsername))
                    return AuthResult.Fail("Логин пустой.");

                var currentProfile = await _profileService.GetByIdAsync(userId);
                if (currentProfile == null)
                    return AuthResult.Fail("Профиль не найден.");

                if (string.Equals(currentProfile.Username, newUsername, StringComparison.OrdinalIgnoreCase))
                    return AuthResult.Ok("Логин не изменился.");

                var available = await _profileService.IsUsernameAvailablePublicAsync(newUsername);
                if (!available)
                    return AuthResult.Fail("Этот логин уже занят.");

                var ok = await _profileService.UpdateUsernameAsync(userId, newUsername);
                if (!ok)
                    return AuthResult.Fail("Не удалось обновить логин.");

                if (LocalPlayerData.Instance != null)
                {
                    LocalPlayerData.Instance.LocalDisplayName = newUsername;
                    LocalPlayerData.Save();
                }

                return AuthResult.Ok("Логин обновлён.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"ChangeUsernameAsync: {ex}");
                return AuthResult.Fail(MapSupabaseError(ex.Message));
            }
        }

        public async Task<AuthResult> BeginEmailChangeAsync(string newEmail)
        {
            try
            {
                var user = CurrentUser;
                if (user == null)
                    return AuthResult.Fail("Пользователь не авторизован.");

                newEmail = NormalizeEmail(newEmail);

                if (string.IsNullOrWhiteSpace(newEmail))
                    return AuthResult.Fail("Новая почта пустая.");

                var attrs = new UserAttributes
                {
                    Email = newEmail
                };

                await _client.Auth.Update(attrs);

                return AuthResult.Ok("Код подтверждения для смены почты отправлен.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"BeginEmailChangeAsync: {ex}");
                return AuthResult.Fail(MapSupabaseError(ex.Message));
            }
        }

        public async Task<AuthResult> ConfirmEmailChangeAsync(string newEmail, string code)
        {
            try
            {
                var user = CurrentUser;
                if (user == null)
                    return AuthResult.Fail("Пользователь не авторизован.");

                newEmail = NormalizeEmail(newEmail);
                code = NormalizeOtp(code);

                var session = await _client.Auth.VerifyOTP(newEmail, code, EmailOtpType.EmailChange);
                if (session == null || session.User == null)
                    return AuthResult.Fail("Не удалось подтвердить смену почты.");

                await WaitForSessionAsync();

                var userId = Guid.Parse(session.User.Id);
                var updatedEmail = string.IsNullOrWhiteSpace(session.User.Email)
                    ? newEmail
                    : NormalizeEmail(session.User.Email);

                await _profileService.UpdateEmailMirrorAsync(userId, updatedEmail);

                if (LocalPlayerData.Instance != null)
                {
                    LocalPlayerData.Instance.Email = updatedEmail;
                    LocalPlayerData.Save();
                }

                return AuthResult.Ok("Почта обновлена.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"ConfirmEmailChangeAsync: {ex}");
                return AuthResult.Fail(MapSupabaseError(ex.Message));
            }
        }

        public async Task<AuthResult> SignOutAsync()
        {
            try
            {
                await _client.Auth.SignOut();
                return AuthResult.Ok("Выход выполнен.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"SignOutAsync: {ex}");
                return AuthResult.Fail(ex.Message);
            }
        }

        public async Task<ProfileEntity> GetCurrentProfileAsync()
        {
            var user = CurrentUser;
            if (user == null)
                return null;

            return await _profileService.GetByIdAsync(Guid.Parse(user.Id));
        }

        public async Task<UserStatsEntity> GetCurrentStatsAsync()
        {
            var user = CurrentUser;
            if (user == null)
                return null;

            return await _statsService.GetByUserIdAsync(Guid.Parse(user.Id));
        }

        public async Task<AuthResult> BeginResetStatisticAsync()
        {
            try
            {
                var user = CurrentUser;
                if (user == null)
                    return AuthResult.Fail("Пользователь не авторизован.");

                if (string.IsNullOrWhiteSpace(user.Email))
                    return AuthResult.Fail("У аккаунта нет почты.");

                await _client.Auth.SignInWithOtp(
                    new SignInWithPasswordlessEmailOptions(user.Email)
                    {
                        ShouldCreateUser = false
                    });

                return AuthResult.Ok("Код для сброса статистики отправлен.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"BeginResetStatisticAsync: {ex}");
                return AuthResult.Fail(MapSupabaseError(ex.Message));
            }
        }

        public async Task<AuthResult> ConfirmResetStatisticAsync(string code)
        {
            try
            {
                var user = CurrentUser;
                if (user == null)
                    return AuthResult.Fail("Пользователь не авторизован.");

                if (string.IsNullOrWhiteSpace(user.Email))
                    return AuthResult.Fail("У аккаунта нет почты.");

                code = NormalizeOtp(code);

                var session = await _client.Auth.VerifyOTP(user.Email, code, EmailOtpType.MagicLink);
                if (session == null || session.User == null)
                    return AuthResult.Fail("Неверный код.");

                await WaitForSessionAsync();

                var userId = Guid.Parse(session.User.Id);
                var ok = await _statsService.ResetStatsAsync(userId);

                if (!ok)
                    return AuthResult.Fail("Статистика не найдена.");

                if (LocalPlayerData.Instance != null)
                {
                    LocalPlayerData.Instance.Wins = 0;
                    LocalPlayerData.Instance.Losses = 0;
                    LocalPlayerData.Instance.GamePlayed = 0;
                    LocalPlayerData.Instance.WordsMadeUp = 0;
                    LocalPlayerData.Instance.AverageWordLen = 0;
                    LocalPlayerData.Instance.LongestWord = 0;
                    LocalPlayerData.Instance.SeriesOfVictories = 0;
                    LocalPlayerData.Instance.PointsForAllTime = 0;
                    LocalPlayerData.Save();
                }

                return AuthResult.Ok("Статистика сброшена.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"ConfirmResetStatisticAsync: {ex}");
                return AuthResult.Fail(MapSupabaseError(ex.Message));
            }
        }

        public async Task<AuthResult> BeginDeleteAccountAsync()
        {
            try
            {
                var user = CurrentUser;
                if (user == null)
                    return AuthResult.Fail("Пользователь не авторизован.");

                if (string.IsNullOrWhiteSpace(user.Email))
                    return AuthResult.Fail("У аккаунта нет почты.");

                await _client.Auth.SignInWithOtp(
                    new SignInWithPasswordlessEmailOptions(user.Email)
                    {
                        ShouldCreateUser = false
                    });

                return AuthResult.Ok("Код для удаления аккаунта отправлен.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"BeginDeleteAccountAsync: {ex}");
                return AuthResult.Fail(MapSupabaseError(ex.Message));
            }
        }

        public async Task<AuthResult> ConfirmDeleteAccountAsync(string code)
        {
            try
            {
                var user = CurrentUser;
                if (user == null)
                    return AuthResult.Fail("Пользователь не авторизован.");

                if (string.IsNullOrWhiteSpace(user.Email))
                    return AuthResult.Fail("У аккаунта нет почты.");

                code = NormalizeOtp(code);

                var session = await _client.Auth.VerifyOTP(user.Email, code, EmailOtpType.MagicLink);
                if (session == null || session.User == null)
                    return AuthResult.Fail("Неверный код.");

                await WaitForSessionAsync();

                var userId = Guid.Parse(session.User.Id);

                await _statsService.ResetStatsAsync(userId);

                var deleted = await _profileService.SoftDeleteAsync(userId);
                if (!deleted)
                    return AuthResult.Fail("Не удалось удалить аккаунт.");

                LocalPlayerData.ResetToGuest();
                await _client.Auth.SignOut();

                return AuthResult.Ok("Аккаунт удалён.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"ConfirmDeleteAccountAsync: {ex}");
                return AuthResult.Fail(MapSupabaseError(ex.Message));
            }
        }

        public Task<AuthResult> BeginEmailAuthAsync(string email, string username)
        {
            return BeginRegistrationAsync(email, username);
        }

        public Task<AuthResult> VerifyEmailAuthAsync(string email, string code, string username)
        {
            return VerifyRegistrationAsync(email, code, username);
        }

        private async Task WaitForSessionAsync()
        {
            for (int i = 0; i < 10; i++)
            {
                if (_client.Auth.CurrentSession != null && _client.Auth.CurrentUser != null)
                    return;

                await Task.Delay(100);
            }
        }

        private static string NormalizeEmail(string email)
        {
            return string.IsNullOrWhiteSpace(email)
                ? string.Empty
                : email.Trim().ToLowerInvariant();
        }

        private static string NormalizeUsername(string username)
        {
            return string.IsNullOrWhiteSpace(username)
                ? string.Empty
                : username.Trim();
        }

        private static string NormalizeOtp(string code)
        {
            return string.IsNullOrWhiteSpace(code)
                ? string.Empty
                : code.Trim().Replace(" ", string.Empty);
        }

        private string MapSupabaseError(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "Неизвестная ошибка.";

            var text = raw.ToLowerInvariant();

            if (text.Contains("otp_expired") || text.Contains("expired"))
                return "Срок действия кода истёк. Запроси новый код.";

            if (text.Contains("invalid"))
                return "Неверный код.";

            if (text.Contains("over_email_send_rate_limit") || text.Contains("rate limit"))
                return "Код запрашивается слишком часто. Подожди немного.";

            if (text.Contains("user already registered"))
                return "Пользователь с такой почтой уже существует.";

            if (text.Contains("username_taken"))
                return "Этот логин уже занят.";

            if (text.Contains("duplicate") || text.Contains("unique"))
                return "Такое значение уже занято.";

            if (text.Contains("not_authenticated"))
                return "Пользователь не авторизован.";

            if (text.Contains("email not confirmed"))
                return "Почта ещё не подтверждена.";

            if (text.Contains("invalid login credentials"))
                return "Неверный код или аккаунт не найден.";

            return raw;
        }
    }

    public class AuthResult
    {
        public bool Success;
        public string Message;

        public static AuthResult Ok(string message) => new AuthResult
        {
            Success = true,
            Message = message
        };

        public static AuthResult Fail(string message) => new AuthResult
        {
            Success = false,
            Message = message
        };
    }
}