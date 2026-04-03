using System.Collections.Generic;
using Balda.Features.Game.Domain;
using Balda.Features.Game.Rules;
using UnityEngine;

namespace Balda.Features.Game.Bot
{
    public class EasyBotMoveProvider : IBotMoveProvider
    {
        private const int PreferredMaxWordLength = 6;
        private const int MaxCollectedMoves = 8;

        private readonly WordDictionaryService dictionaryService;
        private readonly BotWordPathFinder pathFinder;

        public EasyBotMoveProvider(WordDictionaryService dictionaryService)
        {
            this.dictionaryService = dictionaryService;
            pathFinder = new BotWordPathFinder();
        }

        public bool TryFindMove(GameSession session, out BotMove move)
        {
            move = null;

            if (session == null || session.Board == null)
                return false;

            var legalPlacements = GetLegalPlacements(session.Board);
            if (legalPlacements.Count == 0)
                return false;

            var words = dictionaryService.GetAllWords();
            if (words == null || words.Count == 0)
                return false;

            var usedWords = BuildUsedWordsSet(session);
            int maxPossibleLength = CountFilledCells(session.Board) + 1;

            var candidates = CollectMoves(words, session.Board, legalPlacements, usedWords, 2, Mathf.Min(PreferredMaxWordLength, maxPossibleLength));
            if (candidates.Count == 0 && maxPossibleLength > PreferredMaxWordLength)
            {
                candidates = CollectMoves(words, session.Board, legalPlacements, usedWords, PreferredMaxWordLength + 1, maxPossibleLength);
            }

            if (candidates.Count == 0)
                return false;

            move = candidates[Random.Range(0, candidates.Count)];
            return move != null;
        }

        private List<BotMove> CollectMoves(
            IReadOnlyList<string> words,
            BoardState board,
            List<BoardPosition> legalPlacements,
            HashSet<string> usedWords,
            int minLength,
            int maxLength)
        {
            var result = new List<BotMove>();

            if (minLength > maxLength)
                return result;

            for (int i = 0; i < words.Count; i++)
            {
                string word = words[i];
                if (string.IsNullOrWhiteSpace(word))
                    continue;

                if (word.Length < minLength || word.Length > maxLength)
                    continue;

                if (usedWords.Contains(word))
                    continue;

                if (TryFindMoveForWord(board, word, legalPlacements, out BotMove move))
                {
                    result.Add(move);
                    if (result.Count >= MaxCollectedMoves)
                        break;
                }
            }

            return result;
        }

        private bool TryFindMoveForWord(BoardState board, string word, List<BoardPosition> legalPlacements, out BotMove move)
        {
            move = null;

            if (board == null || string.IsNullOrWhiteSpace(word) || legalPlacements == null || legalPlacements.Count == 0)
                return false;

            List<int> placementOrder = BuildShuffledIndexes(legalPlacements.Count);
            List<int> letterOrder = BuildShuffledIndexes(word.Length);

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

        private static List<int> BuildShuffledIndexes(int count)
        {
            var result = new List<int>(count);
            for (int i = 0; i < count; i++)
                result.Add(i);

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
