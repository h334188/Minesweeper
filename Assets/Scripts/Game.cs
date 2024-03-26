using TMPro;
using UnityEngine;

public class Game : MonoBehaviour {

    public int width = 16;
    public int height = 16;
    public int mineCount = 32;

    private Board board;
    private Cell[,] state;

    private bool gameover;

    private bool isWin;
    public TextMeshProUGUI gameOverText;

    public TextMeshProUGUI mineCountText;
    public TMP_InputField newMineCount;
    private bool control = true;

    // 在编辑器上更新某些值时自动调用（可用于检测值是否有效）
    private void OnValidate() {
        mineCount = Mathf.Clamp(mineCount, 0, width * height);
    }

    private void Awake() {
        board = GetComponentInChildren<Board>();
    }

    // 在游戏对象运行上的第一帧调用
    private void Start() {
        NewGame();
    }

    public void NewGame() {
        state = new Cell[width, height];
        gameover = false;

        mineCountText.text = mineCount.ToString();

        GenerateCells();
        GenerateMines();
        GenerateNumbers();

        Camera.main.transform.position = new Vector3(width / 2f, height / 2f, -10);
        board.Draw(state);
    }

    /// <summary>
    /// 生成cell
    /// </summary>
    private void GenerateCells() {
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                Cell cell = new Cell();
                cell.position = new Vector3Int(x, y, 0);
                cell.type = Cell.Type.Empty;
                state[x, y] = cell;
            }
        }
    }

    private void GenerateMines() {
        for (int i = 0;i < mineCount; i++) {
            int x = Random.Range(0, width);
            int y = Random.Range(0, height);

            while (state[x, y].type == Cell.Type.Mine) {
                x++;

                if (x >= width) {
                    x = 0;
                    y++;

                    if (y >= height) {
                        y = 0;
                    }
                }
            }

            state[x, y].type = Cell.Type.Mine;
            //state[x, y].revealed = true;// 显示一下看看
        }
    }

    private void GenerateNumbers() {
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                Cell cell = state[x, y];

                if (cell.type == Cell.Type.Mine) {
                    continue;
                }

                cell.number = CountMines(x, y);

                if (cell.number > 0) {
                    cell.type = Cell.Type.Number;
                }

                state[x, y] = cell;
            }
        }
    }

    private int CountMines(int cellX, int cellY) {
        int count = 0;

        for (int adjacentX = -1; adjacentX <= 1; adjacentX++) {
            for (int adjacentY = -1; adjacentY <= 1; adjacentY++) {
                if (adjacentX == 0 && adjacentY == 0) {
                    continue;
                }

                int x = cellX + adjacentX;
                int y = cellY + adjacentY;

                if (GetCell(x, y).type == Cell.Type.Mine) {
                    count++;
                }
            }
        }

        return count;
    }

    // 在游戏或脚本运行中的每一帧自动调用
    private void Update() {
        if (Input.GetKeyDown(KeyCode.R)) {
            NewGame();
        }

        if (Input.GetKeyDown(KeyCode.Space)) {
            newMineCount.gameObject.SetActive(true);
            control = false;
        }
        if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return)) {
            mineCount = Mathf.Clamp(int.Parse(newMineCount.text), 0, width * height);
            newMineCount.gameObject.SetActive(false);
            control = true;
            NewGame();
        }
        if (Input.GetKeyDown(KeyCode.Escape)) {
            newMineCount.gameObject.SetActive(false);
            control = true;
        }

        if (control && !gameover) {
            // 左键0 右键1
            if (Input.GetMouseButtonDown(1)) {
                Flag();
            } else if (Input.GetMouseButtonDown(0)) {
                Reveal();
            }
        }
    }

    private void Flag() {
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPosition = board.tilemap.WorldToCell(worldPosition);// * retrn V3Int
        Cell cell = GetCell(cellPosition.x, cellPosition.y);

        if (cell.type == Cell.Type.Invalid || cell.revealed) {
            return;
        }

        cell.flagged = !cell.flagged;
        state[cellPosition.x, cellPosition.y] = cell;
        board.Draw(state);

        int currentMine = Mathf.Clamp(mineCount - board.flagedNum, 0, width * height);
        mineCountText.text = currentMine.ToString();
    }

    private void Reveal() {
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPosition = board.tilemap.WorldToCell(worldPosition);// * V3Int
        Cell cell = GetCell(cellPosition.x, cellPosition.y);

        if (cell.type == Cell.Type.Invalid || cell.revealed || cell.flagged) {
            return;
        }

        switch (cell.type) {
            case Cell.Type.Mine:
                Explode(cell);
                break;

            case Cell.Type.Empty:
                Flood(cell);
                CheckWinCondition();
                break;

            default:
                cell.revealed = true;
                state[cellPosition.x, cellPosition.y] = cell;
                CheckWinCondition();
                break;
        }

        board.Draw(state);
    }

    // flood 淹没、泛滥、泛洪（一次性大范围掀开empty）
    private void Flood(Cell cell) {
        if (cell.revealed) return;
        if (cell.type == Cell.Type.Mine || cell.type == Cell.Type.Invalid) return;

        // ** 此处如果是number，也会显示，但不会继续flood
        cell.revealed = true;
        state[cell.position.x, cell.position.y] = cell;

        if (cell.type == Cell.Type.Empty) {
            Flood(GetCell(cell.position.x - 1, cell.position.y));
            Flood(GetCell(cell.position.x + 1, cell.position.y));
            Flood(GetCell(cell.position.x, cell.position.y - 1));
            Flood(GetCell(cell.position.x, cell.position.y + 1));
        }
    }

    private void Explode(Cell cell) {// 标记错的如何显示
        //Debug.Log("Game Over!");

        gameover = true;

        cell.revealed = true;
        cell.exploded = true;
        state[cell.position.x, cell.position.y] = cell;

        // 不能直接调用Draw，否则也只是展示单个爆炸的变化，而不是所有的揭露
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                cell = state[x, y];

                // 揭露其他的地雷
                // 如果是地雷，并且无标记，则为引爆状态；否则为自然地雷状态
                if (cell.type == Cell.Type.Mine) {
                    if (!cell.flagged) {
                        cell.exploded = true;
                    }
                    cell.revealed = true;
                    state[x, y] = cell;
                }
            }
        }

        isWin = false;
        Invoke("ShowGameOverText", 1f);
        //ShowGameOverText(false);
    }

    private void CheckWinCondition() {
        for (int x = 0;x < width; x++) {
            for (int y = 0;y < height; y++) {
                Cell cell = state[x, y];

                if (cell.type != Cell.Type.Mine && !cell.revealed) {
                    return;
                }
            }
        }

        //Debug.Log("You win!");
        gameover = true;

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                Cell cell = state[x, y];

                // 地雷自我标记
                if (cell.type == Cell.Type.Mine) {
                    cell.flagged = true;
                    state[x, y] = cell;
                }
            }
        }

        mineCountText.text = "0";
        isWin = true;
        Invoke("ShowGameOverText", 1f);
        //ShowGameOverText(true);
    }

    private void ShowGameOverText() {
        gameOverText.text = isWin ? "You Win!" : "Game Over!";

        // 显示按钮（获取父游戏对象的方法）
        gameOverText.transform.parent.gameObject.SetActive(true);
    }

    private Cell GetCell(int x, int y) {
        if (IsValid(x, y)) {
            return state[x, y];
        } else {
            return new Cell();
        }
    }

    private bool IsValid(int x, int y) {
        return x >= 0 && x < width && y >= 0 && y < height;
    }
}
