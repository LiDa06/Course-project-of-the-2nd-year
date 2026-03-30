using System;
using System.Collections.Generic;

namespace Balda.Features.Game.Domain
{
    [Serializable]
    public class TurnDraft
    {
        public int Row = -1;
        public int Col = -1;
        public string Letter = "";
        public string CandidateWord = "";
        public List<BoardPosition> SelectedPath = new();

        public bool IsActive =>
            Row >= 0 &&
            Col >= 0 &&
            !string.IsNullOrWhiteSpace(Letter);

        public void Start(int row, int col, string letter)
        {
            Row = row;
            Col = col;
            Letter = Normalize(letter);
            CandidateWord = "";
            SelectedPath.Clear();
        }

        public void ClearSelection()
        {
            CandidateWord = "";
            SelectedPath.Clear();
        }

        public bool ContainsPosition(int row, int col)
        {
            for (int i = 0; i < SelectedPath.Count; i++)
            {
                if (SelectedPath[i].Row == row && SelectedPath[i].Col == col)
                    return true;
            }

            return false;
        }

        public void Clear()
        {
            Row = -1;
            Col = -1;
            Letter = "";
            CandidateWord = "";
            SelectedPath.Clear();
        }

        private string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? ""
                : value.Trim().ToUpperInvariant().Replace('Ё', 'Е');
        }
    }
}
