using System.Collections.Generic;
using Balda.Features.Game.Domain;
using Balda.Features.Game.Rules;
using UnityEngine;

namespace Balda.Features.Game.Bot
{
    public class BotMoveSearchService
    {
        private readonly WordDictionaryService dictionaryService;
        private readonly BotWordPathFinder pathFinder;

        public BotMoveSearchService(WordDictionaryService dictionaryService)
        {
            this.dictionaryService = dictionaryService;
            pathFinder = new BotWordPathFinder();
        }

        public List<BotMove> CollectMoves(
            GameSession session,
            int minLength,
            int maxLength,
            int maxCollectedMoves,
            bool shufflePlacements,
            bool shuffleLetters)
        {
            var result = new List<BotMove>();

            if (session == null || session.Board == null)
                return result;

            var legalPlacements = GetLegalPlacements(session.Board);
            if (legalPlacements.Count == 0)
                return result;

            var words = dictionaryService.GetAllWords();
            if (words == null || words.Count == 0)
                return result;

            var usedWords = BuildUsedWordsSet(session);

            if (minLength > maxLength)
                return result;

            for (int i = 0; i < words.Count; i++)
            {
                string word = dictionaryService.Normalize(words[i]);
                if (string.IsNullOrWhiteSpace(word))
                    continue;

                if (word.Length < minLength || word.Length > maxLength)
                    continue;

                if (usedWords.Contains(word))
                    continue;

                if (TryFindMoveForWord(board: session.Board, word: word, legalPlacements: legalPlacements,
                    shufflePlacements: shufflePlacements, shuffleLetters: shuffleLetters, out BotMove move))
                {
                    result.Add(move);

                    if (result.Count >= maxCollectedMoves)
                        break;
                }
            }

            return result;
        }

        public int CountFilledCells(BoardState board)
        {
            if (board == null)
                return 0;

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

        private bool TryFindMoveForWord(
            BoardState board,
            string word,
            List<BoardPosition> legalPlacements,
            bool shufflePlacements,
            bool shuffleLetters,
            out BotMove move)
        {
            move = null;

            if (board == null || string.IsNullOrWhiteSpace(word) || legalPlacements == null || legalPlacements.Count == 0)
                return false;

            List<int> placementOrder = BuildIndexes(legalPlacements.Count, shufflePlacements);
            List<int> letterOrder = BuildIndexes(word.Length, shuffleLetters);

            for (int placementIdx = 0; placementIdx < placementOrder.Count; placementIdx++)
            {
                BoardPosition placement = legalPlacements[placementOrder[placementIdx]];

                for (int letterIdx = 0; letterIdx < letterOrder.Count; letterIdx++)
                {
                    int wordIndex = letterOrder[letterIdx];
                    string letter = word[wordIndex].ToString();

                    board.SetLetter(placement.Row, placement.Col, letter);

                    try
                    {
                        if (pathFinder.TryFindPath(board, word, placement.Row, placement.Col, out List<BoardPosition> path))
                        {
                            move = new BotMove
                            {
                                Row = placement.Row,
                                Col = placement.Col,
                                Letter = letter,
                                Word = word,
                                Path = path
                            };

                            return true;
                        }
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

        private static List<int> BuildIndexes(int count, bool shuffle)
        {
            var result = new List<int>(count);

            for (int i = 0; i < count; i++)
                result.Add(i);

            if (!shuffle)
                return result;

            for (int i = count - 1; i > 0; i--)
            {
                int swapIndex = Random.Range(0, i + 1);
                int temp = result[i];
                result[i] = result[swapIndex];
                result[swapIndex] = temp;
            }

            return result;
        }
    }
}