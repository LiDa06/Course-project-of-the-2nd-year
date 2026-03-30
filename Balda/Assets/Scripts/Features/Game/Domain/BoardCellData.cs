using System;

namespace Balda.Features.Game.Domain
{
    [Serializable]
    public class BoardCellData
    {
        public int Row;
        public int Col;
        public string Letter = "";
        public bool IsStartLetter;

        public bool IsEmpty => string.IsNullOrEmpty(Letter);
    }
}