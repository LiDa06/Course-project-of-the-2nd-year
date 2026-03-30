namespace Balda.Features.Game.Rules
{
    public class ScoreCalculator
    {
        public int CalculateWordScore(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
                return 0;

            return word.Trim().Length;
        }
    }
}