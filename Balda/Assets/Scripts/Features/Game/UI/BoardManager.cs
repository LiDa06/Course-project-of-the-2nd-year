using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Balda.Features.Game.Domain;

namespace Balda.Features.Game.UI
{
    public class BoardManager : MonoBehaviour
    {
        private const float Spacing = 20f;

        [Header("References")]
        [SerializeField] private RectTransform boardArea;
        [SerializeField] private GridLayoutGroup gridLayout;
        [SerializeField] private BoardCellView cellPrefab;

        [Header("Layout")]
        [SerializeField, Range(0.6f, 1f)] private float widthPercent = 0.9f;

        private int boardSize;
        private BoardCellView[,] cells;
        private BoardState currentBoard;

        public event Action<int, int> CellClicked;

        private void OnRectTransformDimensionsChange()
        {
            if (!Application.isPlaying || boardArea == null || gridLayout == null || boardSize <= 0)
                return;

            ApplyBoardWidth();
            UpdateGridCellSize();
        }

        public void BuildBoard(BoardState boardState)
        {
            if (boardState == null)
            {
                Debug.LogError("BoardManager.BuildBoard: boardState is null.");
                return;
            }

            currentBoard = boardState;
            boardSize = boardState.Size;

            ClearOldCells();
            ApplyBoardWidth();
            ConfigureGrid();
            CreateCells();
            Render(boardState);
        }

        public void Render(BoardState boardState)
        {
            if (boardState == null || cells == null)
                return;

            currentBoard = boardState;

            for (int row = 0; row < boardState.Size; row++)
            {
                for (int col = 0; col < boardState.Size; col++)
                {
                    var cellData = boardState.GetCell(row, col);
                    cells[row, col].Render(cellData);
                }
            }
        }

        public void RefreshSelection(IReadOnlyList<BoardPosition> selectedPath, int placedRow, int placedCol, bool hasPlacedCell)
        {
            if (cells == null)
                return;

            for (int row = 0; row < boardSize; row++)
            {
                for (int col = 0; col < boardSize; col++)
                {
                    bool isSelected = Contains(selectedPath, row, col);
                    bool isPlacedLetterCell = hasPlacedCell && row == placedRow && col == placedCol;

                    cells[row, col].SetSelectionState(isSelected, isPlacedLetterCell);
                }
            }
        }

        public BoardCellView GetCellView(int row, int col)
        {
            if (cells == null || row < 0 || row >= boardSize || col < 0 || col >= boardSize)
                return null;

            return cells[row, col];
        }

        private bool Contains(IReadOnlyList<BoardPosition> path, int row, int col)
        {
            if (path == null)
                return false;

            for (int i = 0; i < path.Count; i++)
            {
                if (path[i].Row == row && path[i].Col == col)
                    return true;
            }

            return false;
        }

        private void ApplyBoardWidth()
        {
            RectTransform parent = boardArea.parent as RectTransform;
            if (parent == null)
                return;

            float targetWidth = parent.rect.width * widthPercent;
            boardArea.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
            boardArea.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetWidth);
        }

        private void ConfigureGrid()
        {
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = boardSize;
            gridLayout.spacing = new Vector2(Spacing, Spacing);

            UpdateGridCellSize();
        }

        private void UpdateGridCellSize()
        {
            float boardWidth = boardArea.rect.width - Spacing;
            float totalSpacing = Spacing * (boardSize - 1);
            float cellSize = (boardWidth - totalSpacing) / boardSize;

            gridLayout.cellSize = new Vector2(cellSize, cellSize);
        }

        private void CreateCells()
        {
            cells = new BoardCellView[boardSize, boardSize];

            for (int row = 0; row < boardSize; row++)
            {
                for (int col = 0; col < boardSize; col++)
                {
                    BoardCellView cell = Instantiate(cellPrefab, gridLayout.transform);
                    cell.name = $"Cell_{row}_{col}";
                    cell.Init(row, col, OnCellClickedInternal);
                    cells[row, col] = cell;
                }
            }
        }

        private void ClearOldCells()
        {
            for (int i = gridLayout.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(gridLayout.transform.GetChild(i).gameObject);
            }
        }

        private void OnCellClickedInternal(int row, int col)
        {
            CellClicked?.Invoke(row, col);
        }
    }
}