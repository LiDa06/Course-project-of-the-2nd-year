using System;

namespace Balda.Infrastructure.LocalStorage
{
    [Serializable]
    public class RecentGameInfo
    {
        public long FinishedAtTicks = 0;
        public string Mode = "solo";
        public int BoardSize = 5;
        public string Result = "draw"; // win, loss, draw
        public string OpponentName = "";
        public int PlayerOneScore = 0;
        public int PlayerTwoScore = 0;
        public int TurnCount = 0;
        public string BestWord = "";
        public int DurationSeconds = 0;
    }
}
