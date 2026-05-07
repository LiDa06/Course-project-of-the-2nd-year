using Balda.Features.Game.Domain;
using Balda.Infrastructure.LocalStorage;

namespace Balda.Features.Game.Services
{
    public static class GameStartOptionsHolder
    {
        public static GameStartOptions Current { get; private set; } = CreateDefault();

        public static void Set(GameStartOptions options)
        {
            Current = options ?? CreateDefault();
        }

        public static void Reset()
        {
            Current = CreateDefault();
        }

        private static GameStartOptions CreateDefault()
        {
            if (LocalPlayerData.Instance == null)
                LocalPlayerData.Load();

            string localName = LocalPlayerData.Instance != null &&
                               !string.IsNullOrWhiteSpace(LocalPlayerData.Instance.LocalDisplayName)
                ? LocalPlayerData.Instance.LocalDisplayName
                : "Игрок 1";

            return new GameStartOptions
            {
                BoardSize = LocalSettings.Instance != null ? LocalSettings.Instance.BoardSize : 5,
                Mode = GameMode.Solo,
                BotDifficulty = BotDifficulty.Easy,
                PlayerOneName = localName,
                PlayerTwoName = "Бот"
            };
        }
    }
}