using System.Collections.Generic;
using Balda.Features.Game.Domain;

namespace Balda.Features.Game.Bot
{
    public class BotWordPathFinder
    {
        private static readonly int[] DirRow = { -1, 1, 0, 0 };
        private static readonly int[] DirCol = { 0, 0, -1, 1 };

        public bool TryFindPath(BoardState board, string word, int requiredRow, int requiredCol, out List<BoardPosition> path)
        {
            path = null;

            if (board == null || string.IsNullOrWhiteSpace(word))
                return false;

            int size = board.Size;
            if (size <= 0)
                return false;

            bool[,] visited = new bool[size, size];

            for (int row = 0; row < size; row++)
            {
                for (int col = 0; col < size; col++)
                {
                    var cell = board.GetCell(row, col);
                    if (cell == null || string.IsNullOrWhiteSpace(cell.Letter))
                        continue;

                    if (cell.Letter[0] != word[0])
                        continue;

                    var currentPath = new List<BoardPosition>(word.Length);
                    if (Search(board, word, 0, row, col, requiredRow, requiredCol, false, visited, currentPath))
                    {
                        path = currentPath;
                        return true;
                    }
                }
            }

            return false;
        }

        private bool Search(
            BoardState board,
            string word,
            int index,
            int row,
            int col,
            int requiredRow,
            int requiredCol,
            bool requiredIncluded,
            bool[,] visited,
            List<BoardPosition> path)
        {
            if (!board.IsInside(row, col))
                return false;

            if (visited[row, col])
                return false;

            var cell = board.GetCell(row, col);
            if (cell == null || string.IsNullOrWhiteSpace(cell.Letter))
                return false;

            if (cell.Letter[0] != word[index])
                return false;

            bool nextRequiredIncluded = requiredIncluded || (row == requiredRow && col == requiredCol);

            visited[row, col] = true;
            path.Add(new BoardPosition(row, col));

            if (index == word.Length - 1)
            {
                if (nextRequiredIncluded)
                    return true;

                path.RemoveAt(path.Count - 1);
                visited[row, col] = false;
                return false;
            }

            for (int i = 0; i < 4; i++)
            {
                int nextRow = row + DirRow[i];
                int nextCol = col + DirCol[i];

                if (Search(board, word, index + 1, nextRow, nextCol, requiredRow, requiredCol, nextRequiredIncluded, visited, path))
                    return true;
            }

            path.RemoveAt(path.Count - 1);
            visited[row, col] = false;
            return false;
        }
    }
}
