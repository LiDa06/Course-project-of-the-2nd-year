using Balda.Core.Navigation;
using Balda.Features.Game.Domain;
using Balda.Features.Game.Services;
using Balda.Features.Game.UI;
using Balda.Features.Profile.UI;
using Balda.Features.Settings.UI;
using Balda.Infrastructure.LocalStorage;
using Balda.UI.Common;
using TMPro;
using UnityEngine;

namespace Balda.Features.MainMenu.UI
{
    public class MainScreen : ScreenBase
    {
        [SerializeField] private SliderLabelsAligner slider;
        [SerializeField] private TMP_Text wins;
        [SerializeField] private TMP_Text losses;
        [SerializeField] private TMP_Text persent;

        private CloudMatchStatsSyncService cloudStatsSyncService;

        private void Awake()
        {
            cloudStatsSyncService = new CloudMatchStatsSyncService();
        }

        private void OnEnable()
        {
            if (LocalSettings.Instance == null)
                LocalSettings.Load();

            if (LocalPlayerData.Instance == null)
                LocalPlayerData.Load();

            slider.RefreshFromSettings();

            RefreshShortStats();
            TrySyncPendingStatsToCloud();
        }

        private void RefreshShortStats()
        {
            if (LocalPlayerData.Instance == null)
                LocalPlayerData.Load();

            if (LocalPlayerData.Instance == null)
                return;

            if (wins != null)
                wins.text = LocalPlayerData.Instance.Wins.ToString();

            if (losses != null)
                losses.text = LocalPlayerData.Instance.Losses.ToString();

            if (persent != null)
            {
                persent.text = LocalPlayerData.Instance.GamePlayed == 0
                    ? "0"
                    : $"{LocalPlayerData.Instance.Wins * 100 / LocalPlayerData.Instance.GamePlayed}%";
            }
        }

        private async void TrySyncPendingStatsToCloud()
        {
            if (LocalPlayerData.Instance == null)
                LocalPlayerData.Load();

            if (LocalPlayerData.Instance == null || !LocalPlayerData.Instance.HasUnsyncedStats)
                return;

            if (cloudStatsSyncService == null)
                cloudStatsSyncService = new CloudMatchStatsSyncService();

            bool synced = await cloudStatsSyncService.TrySyncAsync();
            if (synced)
                RefreshShortStats();
        }

        public void UpdateFieldSize()
        {
            if (LocalSettings.Instance == null)
                LocalSettings.Load();

            LocalSettings.Instance.BoardSize = slider.GetFieldSize();
            LocalSettings.Save();

            Debug.Log($"Saved board size = {LocalSettings.Instance.BoardSize}");
        }

        public void OnPlayClick()
        {
            UpdateFieldSize();

            if (LocalSettings.Instance == null)
                LocalSettings.Load();

            if (LocalPlayerData.Instance == null)
                LocalPlayerData.Load();

            GameStartOptionsHolder.Set(new GameStartOptions
            {
                BoardSize = LocalSettings.Instance.BoardSize,
                Mode = GameMode.Solo,
                BotDifficulty = ParseDifficulty(LocalSettings.Instance.BotDifficulty),
                PlayerOneName = GetLocalPlayerName(),
                PlayerTwoName = "Бот"
            });

            ScreenRouter.Instance.Show<GameScreen>();
        }

        public void OnRulesClick()
        {
            ScreenRouter.Instance.Show<RulesScreen>();
        }

        public void OnProfileButtonClick()
        {
            ScreenRouter.Instance.Show<ProfileScreen>();
        }

        public void OnSettingsButtonClick()
        {
            ScreenRouter.Instance.Show<SettingsScreen>();
        }

        private static string GetLocalPlayerName()
        {
            if (LocalPlayerData.Instance == null)
                LocalPlayerData.Load();

            string value = LocalPlayerData.Instance != null
                ? LocalPlayerData.Instance.LocalDisplayName
                : string.Empty;

            return string.IsNullOrWhiteSpace(value) ? "Игрок 1" : value.Trim();
        }

        private static BotDifficulty ParseDifficulty(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return BotDifficulty.Easy;

            switch (value.Trim().ToLowerInvariant())
            {
                case "medium":
                    return BotDifficulty.Medium;

                case "hard":
                    return BotDifficulty.Hard;

                default:
                    return BotDifficulty.Easy;
            }
        }
    }
}