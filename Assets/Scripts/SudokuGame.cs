using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Головний менеджер гри Судоку.
/// Прикріпи цей скрипт до порожнього GameObject "SudokuManager" на сцені.
/// </summary>
public class SudokuGame : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Grid LayoutGroup GameObject (9x9 комірок)")]
    public Transform gridParent;

    [Tooltip("Префаб комірки судоку")]
    public GameObject cellPrefab;

    [Tooltip("Кнопки цифр (1-9) + кнопка стирання")]
    public Button[] numberButtons; // 9 кнопок цифр

    [Tooltip("Кнопка стирання значення")]
    public Button eraseButton;

    [Tooltip("Кнопка нової гри")]
    public Button newGameButton;

    [Tooltip("Кнопка підказки")]
    public Button hintButton;

    [Tooltip("Панель перемоги")]
    public GameObject winPanel;

    [Tooltip("Текст лічильника помилок")]
    public TextMeshProUGUI mistakesText;

    [Tooltip("Текст таймера")]
    public TextMeshProUGUI timerText;

    [Tooltip("Текст підказок")]
    public TextMeshProUGUI hintsText;

    [Header("Visual Settings")]
    public Color defaultCellColor = new Color(0.95f, 0.95f, 0.98f);
    public Color selectedCellColor = new Color(0.67f, 0.85f, 1f);
    public Color highlightedCellColor = new Color(0.85f, 0.92f, 1f);
    public Color givenNumberColor = new Color(0.1f, 0.1f, 0.2f);
    public Color playerNumberColor = new Color(0.2f, 0.4f, 0.9f);
    public Color errorColor = new Color(1f, 0.3f, 0.3f);
    public Color correctHighlightColor = new Color(0.8f, 1f, 0.8f);

    // Внутрішній стан
    private SudokuCell[,] cells = new SudokuCell[9, 9];
    private int[,] solution = new int[9, 9];
    private int[,] puzzle = new int[9, 9];

    private int selectedRow = -1;
    private int selectedCol = -1;
    private int selectedNumber = 0;

    private int mistakes = 0;
    private int hintsLeft = 3;
    private float timer = 0f;
    private bool gameActive = false;

    private const int MAX_MISTAKES = 3;

    void Start()
    {
        InitializeGrid();
        SetupButtons();
        StartNewGame();
    }

    void Update()
    {
        if (gameActive)
        {
            timer += Time.deltaTime;
            UpdateTimerUI();
        }

        HandleKeyboardInput();
    }

    void InitializeGrid()
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                GameObject cellGO = Instantiate(cellPrefab, gridParent);
                cellGO.name = $"Cell_{row}_{col}";

                SudokuCell cell = cellGO.GetComponent<SudokuCell>();
                if (cell == null)
                    cell = cellGO.AddComponent<SudokuCell>();

                int r = row, c = col;
                cell.Init(r, c, this);
                cells[row, col] = cell;
            }
        }
    }

    void SetupButtons()
    {
        for (int i = 0; i < numberButtons.Length && i < 9; i++)
        {
            int num = i + 1;
            numberButtons[i].onClick.AddListener(() => OnNumberSelected(num));
        }

        if (eraseButton) eraseButton.onClick.AddListener(EraseSelected);
        if (newGameButton) newGameButton.onClick.AddListener(StartNewGame);
        if (hintButton) hintButton.onClick.AddListener(UseHint);
    }

    public void StartNewGame()
    {
        mistakes = 0;
        hintsLeft = 3;
        timer = 0f;
        gameActive = true;
        selectedRow = -1;
        selectedCol = -1;
        selectedNumber = 0;

        if (winPanel) winPanel.SetActive(false);

        GenerateSolution();
        CreatePuzzle(35); 
        ApplyPuzzleToGrid();
        UpdateUI();
    }

    void GenerateSolution()
    {
        solution = new int[9, 9];
        SolveSudoku(solution);
    }

    bool SolveSudoku(int[,] board)
    {
        if (IsEmpty(board))
            FillFirstRow(board);

        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if (board[row, col] == 0)
                {
                    List<int> nums = GetShuffledNumbers();
                    foreach (int num in nums)
                    {
                        if (IsValidPlacement(board, row, col, num))
                        {
                            board[row, col] = num;
                            if (SolveSudoku(board)) return true;
                            board[row, col] = 0;
                        }
                    }
                    return false;
                }
            }
        }
        return true;
    }

    bool IsEmpty(int[,] board)
    {
        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
                if (board[r, c] != 0) return false;
        return true;
    }

    void FillFirstRow(int[,] board)
    {
        List<int> nums = GetShuffledNumbers();
        for (int c = 0; c < 9; c++)
            board[0, c] = nums[c];
    }

    List<int> GetShuffledNumbers()
    {
        List<int> nums = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        for (int i = nums.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (nums[i], nums[j]) = (nums[j], nums[i]);
        }
        return nums;
    }

    bool IsValidPlacement(int[,] board, int row, int col, int num)
    {
        for (int c = 0; c < 9; c++)
            if (board[row, c] == num) return false;

        for (int r = 0; r < 9; r++)
            if (board[r, col] == num) return false;

        int boxRow = (row / 3) * 3;
        int boxCol = (col / 3) * 3;
        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
                if (board[boxRow + r, boxCol + c] == num) return false;

        return true;
    }

    void CreatePuzzle(int cellsToRemove)
    {
        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
                puzzle[r, c] = solution[r, c];

        int removed = 0;
        int attempts = 0;
        while (removed < cellsToRemove && attempts < 200)
        {
            int row = Random.Range(0, 9);
            int col = Random.Range(0, 9);
            if (puzzle[row, col] != 0)
            {
                puzzle[row, col] = 0;
                removed++;
            }
            attempts++;
        }
    }

    void ApplyPuzzleToGrid()
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                bool isGiven = puzzle[row, col] != 0;
                cells[row, col].SetValue(puzzle[row, col], isGiven);
                cells[row, col].SetColors(defaultCellColor, isGiven ? givenNumberColor : playerNumberColor);
            }
        }
    }

    public void OnCellSelected(int row, int col)
    {
        if (!gameActive) return;

        selectedRow = row;
        selectedCol = col;

        RefreshCellHighlights();

        if (selectedNumber > 0)
            PlaceNumber(row, col, selectedNumber);
    }

    void RefreshCellHighlights()
    {
        int selVal = (selectedRow >= 0 && selectedCol >= 0)
            ? cells[selectedRow, selectedCol].CurrentValue
            : 0;

        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                SudokuCell cell = cells[r, c];

                bool isSelected = (r == selectedRow && c == selectedCol);
                bool sameRowCol = (r == selectedRow || c == selectedCol);
                bool sameBox = (r / 3 == selectedRow / 3 && c / 3 == selectedCol / 3);
                bool sameNumber = selVal > 0 && cell.CurrentValue == selVal;

                if (isSelected)
                    cell.SetBackground(selectedCellColor);
                else if (sameNumber)
                    cell.SetBackground(correctHighlightColor);
                else if (sameRowCol || sameBox)
                    cell.SetBackground(highlightedCellColor);
                else
                    cell.SetBackground(defaultCellColor);
            }
        }
    }

    void OnNumberSelected(int num)
    {
        selectedNumber = num;
        if (selectedRow >= 0 && selectedCol >= 0)
            PlaceNumber(selectedRow, selectedCol, num);
    }

    void PlaceNumber(int row, int col, int num)
    {
        if (!gameActive) return;

        SudokuCell cell = cells[row, col];
        if (cell.IsGiven) return; 

        if (num == solution[row, col])
        {
            cell.SetValue(num, false);
            cell.SetColors(defaultCellColor, playerNumberColor);
            CheckWin();
        }
        else
        {
            // Помилка
            mistakes++;
            cell.FlashError(errorColor, defaultCellColor);
            UpdateUI();

            if (mistakes >= MAX_MISTAKES)
                GameOver();
        }

        RefreshCellHighlights();
    }

    void EraseSelected()
    {
        if (selectedRow < 0 || selectedCol < 0) return;
        SudokuCell cell = cells[selectedRow, selectedCol];
        if (cell.IsGiven) return;

        cell.SetValue(0, false);
        RefreshCellHighlights();
    }

    void UseHint()
    {
        if (!gameActive || hintsLeft <= 0) return;
        if (selectedRow < 0 || selectedCol < 0) return;

        SudokuCell cell = cells[selectedRow, selectedCol];
        if (cell.IsGiven || cell.CurrentValue == solution[selectedRow, selectedCol]) return;

        hintsLeft--;
        int correctVal = solution[selectedRow, selectedCol];
        cell.SetValue(correctVal, false);
        cell.SetColors(new Color(0.85f, 1f, 0.85f), new Color(0.1f, 0.6f, 0.2f));

        UpdateUI();
        CheckWin();
        RefreshCellHighlights();
    }

    void CheckWin()
    {
        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
                if (cells[r, c].CurrentValue != solution[r, c]) return;

        gameActive = false;
        if (winPanel) winPanel.SetActive(true);
    }

    void GameOver()
    {
        gameActive = false;
        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
                if (!cells[r, c].IsGiven)
                    cells[r, c].SetValue(solution[r, c], false);

        Debug.Log("GAME OVER!");
    }

    void HandleKeyboardInput()
    {
        for (int i = 1; i <= 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i))
            {
                OnNumberSelected(i);
                return;
            }
        }

        if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace))
            EraseSelected();

        if (selectedRow >= 0 && selectedCol >= 0)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow) && selectedRow > 0)
                OnCellSelected(selectedRow - 1, selectedCol);
            if (Input.GetKeyDown(KeyCode.DownArrow) && selectedRow < 8)
                OnCellSelected(selectedRow + 1, selectedCol);
            if (Input.GetKeyDown(KeyCode.LeftArrow) && selectedCol > 0)
                OnCellSelected(selectedRow, selectedCol - 1);
            if (Input.GetKeyDown(KeyCode.RightArrow) && selectedCol < 8)
                OnCellSelected(selectedRow, selectedCol + 1);
        }
    }

    void UpdateUI()
    {
        if (mistakesText) mistakesText.text = $"Помилки: {mistakes}/{MAX_MISTAKES}";
        if (hintsText) hintsText.text = $"Підказки: {hintsLeft}";
    }

    void UpdateTimerUI()
    {
        if (timerText)
        {
            int minutes = (int)(timer / 60);
            int seconds = (int)(timer % 60);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }

    public SudokuCell GetCell(int row, int col) => cells[row, col];
}