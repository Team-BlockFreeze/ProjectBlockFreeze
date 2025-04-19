using UnityEngine;
using Sirenix.OdinInspector;
using UnityEditor;
using System.Collections.Generic;
using Systems.SceneManagement;
using Ami.BroAudio;
using System;
using UnityEngine.SceneManagement;
using System.Linq;

public class LevelSelector : PersistentSingleton<LevelSelector> {
    [SerializeField]
    [Sirenix.OdinInspector.FolderPath]
    private string levelsPath;

    [SerializeField]
    private Vector2Int layoutXY = new Vector2Int(4, 5);

    [SerializeField]
    private LevelSelectButton buttonFab;

    [SerializeField]
    private List<LevelDataSO> levels = new List<LevelDataSO>();

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
    }

    public void ShowAllButtons() {
        foreach (var button in LevelButtons) {
            button.SetActive(true);
        }
    }

    protected override void Awake() {
        base.Awake();
    }

    private void Start() {
#if UNITY_EDITOR
        if (!Application.isPlaying) return;
#endif
        RebuildMatrixFromScene(); // Rebuild matrix and buttons at runtime
    }

    private void OnEnable() {
        BlockKey.Event_LevelComplete.AddListener(OnLevelComplete);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable() {
        BlockKey.Event_LevelComplete.RemoveListener(OnLevelComplete);
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        if (scene.name == "Level Select Blocks") {
            ShowAllButtons();
        }
        else {
            Debug.Log("Current Level: " + ChosenLevel.name);
        }
    }

    private void OnLevelComplete(LevelDataSO levelData) {
        foreach (var buttonObj in LevelButtons) {
            if (buttonObj == null) continue;

            var button = buttonObj.GetComponent<LevelSelectButton>();
            if (button == null) continue;

            if (button.Level == levelData) {
                UnlockAdjacentCells(button.GridPosition);
                break;
            }
        }
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

    private LevelDataSO GetNextLevel(LevelDataSO currentLevel) {
        string currentName = currentLevel.name;

        // Parse: Group (letters), Number (digits), Bonus (optional letter)
        string groupPart = new string(currentName.TakeWhile(char.IsLetter).ToArray());
        string remainder = currentName.Substring(groupPart.Length);
        string numberPart = new string(remainder.TakeWhile(char.IsDigit).ToArray());
        string bonusPart = new string(remainder.SkipWhile(char.IsDigit).ToArray());

        // Debug.Log($"Group: {groupPart}, Number: {numberPart}, Bonus: {bonusPart}");

        if (!int.TryParse(numberPart, out int levelNum))
            return null;

        // If it's a bonus level like A2a, try A2b
        if (!string.IsNullOrEmpty(bonusPart)) {
            char nextBonus = (char)(bonusPart[0] + 1);
            string nextBonusName = $"{groupPart}{levelNum}{nextBonus}";
            var nextBonusLevel = levels.FirstOrDefault(l => l.name.Equals(nextBonusName, StringComparison.OrdinalIgnoreCase));
            if (nextBonusLevel != null) return nextBonusLevel;

            // Fallback to next base: A3
            string nextBaseName = $"{groupPart}{levelNum + 1}";
            var nextBase = levels.FirstOrDefault(l => l.name.Equals(nextBaseName, StringComparison.OrdinalIgnoreCase));
            if (nextBase != null) return nextBase;

            // Fallback to next group: B1
            return FindFirstLevelInNextGroup(groupPart);
        }

        // If it's a base level like A2, check for A2a, A2b...
        for (char c = 'a'; c <= 'z'; c++) {
            string bonusName = $"{groupPart}{levelNum}{c}";
            var bonus = levels.FirstOrDefault(l => l.name.Equals(bonusName, StringComparison.OrdinalIgnoreCase));
            if (bonus != null) return bonus;
        }

        // Then try A3
        string nextBaseLevelName = $"{groupPart}{levelNum + 1}";
        var nextBaseLevel = levels.FirstOrDefault(l => l.name.Equals(nextBaseLevelName, StringComparison.OrdinalIgnoreCase));
        if (nextBaseLevel != null) return nextBaseLevel;

        // Fallback to next group
        return FindFirstLevelInNextGroup(groupPart);
    }


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



    public void LoadNextLevel() {
        LevelDataSO next = GetNextLevel(ChosenLevel);
        // Debug.Log("next level is " + next);
        if (next != null) {
            ChosenLevel = next;
            SceneLoader.instance.LoadSceneGroup(baseLevelIdx, 0);
        }
        else {
            SceneLoader.instance.LoadSceneGroup(levelSelectIdx, 0); // Fallback
        }
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



    [BoxGroup("Buttons")]
    [Button("Snap Buttons to Grid")]
    private void SnapButtonsToWorldGrid() {
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
    private void RebuildMatrixFromScene() {
        // Transform buttonParent = GameObject.Find("ButtonBlocks")?.transform;
        Transform buttonParent = transform;
        if (buttonParent == null) {
            Debug.LogWarning("ButtonBlocks object not found.");
            return;
        }

        LevelSelectButton[] foundButtons = buttonParent.GetComponentsInChildren<LevelSelectButton>();
        buttonMatrix = new LevelSelectButton[layoutXY.x, layoutXY.y];
        LevelButtons.Clear();

        Vector2 topLeft = new Vector2(-layoutXY.x + 1, layoutXY.y - 1);
        float cellSize = 2f;

        foreach (var btn in foundButtons) {
            Vector3 pos = btn.transform.position;
            Vector2 localOffset = new Vector2(pos.x, pos.y) - topLeft;

            int x = Mathf.RoundToInt(localOffset.x / cellSize);
            int y = Mathf.RoundToInt(-localOffset.y / cellSize);

            if (x >= 0 && x < layoutXY.x && y >= 0 && y < layoutXY.y) {
                btn.GridPosition = new Vector2Int(x, y);
                buttonMatrix[x, y] = btn;
                LevelButtons.Add(btn.gameObject);
            }
            else {
                Debug.LogWarning($"Button at {pos} is outside bounds ({x}, {y})");
            }
        }

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }




    private void OnDrawGizmos() {
        if (LevelButtons == null || LevelButtons.Count == 0)
            return;



        Vector2 topLeft = new Vector2(-layoutXY.x + 1, layoutXY.y - 1);
        float cellSize = 2f;

        foreach (var buttonObj in LevelButtons) {
            if (buttonObj == null) continue;

            var button = buttonObj.GetComponent<LevelSelectButton>();
            if (button == null) continue;

            Vector2Int gridPos = button.GridPosition;
            Vector3 pos = topLeft + (Vector2.right * gridPos.x + Vector2.down * gridPos.y) * cellSize;

            Gizmos.color = button.IsUnlocked ? Color.green : Color.red;
            Gizmos.DrawWireCube(pos, Vector3.one * 1.8f);
        }
    }


}
