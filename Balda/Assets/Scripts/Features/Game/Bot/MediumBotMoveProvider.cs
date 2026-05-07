using System.Collections.Generic;
using Balda.Features.Game.Domain;
using Balda.Features.Game.Rules;
using UnityEngine;

namespace Balda.Features.Game.Bot
{
    public class MediumBotMoveProvider : IBotMoveProvider
    {
        private const int TopMovePoolSize = 3;
        private const int MaxCollectedMoves = 256;

        private readonly BotMoveSearchService searchService;

        public MediumBotMoveProvider(WordDictionaryService dictionaryService)
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
                shufflePlacements: true,
                shuffleLetters: true);

            if (candidates == null || candidates.Count == 0)
                return false;

            candidates.Sort((a, b) =>
            {
                int scoreA = GetScore(a);
                int scoreB = GetScore(b);
                return scoreB.CompareTo(scoreA);
            });

            int poolSize = Mathf.Min(TopMovePoolSize, candidates.Count);
            move = candidates[Random.Range(0, poolSize)];
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