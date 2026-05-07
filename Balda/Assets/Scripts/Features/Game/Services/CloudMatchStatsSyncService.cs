using System;
using System.Threading;
using System.Threading.Tasks;
using Balda.Infrastructure.LocalStorage;
using Balda.Infrastructure.Server.Auth;
using UnityEngine;

namespace Balda.Features.Game.Services
{
    public class CloudMatchStatsSyncService
    {
        private static readonly SemaphoreSlim SyncLock = new SemaphoreSlim(1, 1);

        public async Task<bool> TrySyncAsync()
        {
            if (LocalPlayerData.Instance == null)
                LocalPlayerData.Load();

            var local = LocalPlayerData.Instance;
            if (local == null)
                return false;

            // У гостя нет серверного профиля, поэтому его прогресс остается только локальным.
            if (local.IsGuest || string.IsNullOrWhiteSpace(local.CloudUserId))
                return false;

            // Если нечего отправлять, считаем, что состояние уже согласовано.
            if (!local.HasUnsyncedStats)
                return true;

            if (AuthServiceProvider.Auth == null || !AuthServiceProvider.Auth.IsSignedIn)
            {
                Debug.LogWarning("CloudMatchStatsSyncService: пользователь не авторизован, статистика будет синхронизирована позже.");
                return false;
            }

            // Не запускаем второй такой же запрос, пока первый еще не завершился.
            // Это устраняет гонку между FinishGame() и OnEnable() главного экрана.
            if (!await SyncLock.WaitAsync(0))
            {
                Debug.Log("CloudMatchStatsSyncService: синхронизация уже выполняется, повторный запуск пропущен.");
                return false;
            }

            try
            {
                // Данные могли успеть синхронизироваться, пока мы ждали блокировку.
                if (LocalPlayerData.Instance == null)
                    LocalPlayerData.Load();

                local = LocalPlayerData.Instance;
                if (local == null || local.IsGuest || string.IsNullOrWhiteSpace(local.CloudUserId) || !local.HasUnsyncedStats)
                    return true;

                var result = await AuthServiceProvider.Auth.SyncLocalStatsAndRecentGamesAsync();
                if (!result.Success)
                {
                    Debug.LogWarning("CloudMatchStatsSyncService: синхронизация статистики не выполнена: " + result.Message);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("CloudMatchStatsSyncService: синхронизация статистики будет повторена позже: " + ex.Message);
                return false;
            }
            finally
            {
                SyncLock.Release();
            }
        }
    }
}
