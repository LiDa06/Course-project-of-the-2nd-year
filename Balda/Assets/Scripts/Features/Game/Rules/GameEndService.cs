using System.Collections.Generic;
using Balda.Features.Game.Bot;
using Balda.Features.Game.Domain;

namespace Balda.Features.Game.Rules
{
    public class GameEndService
    {
        private readonly WordDictionaryService dictionaryService;
        private readonly BotWordPathFinder pathFinder;

        public GameEndService(WordDictionaryService dictionaryService)
        {
            this.dictionaryService = dictionaryService;
            pathFinder = new BotWordPathFinder();
        }

        public bool HasAnyValidMove(GameSession session)
        {
            if (session == null || session.Board == null || dictionaryService == null)
                return false;

            var legalPlacements = GetLegalPlacements(session.Board);
            if (legalPlacements.Count == 0)
                return false;

            var words = dictionaryService.GetAllWords();
            if (words == null || words.Count == 0)
                return false;

            var usedWords = BuildUsedWordsSet(session);
            int maxPossibleLength = CountFilledCells(session.Board) + 1;

            for (int i = 0; i < words.Count; i++)
            {
                string word = dictionaryService.Normalize(words[i]);
                if (string.IsNullOrWhiteSpace(word))
                    continue;

                if (word.Length < 2 || word.Length > maxPossibleLength)
                    continue;

                if (usedWords.Contains(word))
                    continue;

                if (TryFindMoveForWord(session.Board, word, legalPlacements))
                    return true;
            }

            return false;
        }

        public bool ShouldFinish(GameSession session)
        {
            if (session == null || session.Board == null)
                return true;

            if (session.IsFinished)
                return true;

            return !HasAnyValidMove(session);
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

        private bool TryFindMoveForWord(BoardState board, string word, List<BoardPosition> legalPlacements)
        {
            if (board == null || string.IsNullOrWhiteSpace(word) || legalPlacements == null)
                return false;

            for (int placementIndex = 0; placementIndex < legalPlacements.Count; placementIndex++)
            {
                BoardPosition placement = legalPlacements[placementIndex];

                for (int wordIndex = 0; wordIndex < word.Length; wordIndex++)
                {
                    string letter = word[wordIndex].ToString();

                    board.SetLetter(placement.Row, placement.Col, letter);

                    try
                    {
                        if (pathFinder.TryFindPath(board, word, placement.Row, placement.Col, out _))
                            return true;
                    }
                    finally
                    {
                        board.ClearCell(placement.Row, placement.Col);
                    }
                }
            }

            return false;
        }

        private static List<BoardPosition> GetLegalPlacements(BoardState board)
        {
            var result = new List<BoardPosition>();

            for (int row = 0; row < board.Size; row++)
            {
                for (int col = 0; col < board.Size; col++)
                {
                    if (board.CanPlaceNewLetter(row, col))
                        result.Add(new BoardPosition(row, col));
                }
            }

            return result;
        }

        private HashSet<string> BuildUsedWordsSet(GameSession session)
        {
            var result = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

            if (session?.UsedWords == null)
                return result;

            for (int i = 0; i < session.UsedWords.Count; i++)
            {
                string normalized = dictionaryService.Normalize(session.UsedWords[i]);
                if (!string.IsNullOrWhiteSpace(normalized))
                    result.Add(normalized);
            }

            return result;
        }

        private static int CountFilledCells(BoardState board)
        {
            int count = 0;

            for (int row = 0; row < board.Size; row++)
            {
                for (int col = 0; col < board.Size; col++)
                {
                    if (!board.IsEmpty(row, col))
                        count++;
                }
            }

            return count;
        }
    }
}