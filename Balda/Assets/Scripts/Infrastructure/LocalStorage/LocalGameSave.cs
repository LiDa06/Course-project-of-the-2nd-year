using System;
using System.Collections.Generic;

namespace Balda.Infrastructure.LocalStorage
{
    [Serializable]
    public class LocalGameSave
    {
        public string SessionId = "";
        public string Mode = "solo";          // solo, local, online
        public string Difficulty = "easy";    // easy, medium, hard

        public int BoardSize = 5;
        public string BoardJson = "";

        public int CurrentPlayerIndex = 0;
        public int PlayerOneScore = 0;
        public int PlayerTwoScore = 0;

        public List<string> UsedWords = new List<string>();

        public bool IsFinished = false;
        public long SavedAtTicks = DateTime.UtcNow.Ticks;
    }
}