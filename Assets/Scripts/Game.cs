using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Game : MonoBehaviour {

    [Header("Default Setting")]
    public int width = 16;
    public int height = 16;
    public int mineCount = 32;
    public TextMeshProUGUI mineCountText;

    private Board board;
    private Cell[,] state;

    [Header("Game Over Tip")]
    public TextMeshProUGUI gameOverText;
    private bool gameover;// ��Ϸ�Ƿ����
    private bool isWin;// �Ƿ�ʤ�������ڿ���Tip��ʾ�ı���

    [Header("New Setting")]
    public GameObject newSettingInput;
    public TMP_InputField newWidthCount;
    public TMP_InputField newHeightCount;
    public TMP_InputField newMineCount;
    private bool control = true;

    private float defaultWidth = 16f;
    private float defaultHeight = 16f;
    private float defaultSize = 10f;
    private Tilemap tilemap;
    private int maxWidth = 25;
    private int maxHeight = 25;

    private float clickTime = 0;
    private Vector3Int clickPosition = Vector3Int.zero;

    // �ڱ༭���ϸ���ĳЩֵʱ�Զ����ã������ڼ��ֵ�Ƿ���Ч��
    private void OnValidate() {
        mineCount = Mathf.Clamp(mineCount, 0, width * height);
    }

    private void Awake() {
        board = GetComponentInChildren<Board>();
        tilemap = GetComponentInChildren<Tilemap>();
    }

    // ����Ϸ���������ϵĵ�һ֡����
    private void Start() {
        clickTime = Time.time;// 0
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
        //float mainCameraSize = Camera.main.orthographicSize;
        //Debug.Log(defaultSize + "@@@" + width + "@@@" + defaultWidth);
        Camera.main.orthographicSize = 
            Mathf.Max(width / defaultWidth * defaultSize, height / defaultHeight * defaultSize);
        //Debug.Log(Camera.main.orthographicSize);
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
            newSettingInput.gameObject.SetActive(true);
            control = false;
        }
        if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return)) {
            //Debug.Log(newMineCount.text);
            newSettingInput.gameObject.SetActive(false);
            control = true;
            if (IsChangeSetting()) {
                tilemap.ClearAllTiles();
                NewGame();
            }
        }
        if (Input.GetKeyDown(KeyCode.Escape)) {
            newSettingInput.gameObject.SetActive(false);
            control = true;
        }

        if (control && !gameover) {
            // ���0 �Ҽ�1
            if (Input.GetMouseButtonDown(1)) {
                Flag();
            } else if (Input.GetMouseButtonDown(0)) {
                //Debug.Log(clickTime + "@@@" + Time.time);
                if (Time.time - clickTime <= .2f 
                    && IsSamePosition(clickPosition, MousePositionToCellPosition())
                    && CheckCell(clickPosition.x, clickPosition.y)) {
                    Debug.Log("double click the same revealed cell");

                    QuickReveal(clickPosition.x, clickPosition.y);
                }
                clickTime = Time.time;
                clickPosition = MousePositionToCellPosition();

                Reveal(clickPosition.x, clickPosition.y);
            }
        }
    }

    private void QuickReveal(int cellX, int cellY) {
        for (int adjacentX = -1; adjacentX <= 1; adjacentX++) {
            for (int adjacentY = -1; adjacentY <= 1; adjacentY++) {
                int x = cellX + adjacentX;
                int y = cellY + adjacentY;

                if (adjacentX == 0 && adjacentY == 0 || GetCell(x, y).flagged) {
                    continue;
                }

                // �ƿ�x y�ĸ���
                Reveal(x, y);
            }
        }
    }

    private bool CheckCell(int cellX, int cellY) {
        Cell cell = GetCell(cellX, cellY);
        if (!cell.revealed || cell.number != CountFlaggedMines(cellX, cellY)) {  
            return false;
        }
        return true;
    }

    private int CountFlaggedMines(int cellX, int cellY) {
        int count = 0;

        for (int adjacentX = -1; adjacentX <= 1; adjacentX++) {
            for (int adjacentY = -1; adjacentY <= 1; adjacentY++) {
                if (adjacentX == 0 && adjacentY == 0) {
                    continue;
                }

                int x = cellX + adjacentX;
                int y = cellY + adjacentY;

                if (GetCell(x, y).flagged) {
                    count++;
                }
            }
        }

        return count;
    }

    private bool IsSamePosition(Vector3Int position1, Vector3Int position2) {
        return position1.x == position2.x && position1.y == position2.y;
    }

    private Vector3Int MousePositionToCellPosition() {
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPosition = board.tilemap.WorldToCell(worldPosition);// * V3Int
        return cellPosition;
    }

    /// <summary>
    /// ��ȡ�û�����������²���
    /// </summary>
    /// <returns>�Ƿ�������</returns>
    private bool IsChangeSetting() {
        bool flag = false;
        if (newMineCount.text != "") {
            mineCount = Mathf.Clamp(int.Parse(newMineCount.text), 1, width * height);
            newMineCount.text = "";
            flag = true;
        }
        if (newHeightCount.text != "" && newWidthCount.text != "") {
            height = Mathf.Clamp(int.Parse(newHeightCount.text), 2, maxHeight);
            width = Mathf.Clamp(int.Parse(newWidthCount.text), 2, maxWidth);
            newHeightCount.text = "";
            newWidthCount.text = "";
            flag = true;
        }
        return flag;
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

    private void Reveal(int cellX, int cellY) {
        Cell cell = GetCell(cellX, cellY);

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
                state[cellX, cellY] = cell;
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
