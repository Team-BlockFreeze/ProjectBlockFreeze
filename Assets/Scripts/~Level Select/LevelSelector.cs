using UnityEngine;
using Sirenix.OdinInspector;
using UnityEditor;
using System.Collections.Generic;
using Systems.SceneManagement;
using Ami.BroAudio;
using System;
using UnityEngine.SceneManagement;
using System.Linq;

public class LevelSelector : MonoBehaviour {

    public string GroupName;



    [SerializeField]
    [Sirenix.OdinInspector.FolderPath]
    private string levelsPath;

    [SerializeField]
    private Vector2Int layoutXY = new Vector2Int(4, 5);

    [SerializeField]
    private LevelSelectButton buttonFab;

    [SerializeField]
    private List<LevelDataSO> levels = new List<LevelDataSO>();
    public List<LevelDataSO> Levels => levels;

    public LevelDataSO ChosenLevel { get; set; }

    public ParticleSystem clickParticlePrefab;
    public SoundID LevelSelectedSFX;

    [ReadOnly]
    [SerializeField]
    private List<GameObject> LevelButtons = new List<GameObject>();

    public void HideAllButtons() {
        foreach (var button in LevelButtons) {
            button.SetActive(false);
        }

        //! Dirty sol: hides the plane
        var plane = transform.Find("Plane");
        if (plane != null) {
            plane.gameObject.SetActive(false);
        }
    }

    public void ShowAllButtons() {
        foreach (var button in LevelButtons) {
            button.SetActive(true);
        }


        //! Dirty sol: hides the plane
        var plane = transform.Find("Plane");
        if (plane != null) {
            plane.gameObject.SetActive(true);
        }

    }


    private void Start() {
#if UNITY_EDITOR
        if (!Application.isPlaying) return;
#endif
        RebuildMatrixFromScene(); // Rebuild matrix and buttons at runtime
    }

    [BoxGroup("Buttons")]
    // [Button]
    private void LoadLevelsFromPath() {
        if (levelsPath == null || levelsPath.Length == 0) {
            Debug.LogWarning("No levels path");
            return;
        }

        levels.Clear();

        string[] guids = AssetDatabase.FindAssets("t:LevelDataSO", new[] { levelsPath });

        foreach (string guid in guids) {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            LevelDataSO level = AssetDatabase.LoadAssetAtPath<LevelDataSO>(path);
            if (level != null) {
                levels.Add(level);
            }
        }
    }


    public LevelSelectButton[,] buttonMatrix;

    [BoxGroup("Buttons")]
    [Button]
    private void PopulateSceneLevelButtons() {
        LoadLevelsFromPath();

        if (levels == null || levels.Count == 0) {
            Debug.LogWarning("No levels");
            return;
        }

        foreach (var b in LevelButtons)
            GameObject.DestroyImmediate(b);
        LevelButtons.Clear();

        buttonMatrix = new LevelSelectButton[layoutXY.x, layoutXY.y]; // Initialize matrix

        Vector2 topLeft = new Vector2(-layoutXY.x + 1, layoutXY.y - 1);

        int y = 0;
        int x = 0;
        foreach (var l in levels) {
            var newButton = GameObject.Instantiate(buttonFab, transform);
            // var newButton = GameObject.Instantiate(buttonFab, GameObject.Find("ButtonBlocks")?.transform);
            var buttonComponent = newButton.GetComponent<LevelSelectButton>();

            buttonComponent.GridPosition = new Vector2Int(x, y);
            buttonComponent.IsUnlocked = (x == 0 && y == 0); //! Unlock first level only

            Vector2 pos = topLeft + (Vector2.right * x + Vector2.down * y) * 2f;
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


    const int baseLevelIdx = 2;
    const int levelSelectIdx = 1;


    //! Eg: If you're on A2b, and there's no A2c, try A3
    private LevelDataSO FindFirstLevelInNextGroup(string currentGroup) {
        if (string.IsNullOrEmpty(currentGroup) || currentGroup.Length != 1)
            return null;

        char nextGroup = (char)(currentGroup[0] + 1);

        for (int i = 1; i <= 10; i++) {
            string levelName = $"{nextGroup}{i}";
            var level = levels.FirstOrDefault(l => l.name.Equals(levelName, StringComparison.OrdinalIgnoreCase));
            if (level != null)
                return level;
        }

        return null;
    }




    [Button]
    public void UnlockAdjacentCells(Vector2Int cell) {
        if (buttonMatrix == null) {
            Debug.LogWarning("Button matrix not initialized.");
            return;
        }

        Vector2Int[] directions = {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

        foreach (var dir in directions) {
            Vector2Int neighbor = cell + dir;

            if (neighbor.x >= 0 && neighbor.x < layoutXY.x &&
                neighbor.y >= 0 && neighbor.y < layoutXY.y) {

                LevelSelectButton neighborButton = buttonMatrix[neighbor.x, neighbor.y];
                if (neighborButton != null && !neighborButton.IsUnlocked) {
                    neighborButton.IsUnlocked = true;
                }
            }
        }

        // Debug.Log("UnlockAdjacentCells: " + cell);
    }

    public Vector2Int? GetGridPosition(LevelDataSO level) {
        foreach (var button in LevelButtons) {
            LevelSelectButton buttonComponent = button.GetComponent<LevelSelectButton>();

            if (buttonComponent.Level == level) {
                return buttonComponent.GridPosition;
            }
        }

        return null; // Not found
    }

    [BoxGroup("Buttons")]
    [Button("Snap Buttons to Grid")]
    public void SnapButtonsToWorldGrid() {
        foreach (var buttonObj in LevelButtons) {
            if (buttonObj != null) {
                Vector3 pos = buttonObj.transform.position;
                pos.x = Mathf.Round(pos.x);
                pos.y = Mathf.Round(pos.y);
                pos.z = Mathf.Round(pos.z);
                buttonObj.transform.position = pos;
            }
        }

        RebuildMatrixFromScene();

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    [Button("Rebuild Matrix From Scene")]
    public void RebuildMatrixFromScene() {
        Transform buttonParent = transform;
        if (buttonParent == null) {
            Debug.LogWarning("ButtonBlocks object not found.");
            return;
        }

        LevelSelectButton[] foundButtons = buttonParent.GetComponentsInChildren<LevelSelectButton>();
        // Debug.Log("Found " + foundButtons.Length + " buttons.");
        LevelButtons.Clear();

        Dictionary<Vector2Int, LevelSelectButton> buttonDict = new();
        Vector2Int maxSize = Vector2Int.zero;

        Vector2 topLeft = new Vector2(-layoutXY.x + 1, layoutXY.y - 1);
        float cellSize = 2f;

        Vector3 firstButtonOrigin = foundButtons[0].transform.position;

        for (int i = 0; i < foundButtons.Length; i++) {
            var btn = foundButtons[i];
            Vector3 pos = btn.transform.position;
            Vector2 localOffset = new Vector2(pos.x - firstButtonOrigin.x, pos.y - firstButtonOrigin.y);

            int x = Mathf.RoundToInt(localOffset.x / cellSize);
            int y = Mathf.RoundToInt(-localOffset.y / cellSize); // Downward Y

            Vector2Int gridPos = new(x, y);
            btn.GridPosition = gridPos;
            buttonDict[gridPos] = btn;
            LevelButtons.Add(btn.gameObject);

            if (i == 0) {
                btn.IsUnlocked = true; //! Akways unlock first button
            }

            maxSize.x = Mathf.Max(maxSize.x, x + 1);
            maxSize.y = Mathf.Max(maxSize.y, y + 1);
        }

        buttonMatrix = new LevelSelectButton[maxSize.x, maxSize.y];

        foreach (var kvp in buttonDict) {
            buttonMatrix[kvp.Key.x, kvp.Key.y] = kvp.Value;
        }

        layoutXY = maxSize; // Optional: store new size

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }






    private void OnDrawGizmos() {
        if (LevelButtons == null || LevelButtons.Count == 0)
            return;

        foreach (var buttonObj in LevelButtons) {
            if (buttonObj == null) continue;

            var button = buttonObj.GetComponent<LevelSelectButton>();
            if (button == null) continue;

            Vector3 pos = button.transform.position;

            Gizmos.color = button.IsUnlocked ? Color.green : Color.red;
            Gizmos.DrawWireCube(pos, Vector3.one * 1.8f);
        }
    }



}
