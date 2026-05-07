using System;
using System.Collections.Generic;
using Balda.Core.Navigation;
using Balda.Features.MainMenu.UI;
using Balda.Infrastructure.LocalStorage;
using Balda.UI.Common;
using TMPro;
using UnityEngine;

namespace Balda.Features.Profile.UI
{
    public class ProfileScreen : ScreenBase
    {
        [Header("Account")]
        [SerializeField] private TMP_Text userNameText;
        [SerializeField] private TMP_Text emailText;
        [SerializeField] private TMP_Text createdAtText;

        [Header("Stats")]
        [SerializeField] private TMP_Text gamesPlayedText;
        [SerializeField] private TMP_Text winsText;
        [SerializeField] private TMP_Text lossesText;
        [SerializeField] private TMP_Text winRateText;
        [SerializeField] private TMP_Text wordsMadeUp;
        [SerializeField] private TMP_Text averageWordLenText;
        [SerializeField] private TMP_Text longestWordText;
        [SerializeField] private TMP_Text pointsText;

        [Header("Recent Games")]
        [SerializeField] private RecentGameCardView[] recentGameCards;

        [Header("Achievements")]
        [SerializeField] private TMP_Text[] achievementTexts;

        private void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (LocalPlayerData.Instance == null)
                LocalPlayerData.Load();

            var data = LocalPlayerData.Instance;
            if (data == null)
                return;

            string displayName = string.IsNullOrWhiteSpace(data.LocalDisplayName)
                ? "Гость"
                : data.LocalDisplayName;

            string email = string.IsNullOrWhiteSpace(data.Email)
                ? "—"
                : data.Email;

            if (userNameText != null)
                userNameText.text = displayName;

            if (emailText != null)
                emailText.text = email;

            if (createdAtText != null)
                createdAtText.text = FormatCreatedAt(data.CreatedAtTicks);

            int games = data.GamePlayed;
            int wins = data.Wins;
            int losses = data.Losses;
            int winRate = games > 0
                ? Mathf.RoundToInt((float)wins / games * 100f)
                : 0;

            if (gamesPlayedText != null)
                gamesPlayedText.text = games.ToString();

            if (winsText != null)
                winsText.text = wins.ToString();

            if (lossesText != null)
                lossesText.text = losses.ToString();

            if (winRateText != null)
                winRateText.text = $"{winRate}%";

            if (wordsMadeUp != null)
                wordsMadeUp.text = data.WordsMadeUp.ToString();

            if (averageWordLenText != null)
                averageWordLenText.text = data.AverageWordLen.ToString();

            if (longestWordText != null)
                longestWordText.text = data.LongestWord.ToString();

            if (pointsText != null)
                pointsText.text = data.PointsForAllTime.ToString();

            FillRecentGames(data.RecentGames);
        }

        public void OnBackClick()
        {
            ScreenRouter.Instance.Show<MainScreen>();
        }

        private void FillRecentGames(List<RecentGameInfo> recentGames)
        {
            if (recentGameCards == null || recentGameCards.Length == 0)
                return;

            for (int i = 0; i < recentGameCards.Length; i++)
            {
                if (recentGameCards[i] == null)
                    continue;

                RecentGameInfo game = null;
                if (recentGames != null && i < recentGames.Count)
                    game = recentGames[i];

                recentGameCards[i].Bind(game);
            }
        }

        private static string FormatCreatedAt(long createdAtTicks)
        {
            if (createdAtTicks <= 0)
                return "—";

            try
            {
                DateTime date = new DateTime(createdAtTicks, DateTimeKind.Utc).ToLocalTime();
                return date.ToString("dd.MM.yyyy HH:mm");
            }
            catch
            {
                return "—";
            }
        }
    }
}