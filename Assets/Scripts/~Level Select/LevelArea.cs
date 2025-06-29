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

    [ValidateInput("ValidateGroupName", "Duplicate GroupName detected! Another LevelArea already uses this name.", InfoMessageType.Warning)]
    public string GroupName;

#if UNITY_EDITOR
    private bool ValidateGroupName(string groupName) {
        if (string.IsNullOrEmpty(groupName)) return true;

        var allLevelAreas = FindObjectsByType<LevelArea>(FindObjectsSortMode.None);
        int count = 0;

        foreach (var area in allLevelAreas) {
            if (area != this && !string.IsNullOrEmpty(area.GroupName) &&
                area.GroupName.Equals(groupName, StringComparison.OrdinalIgnoreCase)) {
                count++;
            }
        }

        return count == 0;
    }
#endif


    [SerializeField][FolderPath] private string levelsPath;

    [SerializeField] private Vector2Int layoutXY = new(4, 5);

    [SerializeField] private LevelButton buttonFab;

    [SerializeField] private List<LevelDataSO> levels = new();

    public ParticleSystem clickParticlePrefab;
    public SoundID LevelSelectedSFX;

    [ReadOnly][SerializeField] private List<GameObject> LevelButtons = new();

    [BoxGroup("Branch Visualization")]
    [SerializeField]
    [Tooltip("A prefab with a RectTransform and an Image (pointing right by default) to represent a branch connection.")]
    private GameObject branchArrowPrefab;

    [BoxGroup("Branch Visualization")]
    [ReadOnly]
    [SerializeField]
    [Tooltip("A container object that will be created to hold the generated branch arrows.")]
    private Transform branchArrowContainer;
    public Transform BranchArrowContainer => branchArrowContainer;

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
        var plane = transform.Find("FloorContainer");
        if (plane != null) plane.gameObject.SetActive(false);

        var branches = transform.Find("BranchArrowContainer");
        if (branches != null) branches.gameObject.SetActive(false);

    }

    public void ShowAllButtons() {
        foreach (var button in LevelButtons) button.SetActive(true);


        //! Dirty sol: hides the plane
        var plane = transform.Find("FloorContainer");
        if (plane != null) plane.gameObject.SetActive(true);
        var branches = transform.Find("BranchArrowContainer");
        if (branches != null) branches.gameObject.SetActive(true);
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

        var occupiedPositions = new HashSet<Vector2Int>();
        var buttonPositions = new Dictionary<Vector2Int, LevelButton>();

        // Possible directions: right, down, left, up
        var directions = new Vector2Int[] {
            new Vector2Int(1, 0),   // right
            new Vector2Int(0, -1),  // down
            new Vector2Int(-1, 0),  // left
            new Vector2Int(0, 1)    // up
        };

        var currentPos = Vector2Int.zero; // Start at (0,0)

        for (int i = 0; i < levels.Count; i++) {
            var l = levels[i];
            var newButton = Instantiate(buttonFab, transform);
            var buttonComponent = newButton.GetComponent<LevelButton>();

            buttonComponent.GridPosition = currentPos;
            buttonComponent.Level = l;

            // Position the button in world space
            var worldPos = new Vector2(currentPos.x * 2f, currentPos.y * 2f);
            newButton.transform.position = worldPos;

            // Track this position
            occupiedPositions.Add(currentPos);
            buttonPositions[currentPos] = buttonComponent;
            LevelButtons.Add(newButton.gameObject);

            Debug.Log($"Placed button {i} at {currentPos}");

            // Find next position for the next button (if there is one)
            if (i < levels.Count - 1) {
                var availableDirections = new List<Vector2Int>();

                // Check all directions for available spots
                foreach (var dir in directions) {
                    var nextPos = currentPos + dir;
                    if (!occupiedPositions.Contains(nextPos)) {
                        availableDirections.Add(dir);
                    }
                }

                // Choose a random available direction
                if (availableDirections.Count > 0) {
                    var randomDir = availableDirections[UnityEngine.Random.Range(0, availableDirections.Count)];
                    currentPos += randomDir;
                }
                else {
                    // If no adjacent spots available
                    currentPos = FindNearestFreePosition(occupiedPositions);
                }
            }
        }

        // Convert dictionary to matrix for compatibility
        buttonMatrix = new LevelButton[1, 1];


        EditorUtility.SetDirty(this);
    }

    private Vector2Int FindNearestFreePosition(HashSet<Vector2Int> occupiedPositions) {
        for (int radius = 1; radius < 100; radius++) {
            foreach (var occupied in occupiedPositions) {
                for (int x = -radius; x <= radius; x++) {
                    for (int y = -radius; y <= radius; y++) {
                        if (Mathf.Abs(x) == radius || Mathf.Abs(y) == radius) {
                            var candidate = occupied + new Vector2Int(x, y);
                            if (!occupiedPositions.Contains(candidate)) {
                                return candidate;
                            }
                        }
                    }
                }
            }
        }

        return new Vector2Int(100, 100);
    }

    private void CreateArrow(LevelButton source, LevelButton target) {
        const float arrowOffset = 1.2f;

        Vector3 sourcePos = source.transform.position;
        Vector3 targetPos = target.transform.position;

        Vector3 delta = targetPos - sourcePos;

        Vector3 chosenDirection;
        float angle;

        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y)) {
            if (delta.x > 0) {
                chosenDirection = Vector3.right;
                angle = 0;
            }
            else {
                chosenDirection = Vector3.left;
                angle = 180;
            }
        }
        else {
            if (delta.y > 0) {
                chosenDirection = Vector3.up;
                angle = 90;
            }
            else {
                chosenDirection = Vector3.down;
                angle = -90;
            }
        }

        GameObject arrowInstance = Instantiate(branchArrowPrefab, branchArrowContainer);

        arrowInstance.GetComponent<Canvas>().worldCamera = Camera.main;
        string sourceBase = source.Level.name.Split('_')[0];
        string targetBase = target.Level.name.Split('_')[0];
        arrowInstance.name = $"Arrow_from_{sourceBase}_{targetBase}";

        arrowInstance.transform.position = sourcePos + (chosenDirection * arrowOffset);
        arrowInstance.transform.rotation = Quaternion.Euler(0, 0, angle - 90); // Offset by 90

    }

    [BoxGroup("Branch Visualization")]
    [Button("Draw Branch Connections")]
    public void DrawBranchConnections() {
#if !UNITY_EDITOR
    Debug.LogWarning("This function is intended for Editor use only.");
    return;
#endif

        if (branchArrowPrefab == null) {
            Debug.LogError("Branch Arrow Prefab is not assigned!", this);
            return;
        }

        // Clean up old arrows
        if (branchArrowContainer != null) {
            DestroyImmediate(branchArrowContainer.gameObject);
        }

        // Create a new container
        branchArrowContainer = new GameObject("BranchArrowContainer").transform;
        branchArrowContainer.SetParent(this.transform);

        var allLevelAreas = FindObjectsByType<LevelArea>(FindObjectsSortMode.None);
        if (allLevelAreas.Length == 0) {
            Debug.LogWarning("No LevelAreas found in the scene.");
            return;
        }

        foreach (var buttonGO in LevelButtons) {
            var sourceButton = buttonGO.GetComponent<LevelButton>();
            if (sourceButton == null || sourceButton.Level == null || !sourceButton.Level.HasBranch) {
                continue;
            }

            var branch = sourceButton.Level.Branch;

            LevelArea targetArea = allLevelAreas.FirstOrDefault(area =>
                !string.IsNullOrEmpty(area.GroupName) &&
                area.GroupName.Equals(branch.TargetGroupName, StringComparison.OrdinalIgnoreCase));

            if (targetArea == null) {
                Debug.LogWarning($"Could not find target LevelArea with GroupName '{branch.TargetGroupName}' for branch from level '{sourceButton.Level.name}'.", sourceButton);
                continue;
            }

            LevelButton targetButton = targetArea.GetButtonByLevelName(branch.TargetLevelName);

            if (targetButton == null) {
                targetButton = targetArea.GetButtonByLevelName(branch.TargetLevelName.Split('_')[0]);
                if (targetButton == null) {
                    Debug.LogWarning($"Could not find target LevelButton with name '{branch.TargetLevelName}' in Area '{targetArea.GroupName}'.", sourceButton);
                    continue;
                }
            }

            // Debug.Log($"Drawing branch from {sourceButton.Level.name} to {targetButton.Level.name}");
            CreateArrow(sourceButton, targetButton);
            CreateArrow(targetButton, sourceButton);
        }

        branchArrowContainer.transform.position += new Vector3(0, 0, 0.5f);

        EditorUtility.SetDirty(this);
    }

    /// <summary>
    /// Finds a LevelButton component in this LevelArea by the name of its associated LevelDataSO.
    /// </summary>
    /// <param name="levelName">The exact name of the LevelDataSO asset.</param>
    /// <returns>The LevelButton component or null if not found.</returns>
    public LevelButton GetButtonByLevelName(string levelName) {
        foreach (var buttonGO in LevelButtons) {
            var button = buttonGO.GetComponent<LevelButton>();
            if (button != null && button.Level != null && button.Level.name.Equals(levelName, StringComparison.OrdinalIgnoreCase)) {
                return button;
            }
        }
        return null;
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


    public void SetCellAsCompleted(Vector2Int cell) {
        if (cell.x < 0 || cell.x >= layoutXY.x || cell.y < 0 || cell.y >= layoutXY.y) {
            Debug.LogWarning("Cell out of bounds: " + cell);
            return;
        }

        if (buttonMatrix == null) {
            Debug.LogWarning("Button matrix not initialized.");
            return;
        }

        var button = buttonMatrix[cell.x, cell.y];
        if (button != null) button.IsCompleted = true;
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

    public void UpdateVisuals() {
        foreach (var button in LevelButtons) {
            var buttonComponent = button.GetComponent<LevelButton>();
            if (buttonComponent != null) {
                buttonComponent.UpdateVisuals();
            }
        }
    }

    public Vector2Int? GetGridPosition(LevelDataSO level) {
        foreach (var button in LevelButtons) {
            var buttonComponent = button.GetComponent<LevelButton>();

            if (buttonComponent.Level == level) return buttonComponent.GridPosition;
        }

        return null; // Not found
    }

    public Vector2Int GetGridPositionOfLevel(string levelNumber) {
        // Debug.Log("GetGridPositionOfLevel: " + levelNumber);

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
        GetComponent<LevelSelectButtonFloor>().GenerateFloor();
        DrawBranchConnections();

        RebuildMatrixFromScene();

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    [Button("Rebuild Matrix From Scene")]
    public void RebuildMatrixFromScene() {
        var buttonParent = transform;
        var foundButtons = buttonParent.GetComponentsInChildren<LevelButton>();

        if (foundButtons.Length == 0) {
            buttonMatrix = new LevelButton[0, 0];
            LevelButtons.Clear();
            layoutXY = Vector2Int.zero;
            Debug.Log("No buttons found to build matrix from.");
            return;
        }

        LevelButtons = foundButtons.Select(b => b.gameObject).ToList();

        Vector2 gridWorldOrigin = new Vector2(float.MaxValue, float.MinValue);
        foreach (var btn in foundButtons) {
            var pos = btn.transform.position;
            if (pos.x < gridWorldOrigin.x) gridWorldOrigin.x = pos.x; // Find the minimum X (leftmost)
            if (pos.y > gridWorldOrigin.y) gridWorldOrigin.y = pos.y; // Find the maximum Y (topmost)
        }

        float cellSize = 2f;
        Dictionary<Vector2Int, LevelButton> buttonDict = new();
        Vector2Int maxSize = Vector2Int.zero;

        foreach (var btn in foundButtons) {
            var pos = btn.transform.position;

            int x = Mathf.RoundToInt((pos.x - gridWorldOrigin.x) / cellSize);
            int y = Mathf.RoundToInt((gridWorldOrigin.y - pos.y) / cellSize); // Y is inverted (world Y is up, grid Y is down)

            Vector2Int gridPos = new(x, y);
            btn.GridPosition = gridPos;
            buttonDict[gridPos] = btn;

            maxSize.x = Mathf.Max(maxSize.x, x + 1);
            maxSize.y = Mathf.Max(maxSize.y, y + 1);
        }

        // Create the matrix with the correct dimensions and populate it
        buttonMatrix = new LevelButton[maxSize.x, maxSize.y];
        foreach (var kvp in buttonDict) {
            if (kvp.Key.x < maxSize.x && kvp.Key.y < maxSize.y) {
                buttonMatrix[kvp.Key.x, kvp.Key.y] = kvp.Value;
            }
        }

        layoutXY = maxSize;
        // Debug.Log($"Matrix rebuilt with size {maxSize}. Found {foundButtons.Length} buttons.");

        DrawBranchConnections();


#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }


}