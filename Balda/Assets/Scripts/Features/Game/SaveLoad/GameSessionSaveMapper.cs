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
                CurrentPlayerIndex = session.CurrentPlayerIndex,
                PlayerOneScore = session.PlayerOneScore,
                PlayerTwoScore = session.PlayerTwoScore,
                UsedWords = session.UsedWords != null
                    ? new List<string>(session.UsedWords)
                    : new List<string>(),
                IsFinished = session.IsFinished,
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
                CurrentPlayerIndex = save.CurrentPlayerIndex,
                PlayerOneScore = save.PlayerOneScore,
                PlayerTwoScore = save.PlayerTwoScore,
                UsedWords = save.UsedWords != null
                    ? new List<string>(save.UsedWords)
                    : new List<string>(),
                IsFinished = save.IsFinished
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
    }
}