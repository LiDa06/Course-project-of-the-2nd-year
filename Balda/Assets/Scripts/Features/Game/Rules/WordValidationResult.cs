namespace Balda.Features.Game.Rules
{
    public class WordValidationResult
    {
        public bool IsValid;
        public string Message;
        public string NormalizedWord;

        public static WordValidationResult Success(string normalizedWord)
        {
            return new WordValidationResult
            {
                IsValid = true,
                Message = "",
                NormalizedWord = normalizedWord
            };
        }

        public static WordValidationResult Fail(string message)
        {
            return new WordValidationResult
            {
                IsValid = false,
                Message = message,
                NormalizedWord = ""
            };
        }
    }
}
