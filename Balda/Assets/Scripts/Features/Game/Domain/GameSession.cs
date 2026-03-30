using System;
using System.Collections.Generic;

namespace Balda.Features.Game.Domain
{
    [Serializable]
    public class GameSession
    {
        public string SessionId = Guid.NewGuid().ToString();
        public int BoardSize = 5;
        public BoardState Board;
        public GameMode Mode = GameMode.Solo;
        public string Difficulty = "easy";

        public int CurrentPlayerIndex = 0;
        public int PlayerOneScore = 0;
        public int PlayerTwoScore = 0;

        public List<string> UsedWords = new();

        public bool IsFinished = false;
    }
}