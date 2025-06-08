using System;
using System.Collections.Generic;
using System.Linq;
using Ami.BroAudio;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

public class LevelArea : MonoBehaviour {
    private const int baseLevelIdx = 2;
    private const int levelSelectIdx = 1;

    public string GroupName;


    [SerializeField][FolderPath] private string levelsPath;

    [SerializeField] private Vector2Int layoutXY = new(4, 5);

    [SerializeField] private LevelButton buttonFab;

    [SerializeField] private List<LevelDataSO> levels = new();

    public ParticleSystem clickParticlePrefab;
    public SoundID LevelSelectedSFX;

    [ReadOnly][SerializeField] private List<GameObject> LevelButtons = new();


    public LevelButton[,] buttonMatrix;
    public List<LevelDataSO> Levels => levels;

    public LevelDataSO ChosenLevel { get; set; }


    private void Start() {
#if UNITY_EDITOR
        if (!Application.isPlaying) return;
#endif
        RebuildMatrixFromScene(); // Rebuild matrix and buttons at runtime
    }


    private void OnDrawGizmos() {
        if (LevelButtons == null || LevelButtons.Count == 0)
            return;

        foreach (var buttonObj in LevelButtons) {
            if (buttonObj == null) continue;

            var button = buttonObj.GetComponent<LevelButton>();
            if (button == null) continue;

            var pos = button.transform.position;

            Gizmos.color = button.IsUnlocked ? Color.green : Color.red;
            Gizmos.DrawWireCube(pos, Vector3.one * 1.8f);
        }
    }

    public void HideAllButtons() {
        foreach (var button in LevelButtons) button.SetActive(false);

        //! Dirty sol: hides the plane
        var plane = transform.Find("Plane");
        if (plane != null) plane.gameObject.SetActive(false);
    }

    public void ShowAllButtons() {
        foreach (var button in LevelButtons) button.SetActive(true);


        //! Dirty sol: hides the plane
        var plane = transform.Find("Plane");
        if (plane != null) plane.gameObject.SetActive(true);
    }

    [BoxGroup("Buttons")]
    // [Button]
    private void LoadLevelsFromPath() {
        if (levelsPath == null || levelsPath.Length == 0) {
            Debug.LogWarning("No levels path");
            return;
        }

        levels.Clear();

        var guids = AssetDatabase.FindAssets("t:LevelDataSO", new[] { levelsPath });

        foreach (var guid in guids) {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var level = AssetDatabase.LoadAssetAtPath<LevelDataSO>(path);
            if (level != null) levels.Add(level);
        }
    }

    [BoxGroup("Buttons")]
    [Button]
    private void PopulateSceneLevelButtons() {
        LoadLevelsFromPath();

        if (levels == null || levels.Count == 0) {
            Debug.LogWarning("No levels");
            return;
        }

        foreach (var b in LevelButtons)
            DestroyImmediate(b);
        LevelButtons.Clear();

        buttonMatrix = new LevelButton[layoutXY.x, layoutXY.y]; // Initialize matrix

        var topLeft = new Vector2(-layoutXY.x + 1, layoutXY.y - 1);

        var y = 0;
        var x = 0;
        foreach (var l in levels) {
            var newButton = Instantiate(buttonFab, transform);
            // var newButton = GameObject.Instantiate(buttonFab, GameObject.Find("ButtonBlocks")?.transform);
            var buttonComponent = newButton.GetComponent<LevelButton>();

            buttonComponent.GridPosition = new Vector2Int(x, y);
            // buttonComponent.IsUnlocked = x == 0 && y == 0; //! Unlock first level only

            var pos = topLeft + (Vector2.right * x + Vector2.down * y) * 2f;
            newButton.transform.position = pos;

            buttonComponent.Level = l;

            buttonMatrix[x, y] = buttonComponent;
            Debug.Log(buttonMatrix[x, y].GridPosition);

            LevelButtons.Add(newButton.gameObject);

            x++;
            if (x >= layoutXY.x)
                y++;
            x = x % layoutXY.x;
        }

        EditorUtility.SetDirty(this);
    }


    //! Eg: If you're on A2b, and there's no A2c, try A3
    private LevelDataSO FindFirstLevelInNextGroup(string currentGroup) {
        if (string.IsNullOrEmpty(currentGroup) || currentGroup.Length != 1)
            return null;

        var nextGroup = (char)(currentGroup[0] + 1);

        for (var i = 1; i <= 10; i++) {
            var levelName = $"{nextGroup}{i}";
            var level = levels.FirstOrDefault(l => l.name.Equals(levelName, StringComparison.OrdinalIgnoreCase));
            if (level != null)
                return level;
        }

        return null;
    }



    public void UnlockCell(Vector2Int cell) {
        if (cell.x < 0 || cell.x >= layoutXY.x || cell.y < 0 || cell.y >= layoutXY.y) {
            Debug.LogWarning("Cell out of bounds: " + cell);
            return;
        }

        if (buttonMatrix == null) {
            Debug.LogWarning("Button matrix not initialized.");
            return;
        }

        if (cell.x < 0 || cell.x >= layoutXY.x || cell.y < 0 || cell.y >= layoutXY.y) {
            Debug.LogWarning("Cell out of bounds: " + cell);
            return;
        }

        var button = buttonMatrix[cell.x, cell.y];
        if (button != null) button.IsUnlocked = true;
    }


    [Button]
    public void UnlockAdjacentCells(Vector2Int cell) {
        if (buttonMatrix == null) {
            Debug.LogWarning("Button matrix not initialized.");
            return;
        }

        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        foreach (var dir in directions) {
            var neighbor = cell + dir;

            if (neighbor.x >= 0 && neighbor.x < layoutXY.x &&
                neighbor.y >= 0 && neighbor.y < layoutXY.y) {
                var neighborButton = buttonMatrix[neighbor.x, neighbor.y];
                if (neighborButton != null && !neighborButton.IsUnlocked) neighborButton.IsUnlocked = true;
            }
        }

        // Debug.Log("UnlockAdjacentCells: " + cell);
    }

    public Vector2Int? GetGridPosition(LevelDataSO level) {
        foreach (var button in LevelButtons) {
            var buttonComponent = button.GetComponent<LevelButton>();

            if (buttonComponent.Level == level) return buttonComponent.GridPosition;
        }

        return null; // Not found
    }

    public Vector2Int GetGridPositionOfLevel(string levelNumber) {
        Debug.Log("GetGridPositionOfLevel: " + levelNumber);

        foreach (var level in levels) {
            if (level.name.StartsWith(levelNumber)) {
                var pos = GetGridPosition(level);
                if (pos.HasValue) return pos.Value;
            }
        }

        return new Vector2Int(-1, -1); // Not found
    }

    [BoxGroup("Buttons")]
    [Button("Snap Buttons to Grid")]
    public void SnapButtonsToWorldGrid() {
        foreach (var buttonObj in LevelButtons)
            if (buttonObj != null) {
                var pos = buttonObj.transform.position;
                pos.x = Mathf.Round(pos.x / 2f) * 2f;
                pos.y = Mathf.Round(pos.y / 2f) * 2f;
                pos.z = Mathf.Round(pos.z / 2f) * 2f;
                buttonObj.transform.position = pos;
            }

        RebuildMatrixFromScene();

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    [Button("Rebuild Matrix From Scene")]
    public void RebuildMatrixFromScene() {
        var buttonParent = transform;
        if (buttonParent == null) {
            Debug.LogWarning("ButtonBlocks object not found.");
            return;
        }

        var foundButtons = buttonParent.GetComponentsInChildren<LevelButton>();
        // Debug.Log("Found " + foundButtons.Length + " buttons.");
        LevelButtons.Clear();

        Dictionary<Vector2Int, LevelButton> buttonDict = new();
        var maxSize = Vector2Int.zero;

        var topLeft = new Vector2(-layoutXY.x + 1, layoutXY.y - 1);
        var cellSize = 2f;

        var firstButtonOrigin = foundButtons[0].transform.position;

        for (var i = 0; i < foundButtons.Length; i++) {
            var btn = foundButtons[i];
            var pos = btn.transform.position;
            var localOffset = new Vector2(pos.x - firstButtonOrigin.x, pos.y - firstButtonOrigin.y);

            var x = Mathf.RoundToInt(localOffset.x / cellSize);
            var y = Mathf.RoundToInt(-localOffset.y / cellSize); // Downward Y

            Vector2Int gridPos = new(x, y);
            btn.GridPosition = gridPos;
            buttonDict[gridPos] = btn;
            LevelButtons.Add(btn.gameObject);

            // if (i == 0) btn.IsUnlocked = true; //! Akways unlock first button

            maxSize.x = Mathf.Max(maxSize.x, x + 1);
            maxSize.y = Mathf.Max(maxSize.y, y + 1);
        }

        buttonMatrix = new LevelButton[maxSize.x, maxSize.y];

        foreach (var kvp in buttonDict) buttonMatrix[kvp.Key.x, kvp.Key.y] = kvp.Value;

        layoutXY = maxSize; // Optional: store new size

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }
}