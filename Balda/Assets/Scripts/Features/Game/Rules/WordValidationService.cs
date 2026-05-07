using System;
using Balda.Features.Game.Domain;

namespace Balda.Features.Game.Rules
{
    public class WordValidationService
    {
        private readonly WordDictionaryService dictionaryService;

        public WordValidationService(WordDictionaryService dictionaryService)
        {
            this.dictionaryService = dictionaryService;
        }

        public ValidationResult ValidateBasic(GameSession session, TurnDraft draft, string candidateWord)
        {
            string normalizedWord = dictionaryService.Normalize(candidateWord);

            if (session == null)
                return ValidationResult.Fail(FailureReason.SessionMissing, "Сессия игры не найдена.");

            if (session.Board == null)
                return ValidationResult.Fail(FailureReason.BoardMissing, "Поле игры не найдено.");

            if (draft == null || !draft.IsActive)
                return ValidationResult.Fail(FailureReason.DraftMissing, "Сначала поставьте новую букву.");

            if (string.IsNullOrWhiteSpace(normalizedWord))
                return ValidationResult.Fail(FailureReason.EmptyWord, "Слово пустое.");

            if (draft.SelectedPath == null || draft.SelectedPath.Count == 0)
                return ValidationResult.Fail(FailureReason.EmptyPath, "Проведи пальцем по буквам, чтобы собрать слово.");

            if (!draft.ContainsPosition(draft.Row, draft.Col))
                return ValidationResult.Fail(FailureReason.MissingNewLetter, "Слово должно проходить через новую букву.");

            if (normalizedWord.Length < 2)
                return ValidationResult.Fail(FailureReason.TooShort, "Слово слишком короткое.");

            if (!string.Equals(normalizedWord, draft.CandidateWord, StringComparison.OrdinalIgnoreCase))
                return ValidationResult.Fail(FailureReason.PathMismatch, "Слово не совпадает с выбранным маршрутом.");

            if (!dictionaryService.Contains(normalizedWord))
                return ValidationResult.Fail(FailureReason.NotInDictionary, "Такого слова нет в словаре.");

            if (session.UsedWords != null)
            {
                for (int i = 0; i < session.UsedWords.Count; i++)
                {
                    if (string.Equals(session.UsedWords[i], normalizedWord, StringComparison.OrdinalIgnoreCase))
                        return ValidationResult.Fail(FailureReason.AlreadyUsed, "Это слово уже использовалось.");
                }
            }

            return ValidationResult.Success(normalizedWord);
        }

        public enum FailureReason
        {
            None,
            SessionMissing,
            BoardMissing,
            DraftMissing,
            EmptyWord,
            EmptyPath,
            MissingNewLetter,
            TooShort,
            PathMismatch,
            NotInDictionary,
            AlreadyUsed
        }

        public readonly struct ValidationResult
        {
            public bool IsValid { get; }
            public string Message { get; }
            public string NormalizedWord { get; }
            public FailureReason Reason { get; }

            private ValidationResult(bool isValid, string message, string normalizedWord, FailureReason reason)
            {
                IsValid = isValid;
                Message = message;
                NormalizedWord = normalizedWord;
                Reason = reason;
            }

            public static ValidationResult Success(string normalizedWord)
            {
                return new ValidationResult(true, "", normalizedWord, FailureReason.None);
            }

            public static ValidationResult Fail(FailureReason reason, string message)
            {
                return new ValidationResult(false, message, "", reason);
            }
        }
    }
}