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

    // �ڱ༭���ϸ���ĳЩֵʱ�Զ����ã������ڼ��ֵ�Ƿ���Ч��
    private void OnValidate() {
        mineCount = Mathf.Clamp(mineCount, 0, width * height);
    }

    private void Awake() {
        board = GetComponentInChildren<Board>();
    }

    // ����Ϸ���������ϵĵ�һ֡����
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
    /// ����cell
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
            //state[x, y].revealed = true;// ��ʾһ�¿���
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

    // ����Ϸ��ű������е�ÿһ֡�Զ�����
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
            // ���0 �Ҽ�1
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

    // flood ��û�����ġ����飨һ���Դ�Χ�ƿ�empty��
    private void Flood(Cell cell) {
        if (cell.revealed) return;
        if (cell.type == Cell.Type.Mine || cell.type == Cell.Type.Invalid) return;

        // ** �˴������number��Ҳ����ʾ�����������flood
        cell.revealed = true;
        state[cell.position.x, cell.position.y] = cell;

        if (cell.type == Cell.Type.Empty) {
            Flood(GetCell(cell.position.x - 1, cell.position.y));
            Flood(GetCell(cell.position.x + 1, cell.position.y));
            Flood(GetCell(cell.position.x, cell.position.y - 1));
            Flood(GetCell(cell.position.x, cell.position.y + 1));
        }
    }

    private void Explode(Cell cell) {// ��Ǵ�������ʾ
        //Debug.Log("Game Over!");

        gameover = true;

        cell.revealed = true;
        cell.exploded = true;
        state[cell.position.x, cell.position.y] = cell;

        // ����ֱ�ӵ���Draw������Ҳֻ��չʾ������ը�ı仯�����������еĽ�¶
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                cell = state[x, y];

                // ��¶�����ĵ���
                // ����ǵ��ף������ޱ�ǣ���Ϊ����״̬������Ϊ��Ȼ����״̬
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

                // �������ұ��
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

        // ��ʾ��ť����ȡ����Ϸ����ķ�����
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
