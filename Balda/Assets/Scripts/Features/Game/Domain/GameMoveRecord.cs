using System;
using System.Collections.Generic;

namespace Balda.Features.Game.Domain
{
    [Serializable]
    public class GameMoveRecord
    {
        public int TurnNumber = 0;
        public int PlayerIndex = -1;
        public string PlayerDisplayName = "";
        public int PlacedRow = -1;
        public int PlacedCol = -1;
        public string PlacedLetter = "";
        public string Word = "";
        public int Score = 0;
        public bool IsStartRecord = false;
        public string BoardJson = "";
        public List<BoardPosition> WordPath = new();
    }
}
