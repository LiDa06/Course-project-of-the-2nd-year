using System;

namespace Balda.Features.Game.Services
{
    [Serializable]
    public class SuggestedWordEntry
    {
        public string Word = "";
        public string PlayerName = "";
        public string Email = "";
        public int BoardSize = 5;
        public long SuggestedAtTicks = 0;
    }
}