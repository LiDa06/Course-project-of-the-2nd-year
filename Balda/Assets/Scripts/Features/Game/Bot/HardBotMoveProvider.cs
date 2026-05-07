using System.Collections.Generic;
using Balda.Features.Game.Domain;
using Balda.Features.Game.Rules;
using UnityEngine;

namespace Balda.Features.Game.Bot
{
    public class HardBotMoveProvider : IBotMoveProvider
    {
        private const int MaxCollectedMoves = 2048;

        private readonly BotMoveSearchService searchService;

        public HardBotMoveProvider(WordDictionaryService dictionaryService)
        {
            searchService = new BotMoveSearchService(dictionaryService);
        }

        public bool TryFindMove(GameSession session, out BotMove move)
        {
            move = null;

            if (session == null || session.Board == null)
                return false;

            int maxPossibleLength = searchService.CountFilledCells(session.Board) + 1;

            List<BotMove> candidates = searchService.CollectMoves(
                session: session,
                minLength: 2,
                maxLength: maxPossibleLength,
                maxCollectedMoves: MaxCollectedMoves,
                shufflePlacements: false,
                shuffleLetters: false);

            if (candidates == null || candidates.Count == 0)
                return false;

            int bestScore = -1;
            List<BotMove> bestMoves = new List<BotMove>();

            for (int i = 0; i < candidates.Count; i++)
            {
                BotMove candidate = candidates[i];
                int score = GetScore(candidate);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMoves.Clear();
                    bestMoves.Add(candidate);
                }
                else if (score == bestScore)
                {
                    bestMoves.Add(candidate);
                }
            }

            if (bestMoves.Count == 0)
                return false;

            move = bestMoves[Random.Range(0, bestMoves.Count)];
            return move != null;
        }

        private static int GetScore(BotMove move)
        {
            if (move == null || string.IsNullOrWhiteSpace(move.Word))
                return 0;

            return move.Word.Trim().Length;
        }
    }
}