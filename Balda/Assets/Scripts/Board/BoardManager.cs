using UnityEngine;
using UnityEngine.UI;

public class BoardManager : MonoBehaviour
{
    [Header("Board Size")]
    [Range(5, 10)]
    [SerializeField] private int boardSize = 5;

    [Header("References")]
    [SerializeField] private RectTransform boardArea; 
    [SerializeField] private GridLayoutGroup gridLayout;
    [SerializeField] private BoardCellView cellPrefab;

    [Header("Layout")]
    [SerializeField] private float spacing = 6f;
    [SerializeField, Range(0.6f, 1f)] private float widthPercent = 0.9f;

    private BoardCellView[,] cells;

    private void Start()
    {
        ApplyBoardWidth();
        BuildBoard();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!Application.isPlaying || boardArea == null || gridLayout == null)
            return;

        ApplyBoardWidth();
        UpdateGridCellSize();
    }

    public void BuildBoard()
    {
        ClearOldCells();
        ApplyBoardWidth();
        ConfigureGrid();
        CreateCells();
    }

    private void ApplyBoardWidth()
    {
        RectTransform parent = boardArea.parent as RectTransform;
        if (parent == null) return;

        float targetWidth = parent.rect.width * widthPercent;
        boardArea.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
        boardArea.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetWidth);
    }

    private void ConfigureGrid()
    {
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = boardSize;
        gridLayout.spacing = new Vector2(spacing, spacing);

        UpdateGridCellSize();
    }

    private void UpdateGridCellSize()
    {
        float boardWidth = boardArea.rect.width - 50;
        float totalSpacing = spacing * (boardSize - 1);
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
                cell.Init(row, col, OnCellClicked);
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

    private void OnCellClicked(BoardCellView cell)
    {
        Debug.Log($"Clicked: [{cell.Row}, {cell.Col}]");
    }

    public void SetBoardSize(int newSize)
    {
        boardSize = Mathf.Clamp(newSize, 5, 10);
        BuildBoard();
    }
}