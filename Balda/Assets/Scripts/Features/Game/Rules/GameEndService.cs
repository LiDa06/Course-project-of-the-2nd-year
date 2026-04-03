using Balda.Features.Game.Domain;

namespace Balda.Features.Game.Rules
{
    public class GameEndService
    {
        public bool HasAnyLegalPlacement(GameSession session)
        {
            if (session == null || session.Board == null)
                return false;

            for (int row = 0; row < session.Board.Size; row++)
            {
                for (int col = 0; col < session.Board.Size; col++)
                {
                    if (session.Board.CanPlaceNewLetter(row, col))
                        return true;
                }
            }

            return false;
        }

        public bool ShouldFinish(GameSession session)
        {
            if (session == null || session.Board == null)
                return true;

            if (session.IsFinished)
                return true;

            return !HasAnyLegalPlacement(session);
        }

        public int ResolveWinnerIndex(GameSession session)
        {
            if (session == null)
                return -1;

            if (session.PlayerOneScore > session.PlayerTwoScore)
                return 0;

            if (session.PlayerTwoScore > session.PlayerOneScore)
                return 1;

            return -1;
        }
    }
}
