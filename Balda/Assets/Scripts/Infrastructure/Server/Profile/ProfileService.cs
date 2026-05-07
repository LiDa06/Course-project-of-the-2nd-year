using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Balda.Infrastructure.Server.Models;

namespace Balda.Infrastructure.Server.Profile
{
    public class ProfileService
    {
        private readonly Supabase.Client _client;

        public ProfileService(Supabase.Client client)
        {
            _client = client;
        }

        public async Task<ProfileEntity> GetByIdAsync(Guid userId)
        {
            var response = await _client
                .From<ProfileEntity>()
                .Where(x => x.Id == userId)
                .Get();

            return response.Models.FirstOrDefault();
        }

        public async Task<bool> IsUsernameAvailablePublicAsync(string username)
        {
            var normalized = NormalizeUsername(username);
            if (string.IsNullOrWhiteSpace(normalized))
                return false;

            var payload = new Dictionary<string, object>
            {
                ["p_username"] = normalized
            };

            var result = await _client.Rpc("is_username_available", payload);

            return ParseBooleanResponse(result?.Content);
        }

        public async Task<bool?> IsActiveEmailRegisteredPublicAsync(string email)
        {
            var normalized = NormalizeEmail(email);
            if (string.IsNullOrWhiteSpace(normalized))
                return false;

            var payload = new Dictionary<string, object>
            {
                ["p_email"] = normalized
            };

            try
            {
                var result = await _client.Rpc("is_active_email_registered", payload);
                return ParseBooleanResponse(result?.Content);
            }
            catch (Exception ex)
            {
                // Если RPC ещё не создан в Supabase, не ломаем вход:
                // Supabase Auth всё равно вернёт ошибку, которую обработает маппер.
                UnityEngine.Debug.LogWarning("IsActiveEmailRegisteredPublicAsync failed: " + ex.Message);
                return null;
            }
        }

        public async Task<bool> EnsureProfileAndStatsAsync(string username)
        {
            var normalized = NormalizeUsername(username);
            if (string.IsNullOrWhiteSpace(normalized))
                return false;

            var payload = new Dictionary<string, object>
            {
                ["p_username"] = normalized
            };

            var result = await _client.Rpc("ensure_profile_and_stats", payload);

            return ParseBooleanResponse(result?.Content);
        }

        public async Task<bool> UpdateUsernameAsync(Guid userId, string newUsername)
        {
            var profile = await GetByIdAsync(userId);
            if (profile == null)
                return false;

            profile.Username = NormalizeUsername(newUsername);
            await _client.From<ProfileEntity>().Update(profile);
            return true;
        }

        public async Task<bool> UpdateEmailMirrorAsync(Guid userId, string newEmail)
        {
            var profile = await GetByIdAsync(userId);
            if (profile == null)
                return false;

            profile.Email = NormalizeEmail(newEmail);
            await _client.From<ProfileEntity>().Update(profile);
            return true;
        }

        public async Task<bool> SoftDeleteAsync(Guid userId)
        {
            var profile = await GetByIdAsync(userId);
            if (profile == null)
                return false;

            profile.IsDeleted = true;
            profile.DeletedAt = DateTime.UtcNow;

            // В твоей таблице profiles.username имеет ограничение NOT NULL,
            // поэтому при soft delete нельзя записывать null.
            // Ставим техническое уникальное имя, чтобы старый логин освободился,
            // а ограничение NOT NULL не нарушалось.
            profile.Username = BuildDeletedUsername(userId);

            // Email не очищаем: Supabase Auth-пользователь при soft delete остаётся в auth.users,
            // поэтому эта почта фактически всё ещё занята. Если очистить зеркало email,
            // регистрация на тот же адрес будет выглядеть свободной в profiles, но упадёт в Auth.
            await _client.From<ProfileEntity>().Update(profile);
            return true;
        }

        private static string BuildDeletedUsername(Guid userId)
        {
            // Короткое значение, чтобы не упереться в возможное ограничение длины username.
            // Пример: del_a1b2c3d4e5f6
            var compactId = userId.ToString("N");
            return "del_" + compactId.Substring(0, 12);
        }

        private static string NormalizeUsername(string username)
        {
            return string.IsNullOrWhiteSpace(username)
                ? string.Empty
                : username.Trim();
        }

        private static string NormalizeEmail(string email)
        {
            return string.IsNullOrWhiteSpace(email)
                ? string.Empty
                : email.Trim().ToLowerInvariant();
        }

        private static bool ParseBooleanResponse(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return false;

            var text = content.Trim();

            if (text.StartsWith("\"") && text.EndsWith("\"") && text.Length >= 2)
                text = text.Substring(1, text.Length - 2);

            if (bool.TryParse(text, out var parsed))
                return parsed;

            if (text == "1")
                return true;

            if (text == "0")
                return false;

            return false;
        }
    }
}