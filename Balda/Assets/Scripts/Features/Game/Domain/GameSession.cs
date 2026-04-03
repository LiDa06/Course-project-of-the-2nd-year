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

        public string PlayerOneDisplayName = "Игрок 1";
        public string PlayerTwoDisplayName = "Игрок 2";
        public string StartWord = "";

        public ParticipantType PlayerOneType = ParticipantType.Human;
        public ParticipantType PlayerTwoType = ParticipantType.Bot;

        public int CurrentPlayerIndex = 0;
        public int PlayerOneScore = 0;
        public int PlayerTwoScore = 0;
        public int TurnNumber = 1;

        public List<string> UsedWords = new();
        public List<GameMoveRecord> MoveHistory = new();

        public bool IsFinished = false;
        public int WinnerIndex = -2; // -2 = not resolved, -1 = draw, 0/1 = winner
        public long StartedAtTicks = DateTime.UtcNow.Ticks;
        public long FinishedAtTicks = 0;

        public string LastAcceptedWord = "";
        public int LastAcceptedScore = 0;
        public bool ResultApplied = false;

        public GamePhase Phase = GamePhase.WaitingForLetter;

        public bool IsCurrentTurnBot =>
            (CurrentPlayerIndex == 0 ? PlayerOneType : PlayerTwoType) == ParticipantType.Bot;
    }
}
