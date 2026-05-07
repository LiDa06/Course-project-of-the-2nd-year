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
        private readonly RecentGamesService _recentGamesService;

        private string _pendingResetStatisticEmail = string.Empty;
        private string _pendingDeleteAccountEmail = string.Empty;

        public SupabaseAuthService(Supabase.Client client)
        {
            _client = client;
            _profileService = new ProfileService(client);
            _statsService = new UserStatsService(client);
            _recentGamesService = new RecentGamesService(client);
        }

        public User CurrentUser => _client.Auth.CurrentUser ?? _client.Auth.CurrentSession?.User;

        public bool IsSignedIn => CurrentUser != null || HasUsableSession();

        public string CurrentAccountEmail => GetCurrentAccountEmail();

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

                var emailRegistered = await _profileService.IsActiveEmailRegisteredPublicAsync(email);
                if (emailRegistered == true)
                    return AuthResult.Fail("Пользователь с такой почтой уже существует. Попробуй войти в аккаунт.");

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
                Debug.LogError("BeginRegistrationAsync EXCEPTION:");
                Debug.LogError(ex.ToString());

                if (ex.InnerException != null)
                    Debug.LogError("Inner 1: " + ex.InnerException);

                if (ex.InnerException?.InnerException != null)
                    Debug.LogError("Inner 2: " + ex.InnerException.InnerException);

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

                var session = await _client.Auth.VerifyOTP(email, code, EmailOtpType.Signup);

                if (session == null || session.User == null)
                    return AuthResult.Fail("Не удалось подтвердить код.");

                var userId = Guid.Parse(session.User.Id);

                await WaitForSessionAsync();

                var profileBefore = await WaitForProfileAsync(userId);
                bool wasCreated = profileBefore == null;

                var ensured = await _profileService.EnsureProfileAndStatsAsync(username);
                if (!ensured)
                    return AuthResult.Fail("Не удалось инициализировать профиль.");

                var profile = await WaitForProfileAsync(userId);
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
                    await _recentGamesService.ReplaceLastAsync(userId, LocalPlayerData.Instance.RecentGames);
                }

                await SyncLocalPlayerDataFromServerAsync(userId, profile);

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

        public async Task<AuthResult> BeginLoginAsync(string email)
        {
            try
            {
                email = NormalizeEmail(email);

                if (string.IsNullOrWhiteSpace(email))
                    return AuthResult.Fail("Email пустой.");

                var emailRegistered = await _profileService.IsActiveEmailRegisteredPublicAsync(email);
                if (emailRegistered == false)
                    return AuthResult.Fail("Аккаунт с такой почтой не найден. Проверь email или зарегистрируйся.");

                await _client.Auth.SignInWithOtp(
                    new SignInWithPasswordlessEmailOptions(email)
                    {
                        ShouldCreateUser = false
                    });

                return AuthResult.Ok("Код отправлен на почту.");
            }
            catch (Exception ex)
            {
                Debug.LogError("BeginLoginAsync EXCEPTION:");
                Debug.LogError(ex.ToString());

                if (ex.InnerException != null)
                    Debug.LogError("Inner 1: " + ex.InnerException);

                if (ex.InnerException?.InnerException != null)
                    Debug.LogError("Inner 2: " + ex.InnerException.InnerException);

                return AuthResult.Fail(MapSupabaseError(ex.Message));
            }
        }

        public async Task<AuthResult> VerifyLoginAsync(string email, string code)
        {
            try
            {
                email = NormalizeEmail(email);
                code = NormalizeOtp(code);

                if (string.IsNullOrWhiteSpace(email))
                    return AuthResult.Fail("Email пустой.");

                if (string.IsNullOrWhiteSpace(code))
                    return AuthResult.Fail("Код пустой.");

                var session = await _client.Auth.VerifyOTP(email, code, EmailOtpType.MagicLink);
                if (session == null || session.User == null)
                    return AuthResult.Fail("Не удалось войти.");

                await WaitForSessionAsync();

                var userId = Guid.Parse(session.User.Id);
                var profile = await WaitForProfileAsync(userId);

                if (profile == null)
                    return AuthResult.Fail("Профиль не найден после подтверждения входа.");

                if (profile.IsDeleted)
                {
                    await _client.Auth.SignOut();
                    return AuthResult.Fail("Этот аккаунт удалён.");
                }

                var stats = await _statsService.GetByUserIdAsync(userId);
                if (stats == null)
                    await _statsService.CreateDefaultAsync(userId);

                await SyncLocalPlayerDataFromServerAsync(userId, profile);

                return AuthResult.Ok("Вход выполнен.");
            }
            catch (Exception ex)
            {
                Debug.LogError("VerifyLoginAsync EXCEPTION:");
                Debug.LogError(ex.ToString());

                if (ex.InnerException != null)
                    Debug.LogError("Inner 1: " + ex.InnerException);

                if (ex.InnerException?.InnerException != null)
                    Debug.LogError("Inner 2: " + ex.InnerException.InnerException);

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
                Debug.LogError("ChangeUsernameAsync EXCEPTION:");
                Debug.LogError(ex.ToString());

                if (ex.InnerException != null)
                    Debug.LogError("Inner 1: " + ex.InnerException);

                if (ex.InnerException?.InnerException != null)
                    Debug.LogError("Inner 2: " + ex.InnerException.InnerException);

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

                string currentEmail = GetCurrentAccountEmail();
                if (string.Equals(currentEmail, newEmail, StringComparison.OrdinalIgnoreCase))
                    return AuthResult.Fail("Это уже текущая почта аккаунта.");

                var emailRegistered = await _profileService.IsActiveEmailRegisteredPublicAsync(newEmail);
                if (emailRegistered == true)
                    return AuthResult.Fail("Эта почта уже используется другим аккаунтом.");

                var attrs = new UserAttributes
                {
                    Email = newEmail
                };

                await _client.Auth.Update(attrs);

                return AuthResult.Ok("Код подтверждения для смены почты отправлен.");
            }
            catch (Exception ex)
            {
                Debug.LogError("BeginEmailChangeAsync EXCEPTION:");
                Debug.LogError(ex.ToString());

                if (ex.InnerException != null)
                    Debug.LogError("Inner 1: " + ex.InnerException);

                if (ex.InnerException?.InnerException != null)
                    Debug.LogError("Inner 2: " + ex.InnerException.InnerException);

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

                // После VerifyOTP SDK иногда оставляет старый Email в session.User.
                // Если код на новую почту успешно подтверждён, источником правды здесь является newEmail.
                var updatedEmail = newEmail;

                await _profileService.UpdateEmailMirrorAsync(userId, updatedEmail);

                if (LocalPlayerData.Instance != null)
                {
                    LocalPlayerData.Instance.Email = updatedEmail;
                    LocalPlayerData.Instance.CloudUserId = userId.ToString();
                    LocalPlayerData.Instance.IsGuest = false;
                    LocalPlayerData.Save();
                }

                return AuthResult.Ok("Почта обновлена.");
            }
            catch (Exception ex)
            {
                Debug.LogError("ConfirmEmailChangeAsync EXCEPTION:");
                Debug.LogError(ex.ToString());

                if (ex.InnerException != null)
                    Debug.LogError("Inner 1: " + ex.InnerException);

                if (ex.InnerException?.InnerException != null)
                    Debug.LogError("Inner 2: " + ex.InnerException.InnerException);

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
                Debug.LogError("SignOutAsync EXCEPTION:");
                Debug.LogError(ex.ToString());
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

        public async Task<AuthResult> SyncLocalStatsAndRecentGamesAsync()
        {
            Guid userId = Guid.Empty;
            LocalPlayerData local = null;

            try
            {
                var user = CurrentUser;
                if (user == null)
                    return AuthResult.Fail("Пользователь не авторизован.");

                if (LocalPlayerData.Instance == null)
                    LocalPlayerData.Load();

                local = LocalPlayerData.Instance;
                if (local == null)
                    return AuthResult.Fail("Локальные данные игрока не найдены.");

                if (local.IsGuest)
                    return AuthResult.Ok("Гостевая статистика хранится локально.");

                userId = Guid.Parse(user.Id);
                if (!string.IsNullOrWhiteSpace(local.CloudUserId) &&
                    !string.Equals(local.CloudUserId, userId.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return AuthResult.Fail("Локальные данные относятся к другому пользователю.");
                }

                await ExecuteSyncRequestWithRetryAsync(
                    () => _statsService.SaveFromLocalAsync(userId, local),
                    "сохранение статистики");

                await ExecuteSyncRequestWithRetryAsync(
                    () => _recentGamesService.ReplaceLastAsync(userId, local.RecentGames),
                    "сохранение последних игр");

                local.HasUnsyncedStats = false;
                LocalPlayerData.Save();

                return AuthResult.Ok("Статистика синхронизирована.");
            }
            catch (Exception ex)
            {
                // Иногда Unity/Supabase получает обрыв HTTP-соединения уже после того,
                // как сервер успел применить PATCH/INSERT. В таком случае проверяем
                // текущее состояние сервера и не показываем ошибку как критическую.
                if (userId != Guid.Empty && local != null && IsTransientNetworkException(ex))
                {
                    try
                    {
                        bool serverAlreadyHasData = await ServerAlreadyContainsLocalStatsAsync(userId, local);
                        if (serverAlreadyHasData)
                        {
                            local.HasUnsyncedStats = false;
                            LocalPlayerData.Save();
                            Debug.LogWarning("SyncLocalStatsAndRecentGamesAsync: сервер применил данные, но HTTP-ответ был оборван. Локальный флаг синхронизации снят.");
                            return AuthResult.Ok("Статистика синхронизирована.");
                        }
                    }
                    catch (Exception verifyEx)
                    {
                        Debug.LogWarning("SyncLocalStatsAndRecentGamesAsync: не удалось проверить состояние сервера после сетевой ошибки: " + verifyEx.Message);
                    }

                    Debug.LogWarning("SyncLocalStatsAndRecentGamesAsync: временная сетевая ошибка, синхронизация будет повторена позже: " + ex.Message);
                    return AuthResult.Fail("Сервер временно недоступен. Статистика сохранена локально и будет отправлена позже.");
                }

                Debug.LogError("SyncLocalStatsAndRecentGamesAsync EXCEPTION:");
                Debug.LogError(ex.ToString());
                return AuthResult.Fail(MapSupabaseError(ex.Message));
            }
        }

        public async Task<AuthResult> BeginResetStatisticAsync()
        {
            try
            {
                string email = GetCurrentAccountEmail();
                if (string.IsNullOrWhiteSpace(email))
                    return AuthResult.Fail("Пользователь не авторизован.");

                _pendingResetStatisticEmail = email;

                await _client.Auth.SignInWithOtp(
                    new SignInWithPasswordlessEmailOptions(email)
                    {
                        ShouldCreateUser = false
                    });

                return AuthResult.Ok("Код для сброса статистики отправлен.");
            }
            catch (Exception ex)
            {
                Debug.LogError("BeginResetStatisticAsync EXCEPTION:");
                Debug.LogError(ex.ToString());

                if (ex.InnerException != null)
                    Debug.LogError("Inner 1: " + ex.InnerException);

                if (ex.InnerException?.InnerException != null)
                    Debug.LogError("Inner 2: " + ex.InnerException.InnerException);

                return AuthResult.Fail(MapSupabaseError(ex.Message));
            }
        }

        public async Task<AuthResult> ConfirmResetStatisticAsync(string code)
        {
            try
            {
                string email = !string.IsNullOrWhiteSpace(_pendingResetStatisticEmail)
                    ? _pendingResetStatisticEmail
                    : GetCurrentAccountEmail();

                if (string.IsNullOrWhiteSpace(email))
                    return AuthResult.Fail("Пользователь не авторизован.");

                code = NormalizeOtp(code);

                var session = await _client.Auth.VerifyOTP(email, code, EmailOtpType.MagicLink);
                if (session == null || session.User == null)
                    return AuthResult.Fail("Неверный код.");

                await WaitForSessionAsync();

                _pendingResetStatisticEmail = string.Empty;

                var userId = Guid.Parse(session.User.Id);
                var ok = await _statsService.ResetStatsAsync(userId);

                if (!ok)
                    return AuthResult.Fail("Не удалось сбросить статистику.");

                await _recentGamesService.DeleteAllAsync(userId);

                if (LocalPlayerData.Instance != null)
                {
                    LocalPlayerData.Instance.Wins = 0;
                    LocalPlayerData.Instance.Losses = 0;
                    LocalPlayerData.Instance.GamePlayed = 0;
                    LocalPlayerData.Instance.WordsMadeUp = 0;
                    LocalPlayerData.Instance.AverageWordLen = 0;
                    LocalPlayerData.Instance.LongestWord = 0;
                    LocalPlayerData.Instance.PointsForAllTime = 0;
                    LocalPlayerData.Instance.TotalLettersInAcceptedWords = 0;
                    LocalPlayerData.Instance.RecentGames = new System.Collections.Generic.List<RecentGameInfo>();
                    LocalPlayerData.Instance.HasUnsyncedStats = false;
                    LocalPlayerData.Instance.IsGuest = false;
                    LocalPlayerData.Instance.CloudUserId = userId.ToString();
                    LocalPlayerData.Save();
                }

                return AuthResult.Ok("Статистика сброшена.");
            }
            catch (Exception ex)
            {
                Debug.LogError("ConfirmResetStatisticAsync EXCEPTION:");
                Debug.LogError(ex.ToString());

                if (ex.InnerException != null)
                    Debug.LogError("Inner 1: " + ex.InnerException);

                if (ex.InnerException?.InnerException != null)
                    Debug.LogError("Inner 2: " + ex.InnerException.InnerException);

                return AuthResult.Fail(MapSupabaseError(ex.Message));
            }
        }

        public async Task<AuthResult> BeginDeleteAccountAsync()
        {
            try
            {
                string email = GetCurrentAccountEmail();
                if (string.IsNullOrWhiteSpace(email))
                    return AuthResult.Fail("Пользователь не авторизован.");

                _pendingDeleteAccountEmail = email;

                await _client.Auth.SignInWithOtp(
                    new SignInWithPasswordlessEmailOptions(email)
                    {
                        ShouldCreateUser = false
                    });

                return AuthResult.Ok("Код для удаления аккаунта отправлен.");
            }
            catch (Exception ex)
            {
                Debug.LogError("BeginDeleteAccountAsync EXCEPTION:");
                Debug.LogError(ex.ToString());

                if (ex.InnerException != null)
                    Debug.LogError("Inner 1: " + ex.InnerException);

                if (ex.InnerException?.InnerException != null)
                    Debug.LogError("Inner 2: " + ex.InnerException.InnerException);

                return AuthResult.Fail(MapSupabaseError(ex.Message));
            }
        }

        public async Task<AuthResult> ConfirmDeleteAccountAsync(string code)
        {
            try
            {
                string email = !string.IsNullOrWhiteSpace(_pendingDeleteAccountEmail)
                    ? _pendingDeleteAccountEmail
                    : GetCurrentAccountEmail();

                if (string.IsNullOrWhiteSpace(email))
                    return AuthResult.Fail("Пользователь не авторизован.");

                code = NormalizeOtp(code);

                var session = await _client.Auth.VerifyOTP(email, code, EmailOtpType.MagicLink);
                if (session == null || session.User == null)
                    return AuthResult.Fail("Неверный код.");

                await WaitForSessionAsync();

                _pendingDeleteAccountEmail = string.Empty;

                var userId = Guid.Parse(session.User.Id);

                await _statsService.ResetStatsAsync(userId);
                await _recentGamesService.DeleteAllAsync(userId);

                var deleted = await _profileService.SoftDeleteAsync(userId);
                if (!deleted)
                    return AuthResult.Fail("Не удалось удалить аккаунт.");

                LocalPlayerData.ResetToGuest();
                await _client.Auth.SignOut();

                return AuthResult.Ok("Аккаунт удалён.");
            }
            catch (Exception ex)
            {
                Debug.LogError("ConfirmDeleteAccountAsync EXCEPTION:");
                Debug.LogError(ex.ToString());

                if (ex.InnerException != null)
                    Debug.LogError("Inner 1: " + ex.InnerException);

                if (ex.InnerException?.InnerException != null)
                    Debug.LogError("Inner 2: " + ex.InnerException.InnerException);

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

        private bool HasUsableSession()
        {
            return _client.Auth.CurrentSession != null &&
                   !string.IsNullOrWhiteSpace(_client.Auth.CurrentSession.AccessToken);
        }

        private string GetCurrentAccountEmail()
        {
            string email = CurrentUser?.Email;

            if (string.IsNullOrWhiteSpace(email))
                email = _client.Auth.CurrentSession?.User?.Email;

            if (string.IsNullOrWhiteSpace(email))
            {
                if (LocalPlayerData.Instance == null)
                    LocalPlayerData.Load();

                email = LocalPlayerData.Instance != null ? LocalPlayerData.Instance.Email : string.Empty;
            }

            return NormalizeEmail(email);
        }

        private async Task WaitForSessionAsync()
        {
            for (int i = 0; i < 50; i++)
            {
                if (_client.Auth.CurrentSession != null &&
                    !string.IsNullOrWhiteSpace(_client.Auth.CurrentSession.AccessToken))
                {
                    return;
                }

                await Task.Delay(200);
            }

            Debug.LogWarning("WaitForSessionAsync: session was not fully initialized in time.");
        }

        private async Task<ProfileEntity> WaitForProfileAsync(Guid userId)
        {
            for (int i = 0; i < 15; i++)
            {
                try
                {
                    var profile = await _profileService.GetByIdAsync(userId);
                    if (profile != null)
                        return profile;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("WaitForProfileAsync try failed: " + ex.Message);
                }

                await Task.Delay(250);
            }

            return null;
        }

        private async Task SyncLocalPlayerDataFromServerAsync(Guid userId, ProfileEntity profile)
        {
            if (LocalPlayerData.Instance == null)
                LocalPlayerData.Load();

            var local = LocalPlayerData.Instance;
            if (local == null || profile == null)
                return;

            if (!local.IsGuest &&
                local.HasUnsyncedStats &&
                string.Equals(local.CloudUserId, userId.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    await ExecuteSyncRequestWithRetryAsync(
                        () => _statsService.SaveFromLocalAsync(userId, local),
                        "сохранение статистики перед загрузкой профиля");

                    await ExecuteSyncRequestWithRetryAsync(
                        () => _recentGamesService.ReplaceLastAsync(userId, local.RecentGames),
                        "сохранение последних игр перед загрузкой профиля");

                    local.HasUnsyncedStats = false;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("SyncLocalPlayerDataFromServerAsync: не удалось отправить локальные изменения перед загрузкой с сервера: " + ex.Message);
                }
            }

            var stats = await _statsService.GetByUserIdAsync(userId);
            if (stats == null)
                stats = await _statsService.CreateDefaultAsync(userId);

            var recentGames = await _recentGamesService.GetLastAsync(userId, 3);

            local.MarkAsCloudUser(userId, profile.Username, profile.Email);
            local.IsFirstLaunch = false;

            if (stats != null)
            {
                local.Wins = stats.Wins;
                local.Losses = stats.Losses;
                local.GamePlayed = stats.GamePlayed;
                local.WordsMadeUp = stats.WordsMadeUp;
                local.AverageWordLen = stats.AverageWordLen;
                local.LongestWord = stats.LongestWord;
                local.PointsForAllTime = stats.PointsForAllTime;
                local.TotalLettersInAcceptedWords = stats.TotalLettersInAcceptedWords;

                if (stats.CreatedAt != default)
                    local.CreatedAtTicks = DateTime.SpecifyKind(stats.CreatedAt, DateTimeKind.Utc).Ticks;
            }

            local.RecentGames = recentGames ?? new System.Collections.Generic.List<RecentGameInfo>();
            local.HasUnsyncedStats = false;

            LocalPlayerData.Save();
        }

        private async Task ExecuteSyncRequestWithRetryAsync(Func<Task> action, string operationName, int attempts = 3)
        {
            if (action == null)
                return;

            Exception lastException = null;

            for (int attempt = 1; attempt <= attempts; attempt++)
            {
                try
                {
                    await action();
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    if (!IsTransientNetworkException(ex) || attempt >= attempts)
                        break;

                    Debug.LogWarning($"Sync retry {attempt}/{attempts}: {operationName} не выполнено из-за временной сетевой ошибки: {ex.Message}");
                    await Task.Delay(350 * attempt);
                }
            }

            throw lastException ?? new Exception($"Не удалось выполнить операцию синхронизации: {operationName}");
        }

        private async Task<bool> ServerAlreadyContainsLocalStatsAsync(Guid userId, LocalPlayerData local)
        {
            if (local == null)
                return false;

            var stats = await _statsService.GetByUserIdAsync(userId);
            if (stats == null)
                return false;

            if (stats.Wins != local.Wins ||
                stats.Losses != local.Losses ||
                stats.GamePlayed != local.GamePlayed ||
                stats.WordsMadeUp != local.WordsMadeUp ||
                stats.AverageWordLen != local.AverageWordLen ||
                stats.LongestWord != local.LongestWord ||
                stats.PointsForAllTime != local.PointsForAllTime ||
                stats.TotalLettersInAcceptedWords != local.TotalLettersInAcceptedWords)
            {
                return false;
            }

            var serverRecentGames = await _recentGamesService.GetLastAsync(userId, 3);
            var localRecentGames = local.RecentGames ?? new System.Collections.Generic.List<RecentGameInfo>();

            int count = Math.Min(3, localRecentGames.Count);
            if (serverRecentGames == null || serverRecentGames.Count != count)
                return false;

            for (int i = 0; i < count; i++)
            {
                if (!RecentGamesAreEqual(serverRecentGames[i], localRecentGames[i]))
                    return false;
            }

            return true;
        }

        private static bool RecentGamesAreEqual(RecentGameInfo a, RecentGameInfo b)
        {
            if (a == null || b == null)
                return a == b;

            bool finishedAtCloseEnough =
                a.FinishedAtTicks <= 0 ||
                b.FinishedAtTicks <= 0 ||
                Math.Abs(a.FinishedAtTicks - b.FinishedAtTicks) <= TimeSpan.TicksPerSecond;

            return finishedAtCloseEnough &&
                   string.Equals(a.Mode ?? string.Empty, b.Mode ?? string.Empty, StringComparison.OrdinalIgnoreCase) &&
                   a.BoardSize == b.BoardSize &&
                   string.Equals(a.Result ?? string.Empty, b.Result ?? string.Empty, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(a.OpponentName ?? string.Empty, b.OpponentName ?? string.Empty, StringComparison.Ordinal) &&
                   a.PlayerOneScore == b.PlayerOneScore &&
                   a.PlayerTwoScore == b.PlayerTwoScore &&
                   a.TurnCount == b.TurnCount &&
                   string.Equals(a.BestWord ?? string.Empty, b.BestWord ?? string.Empty, StringComparison.OrdinalIgnoreCase) &&
                   a.DurationSeconds == b.DurationSeconds;
        }

        private static bool IsTransientNetworkException(Exception ex)
        {
            if (ex == null)
                return false;

            string text = ex.ToString().ToLowerInvariant();
            return text.Contains("httprequestexception") ||
                   text.Contains("webexception") ||
                   text.Contains("transport connection") ||
                   text.Contains("forcibly closed") ||
                   text.Contains("timed out") ||
                   text.Contains("timeout") ||
                   text.Contains("connection") ||
                   text.Contains("network") ||
                   text.Contains("sending the request");
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

            if (text.Contains("otp_expired") || text.Contains("token has expired") || text.Contains("expired"))
                return "Срок действия кода истёк. Запроси новый код.";

            if (text.Contains("over_email_send_rate_limit") ||
                text.Contains("rate limit") ||
                text.Contains("too many requests") ||
                text.Contains("email rate limit exceeded"))
                return "Код запрашивается слишком часто. Подожди немного.";

            if (text.Contains("user already registered") ||
                text.Contains("email_exists") ||
                text.Contains("already registered") ||
                text.Contains("already exists") && text.Contains("email"))
                return "Пользователь с такой почтой уже существует или был удалён. Попробуй войти в аккаунт.";

            if (text.Contains("user_not_found") ||
                text.Contains("user not found") ||
                text.Contains("signups not allowed") ||
                text.Contains("signup disabled") ||
                text.Contains("invalid login credentials"))
                return "Аккаунт с такой почтой не найден или код введён неверно.";

            if (text.Contains("username_taken"))
                return "Этот логин уже занят.";

            if (text.Contains("duplicate key") && text.Contains("username"))
                return "Этот логин уже занят.";

            if (text.Contains("duplicate key") && text.Contains("email"))
                return "Эта почта уже используется другим аккаунтом.";

            if (text.Contains("duplicate") || text.Contains("unique") || text.Contains("23505"))
                return "Такое значение уже занято.";

            if (text.Contains("not_authenticated") ||
                text.Contains("unauthorized") ||
                text.Contains("401"))
                return "Сессия истекла. Войди в аккаунт заново.";

            if (text.Contains("email not confirmed"))
                return "Почта ещё не подтверждена.";

            if (text.Contains("invalid token") ||
                text.Contains("token is invalid") ||
                text.Contains("invalid otp") ||
                text.Contains("otp invalid") ||
                text.Contains("invalid_grant") ||
                text.Contains("bad jwt") ||
                text.Contains("invalid"))
                return "Неверный код. Проверь письмо и введи 6 цифр без пробелов.";

            if (text.Contains("transport connection") ||
                text.Contains("forcibly closed") ||
                text.Contains("sending the request") ||
                text.Contains("httprequestexception") ||
                text.Contains("webexception") ||
                text.Contains("network"))
            {
                return "Сервер временно недоступен. Локальные данные сохранены и будут отправлены позже.";
            }

            return raw;
        }
    }
}