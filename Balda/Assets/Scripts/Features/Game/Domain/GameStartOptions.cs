using System;

namespace Balda.Features.Game.Domain
{
    [Serializable]
    public class GameStartOptions
    {
        public int BoardSize = 5;
        public GameMode Mode = GameMode.Solo;
        public BotDifficulty BotDifficulty = BotDifficulty.Easy;

        public string PlayerOneName = "Игрок 1";
        public string PlayerTwoName = "Игрок 2";

        public bool UseBot => Mode == GameMode.Solo;
    }
}