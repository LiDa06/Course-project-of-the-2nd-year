using System;
using System.Collections.Generic;
using Balda.Features.Game.Domain;
using Balda.Infrastructure.LocalStorage;
using UnityEngine;

namespace Balda.Features.Game.SaveLoad
{
    public static class GameSessionSaveMapper
    {
        public static LocalGameSave ToSave(GameSession session)
        {
            if (session == null)
                return null;

            return new LocalGameSave
            {
                SessionId = session.SessionId ?? "",
                Mode = ToModeString(session.Mode),
                Difficulty = session.Difficulty ?? "easy",
                BoardSize = session.BoardSize,
                BoardJson = session.Board != null ? JsonUtility.ToJson(session.Board) : "",
                PlayerOneDisplayName = session.PlayerOneDisplayName ?? "",
                PlayerTwoDisplayName = session.PlayerTwoDisplayName ?? "",
                StartWord = session.StartWord ?? "",
                PlayerOneType = session.PlayerOneType.ToString(),
                PlayerTwoType = session.PlayerTwoType.ToString(),
                CurrentPlayerIndex = session.CurrentPlayerIndex,
                PlayerOneScore = session.PlayerOneScore,
                PlayerTwoScore = session.PlayerTwoScore,
                TurnNumber = session.TurnNumber,
                UsedWords = session.UsedWords != null
                    ? new List<string>(session.UsedWords)
                    : new List<string>(),
                MoveHistory = session.MoveHistory != null
                    ? new List<GameMoveRecord>(session.MoveHistory)
                    : new List<GameMoveRecord>(),
                IsFinished = session.IsFinished,
                WinnerIndex = session.WinnerIndex,
                StartedAtTicks = session.StartedAtTicks,
                FinishedAtTicks = session.FinishedAtTicks,
                LastAcceptedWord = session.LastAcceptedWord ?? "",
                LastAcceptedScore = session.LastAcceptedScore,
                ResultApplied = session.ResultApplied,
                Phase = session.Phase.ToString(),
                SavedAtTicks = DateTime.UtcNow.Ticks
            };
        }

        public static GameSession FromSave(LocalGameSave save)
        {
            if (save == null)
                return null;

            BoardState board = null;

            if (!string.IsNullOrWhiteSpace(save.BoardJson))
            {
                try
                {
                    board = JsonUtility.FromJson<BoardState>(save.BoardJson);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("GameSessionSaveMapper.FromSave: failed to parse board json: " + ex.Message);
                }
            }

            if (board == null)
                board = new BoardState(save.BoardSize);

            return new GameSession
            {
                SessionId = string.IsNullOrWhiteSpace(save.SessionId)
                    ? Guid.NewGuid().ToString()
                    : save.SessionId,
                BoardSize = save.BoardSize,
                Board = board,
                Mode = ParseMode(save.Mode),
                Difficulty = string.IsNullOrWhiteSpace(save.Difficulty) ? "easy" : save.Difficulty,
                PlayerOneDisplayName = save.PlayerOneDisplayName ?? "",
                PlayerTwoDisplayName = save.PlayerTwoDisplayName ?? "",
                StartWord = save.StartWord ?? "",
                PlayerOneType = ParseParticipantType(save.PlayerOneType, ParticipantType.Human),
                PlayerTwoType = ParseParticipantType(save.PlayerTwoType, ParticipantType.Bot),
                CurrentPlayerIndex = save.CurrentPlayerIndex,
                PlayerOneScore = save.PlayerOneScore,
                PlayerTwoScore = save.PlayerTwoScore,
                TurnNumber = save.TurnNumber <= 0 ? 1 : save.TurnNumber,
                UsedWords = save.UsedWords != null
                    ? new List<string>(save.UsedWords)
                    : new List<string>(),
                MoveHistory = save.MoveHistory != null
                    ? new List<GameMoveRecord>(save.MoveHistory)
                    : new List<GameMoveRecord>(),
                IsFinished = save.IsFinished,
                WinnerIndex = save.WinnerIndex,
                StartedAtTicks = save.StartedAtTicks > 0 ? save.StartedAtTicks : DateTime.UtcNow.Ticks,
                FinishedAtTicks = save.FinishedAtTicks,
                LastAcceptedWord = save.LastAcceptedWord ?? "",
                LastAcceptedScore = save.LastAcceptedScore,
                ResultApplied = save.ResultApplied,
                Phase = ParsePhase(save.Phase, save.IsFinished)
            };
        }

        private static string ToModeString(GameMode mode)
        {
            return mode switch
            {
                GameMode.Solo => "solo",
                GameMode.LocalVersus => "local",
                GameMode.Online => "online",
                _ => "solo"
            };
        }

        private static GameMode ParseMode(string mode)
        {
            if (string.IsNullOrWhiteSpace(mode))
                return GameMode.Solo;

            mode = mode.Trim().ToLowerInvariant();

            return mode switch
            {
                "solo" => GameMode.Solo,
                "local" => GameMode.LocalVersus,
                "online" => GameMode.Online,
                _ => GameMode.Solo
            };
        }

        private static GamePhase ParsePhase(string value, bool isFinished)
        {
            if (isFinished)
                return GamePhase.Finished;

            if (string.IsNullOrWhiteSpace(value))
                return GamePhase.WaitingForLetter;

            return Enum.TryParse(value, true, out GamePhase parsed)
                ? parsed
                : GamePhase.WaitingForLetter;
        }

        private static ParticipantType ParseParticipantType(string value, ParticipantType fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            return Enum.TryParse(value, true, out ParticipantType parsed)
                ? parsed
                : fallback;
        }
    }
}
