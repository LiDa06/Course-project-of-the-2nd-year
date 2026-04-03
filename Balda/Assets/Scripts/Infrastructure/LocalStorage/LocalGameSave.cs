using System;
using System.Collections.Generic;
using Balda.Features.Game.Domain;

namespace Balda.Infrastructure.LocalStorage
{
    [Serializable]
    public class LocalGameSave
    {
        public string SessionId = "";
        public string Mode = "solo";
        public string Difficulty = "easy";

        public int BoardSize = 5;
        public string BoardJson = "";

        public string PlayerOneDisplayName = "";
        public string PlayerTwoDisplayName = "";
        public string StartWord = "";
        public string PlayerOneType = "Human";
        public string PlayerTwoType = "Bot";

        public int CurrentPlayerIndex = 0;
        public int PlayerOneScore = 0;
        public int PlayerTwoScore = 0;
        public int TurnNumber = 1;

        public List<string> UsedWords = new();
        public List<GameMoveRecord> MoveHistory = new();

        public bool IsFinished = false;
        public int WinnerIndex = -2;
        public long StartedAtTicks = 0;
        public long FinishedAtTicks = 0;
        public string LastAcceptedWord = "";
        public int LastAcceptedScore = 0;
        public bool ResultApplied = false;
        public string Phase = "WaitingForLetter";
        public long SavedAtTicks = DateTime.UtcNow.Ticks;
    }
}
