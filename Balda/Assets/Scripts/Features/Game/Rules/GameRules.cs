using Balda.Features.Game.Domain;

namespace Balda.Features.Game.Rules
{
    public class GameRules
    {
        public bool CanPlaceNewLetter(GameSession session, int row, int col)
        {
            if (session == null || session.Board == null)
                return false;

            return session.Board.CanPlaceNewLetter(row, col);
        }

        public bool IsWordAlreadyUsed(GameSession session, string word)
        {
            if (session == null || session.UsedWords == null || string.IsNullOrWhiteSpace(word))
                return false;

            string normalized = NormalizeWord(word);

            for (int i = 0; i < session.UsedWords.Count; i++)
            {
                if (NormalizeWord(session.UsedWords[i]) == normalized)
                    return true;
            }

            return false;
        }

        public bool CanAcceptWord(GameSession session, string word)
        {
            if (session == null)
                return false;

            if (string.IsNullOrWhiteSpace(word))
                return false;

            string normalized = NormalizeWord(word);

            if (normalized.Length < 2)
                return false;

            if (IsWordAlreadyUsed(session, normalized))
                return false;

            return true;
        }

        public string NormalizeWord(string word)
        {
            return string.IsNullOrWhiteSpace(word)
                ? ""
                : word.Trim().ToUpperInvariant();
        }
    }
}