using System.Collections.Generic;
using Balda.Features.Game.Domain;

namespace Balda.Features.Game.Bot
{
    public class BotMove
    {
        public int Row;
        public int Col;
        public string Letter = "";
        public string Word = "";
        public List<BoardPosition> Path = new();
    }
}
