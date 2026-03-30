using System;
using System.Collections.Generic;
using UnityEngine;

namespace Balda.Features.Game.Domain
{
    [Serializable]
    public class BoardState
    {
        public int Size;
        public List<BoardCellData> Cells = new();

        public BoardState()
        {
        }

        public BoardState(int size)
        {
            Initialize(size);
        }

        public void Initialize(int size)
        {
            if (size < 5)
                throw new ArgumentException("Board size must be at least 5.");

            Size = size;
            Cells = new List<BoardCellData>(size * size);

            for (int row = 0; row < size; row++)
            {
                for (int col = 0; col < size; col++)
                {
                    Cells.Add(new BoardCellData
                    {
                        Row = row,
                        Col = col,
                        Letter = "",
                        IsStartLetter = false
                    });
                }
            }
        }

        public bool IsInside(int row, int col)
        {
            return row >= 0 && row < Size && col >= 0 && col < Size;
        }

        public int Index(int row, int col)
        {
            if (!IsInside(row, col))
                throw new IndexOutOfRangeException($"Cell is outside board: [{row}, {col}]");

            return row * Size + col;
        }

        public BoardCellData GetCell(int row, int col)
        {
            return Cells[Index(row, col)];
        }

        public bool IsEmpty(int row, int col)
        {
            return GetCell(row, col).IsEmpty;
        }

        public void SetLetter(int row, int col, string letter, bool isStartLetter = false)
        {
            if (!IsInside(row, col))
                throw new IndexOutOfRangeException($"Cell is outside board: [{row}, {col}]");

            var cell = GetCell(row, col);
            cell.Letter = NormalizeLetter(letter);
            cell.IsStartLetter = isStartLetter;
        }

        public void ClearCell(int row, int col)
        {
            var cell = GetCell(row, col);
            cell.Letter = "";
            cell.IsStartLetter = false;
        }

        public void PlaceStartWord(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
                throw new ArgumentException("Start word is empty.");

            word = NormalizeWord(word);

            if (word.Length != Size)
                throw new ArgumentException($"Start word length must be exactly {Size}.");

            int centerRow = Size / 2;
            int startCol = (Size - word.Length) / 2;

            for (int i = 0; i < word.Length; i++)
            {
                SetLetter(centerRow, startCol + i, word[i].ToString(), true);
            }
        }

        public bool HasOrthogonalLetterNeighbour(int row, int col)
        {
            if (!IsInside(row, col))
                return false;

            int[,] dirs = new int[,]
            {
                { -1, 0 },
                { 1, 0 },
                { 0, -1 },
                { 0, 1 }
            };

            for (int i = 0; i < 4; i++)
            {
                int nextRow = row + dirs[i, 0];
                int nextCol = col + dirs[i, 1];

                if (!IsInside(nextRow, nextCol))
                    continue;

                if (!IsEmpty(nextRow, nextCol))
                    return true;
            }

            return false;
        }

        public bool CanPlaceNewLetter(int row, int col)
        {
            if (!IsInside(row, col))
                return false;

            if (!IsEmpty(row, col))
                return false;

            return HasOrthogonalLetterNeighbour(row, col);
        }

        private string NormalizeLetter(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";

            return value.Trim().ToUpperInvariant().Replace('Ё', 'Е');
        }

        private string NormalizeWord(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";

            return value.Trim().ToUpperInvariant().Replace('Ё', 'Е');
        }
    }
}