using System;
using Balda.Infrastructure.LocalStorage;
using TMPro;
using UnityEngine;

namespace Balda.Features.Profile.UI
{
    public class RecentGameCardView : MonoBehaviour
    {
        [Header("Texts")]
        [SerializeField] private TMP_Text leftTopText;
        [SerializeField] private TMP_Text rightTopText;
        [SerializeField] private TMP_Text leftBottomText;
        [SerializeField] private TMP_Text rightBottomText;

        public void Bind(RecentGameInfo game)
        {
            if (game == null)
            {
                leftTopText.text = "—";
                rightTopText.text = "—";
                leftBottomText.text = "—";
                rightBottomText.text = "—";
                return;
            }

            leftTopText.text = game.OpponentName;
            rightTopText.text = TranslateResult(game.Result);
            leftBottomText.text = FormatDate(game.FinishedAtTicks);
            rightBottomText.text = $"{game.PlayerOneScore}:{game.PlayerTwoScore}";
        }

        private static string TranslateResult(string result)
        {
            if (string.IsNullOrWhiteSpace(result))
                return "—";

            return result.ToLower() switch
            {
                "win" => "Победа",
                "loss" => "Поражение",
                "draw" => "Ничья",
                _ => result
            };
        }

        private static string FormatDate(long ticks)
        {
            if (ticks <= 0)
                return "—";

            try
            {
                DateTime date = new DateTime(ticks, DateTimeKind.Utc).ToLocalTime();
                return date.ToString("dd.MM.yyyy");
            }
            catch
            {
                return "—";
            }
        }
    }
}