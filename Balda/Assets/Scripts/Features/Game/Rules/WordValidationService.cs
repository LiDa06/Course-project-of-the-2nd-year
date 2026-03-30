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
                return ValidationResult.Fail("Сессия игры не найдена.");

            if (session.Board == null)
                return ValidationResult.Fail("Поле игры не найдено.");

            if (draft == null || !draft.IsActive)
                return ValidationResult.Fail("Сначала поставьте новую букву.");

            if (string.IsNullOrWhiteSpace(normalizedWord))
                return ValidationResult.Fail("Слово пустое.");

            if (draft.SelectedPath == null || draft.SelectedPath.Count == 0)
                return ValidationResult.Fail("Проведи пальцем по буквам, чтобы собрать слово.");

            if (!draft.ContainsPosition(draft.Row, draft.Col))
                return ValidationResult.Fail("Слово должно проходить через новую букву.");

            if (normalizedWord.Length < 2)
                return ValidationResult.Fail("Слово слишком короткое.");

            if (!string.Equals(normalizedWord, draft.CandidateWord, StringComparison.OrdinalIgnoreCase))
                return ValidationResult.Fail("Слово не совпадает с выбранным маршрутом.");

            if (!dictionaryService.Contains(normalizedWord))
                return ValidationResult.Fail("Такого слова нет в словаре.");

            if (session.UsedWords != null)
            {
                for (int i = 0; i < session.UsedWords.Count; i++)
                {
                    if (string.Equals(session.UsedWords[i], normalizedWord, StringComparison.OrdinalIgnoreCase))
                        return ValidationResult.Fail("Это слово уже использовалось.");
                }
            }

            return ValidationResult.Success(normalizedWord);
        }

        public readonly struct ValidationResult
        {
            public bool IsValid { get; }
            public string Message { get; }
            public string NormalizedWord { get; }

            private ValidationResult(bool isValid, string message, string normalizedWord)
            {
                IsValid = isValid;
                Message = message;
                NormalizedWord = normalizedWord;
            }

            public static ValidationResult Success(string normalizedWord)
            {
                return new ValidationResult(true, "", normalizedWord);
            }

            public static ValidationResult Fail(string message)
            {
                return new ValidationResult(false, message, "");
            }
        }
    }
}