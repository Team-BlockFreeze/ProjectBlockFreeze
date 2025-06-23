using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Sirenix.OdinInspector;
using Systems.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;


public class LevelAreaController : PersistentSingleton<LevelAreaController> {
    private const int baseLevelIdx = 2;
    private const int levelSelectIdx = 1;


    private const float xBetweenLevelAreas = 20;

    private const float yBetweenLevelAreas = 27.5f;


    /// <summary>
    ///     Example: "A3a_C2"
    ///     - Group: "A"
    ///     - Level Number: 3
    ///     - Bonus Letter: "a"
    ///     - Branch Target: "C2"
    ///     - Follow-up Level (if not branching): would look for A3b, then A4, etc.
    ///     - Since there's a branch ("_C2"), progression halts and logs the transition.
    /// </summary>
    /// <param name="currentLevel"></param>
    [SerializeField] private List<LevelArea> selectors;

    [SerializeField][ReadOnly] private string currentBranch;


    [SerializeField][ReadOnly] private LevelDataSO chosenLevel;

    [SerializeField] private SerializedDictionary<string, LevelArea> selectorMap = new();


    public string CurrentBranch {
        get => currentBranch;
        set => currentBranch = value;
    }

    public LevelDataSO ChosenLevel {
        get => chosenLevel;
        set => chosenLevel = value;
    }


    protected override void Awake() {
        base.Awake();

        selectors = GetComponentsInChildren<LevelArea>(true).ToList(); //! Include inactive is a thing apparently
        selectorMap.Clear();
        foreach (var selector in selectors) selectorMap.Add(selector.GroupName, selector);


#if UNITY_EDITOR
        if (!Application.isPlaying) return;
#endif
        foreach (var selector in selectors) {
            selector.RebuildMatrixFromScene();
            selector.DrawBranchConnections();
        }

    }

    private void Start() {
        currentBranch = "A";
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
        if (scene.name == "Level Select Blocks")
            ShowAllButtons();
        else
            Debug.Log("Current Level: " + ChosenLevel.name);
    }

    private void OnLevelComplete(LevelDataSO completedLevel) {
        Debug.Log($"Level completed: {completedLevel.name}");

        var selector = GetSelectorForLevel(completedLevel);
        // Debug.Log("Selector: " + selector?.name);

        if (selector != null) {
            var gridPos = selector.GetGridPosition(completedLevel);

            if (gridPos.HasValue) {
                selector.UnlockAdjacentCells(gridPos.Value);
                selector.BranchArrowContainer.gameObject.SetActive(true);
            }
        }

        if (!string.IsNullOrEmpty(completedLevel.Branch.TargetGroupName)) {
            var transitions = selector.BranchArrowContainer.GetComponentsInChildren<LevelBranchTransition>(true);
            foreach (var transition in transitions) {
                if (transition.transform.parent.name.Contains(completedLevel.name)) {
                    transition.transform.parent.gameObject.SetActive(true);
                    transition.UnlockBranchTransition();
                }
            }
        }
    }


    public void HideAllButtons() {
        // Debug.Log("HideAllButtons");
        foreach (var selector in selectors) selector.HideAllButtons();
    }

    public void ShowAllButtons() {
        foreach (var selector in selectors) selector.ShowAllButtons();
    }

    public LevelArea GetSelectorForGroup(string groupName) {
        selectorMap.TryGetValue(groupName, out var selector);
        return selector;
    }

    public LevelArea GetSelectorForLevel(LevelDataSO level) {
        var name = level.name;
        var parts = name.Split('_');
        var group = new string(parts[0].TakeWhile(char.IsLetter).ToArray());

        return GetSelectorForGroup(group);
    }

    public LevelDataSO GetNextLevel(LevelDataSO currentLevel) {
        if (currentLevel == null) return null;

        if (currentLevel.HasBranch) {
            var branch = currentLevel.Branch;
            Debug.Log($"Branch transition detected: {currentLevel.name} → Group {branch.TargetGroupName}, Level {branch.TargetLevelName}");
            TransitionToLevelArea(branch);
            return null;
        }

        return currentLevel.NextLevelInSequence;
    }

    public void TransitionToLevelArea(LevelDataSO.BranchTarget branch) {
        if (branch == null) return;

        var targetArea = GetSelectorForGroup(branch.TargetGroupName);
        if (targetArea == null) {
            Debug.LogError($"Cannot find target LevelArea for group '{branch.TargetGroupName}'");
            return;
        }

        CurrentBranch = branch.TargetGroupName;

        var targetGridPos = targetArea.GetGridPositionOfLevel(branch.TargetLevelName);
        if (targetGridPos != null) {
            targetArea.UnlockCell(targetGridPos);
        }
        else {
            Debug.LogWarning($"Could not find level '{branch.TargetLevelName}' in area '{branch.TargetGroupName}' to unlock.");
        }

        var sequence = DOTween.Sequence();
        sequence.Append(transform.DOMove(-targetArea.transform.position + transform.position, 1f)
            .SetEase(Ease.InOutQuad));
    }


    public void LoadNextLevel() {
        BlockCoordinator.Instance.SetAutoplay(false);

        var currentLevel = Instance.ChosenLevel;
        var next = Instance.GetNextLevel(currentLevel);

        if (next != null) {
            Instance.ChosenLevel = next;
            SceneLoader.Instance.LoadSceneGroup(baseLevelIdx, 0);
        }
        else {
            SceneLoader.Instance.LoadSceneGroup(levelSelectIdx, 0);
        }
    }











#if UNITY_EDITOR

    [Button("Auto-Link All Levels", ButtonSizes.Large), GUIColor(0.2f, 0.8f, 0.2f)]
    [PropertySpace(20)]
    private void AutoLinkAllLevels() {
        // Find all LevelDataSO assets in the project
        string[] guids = AssetDatabase.FindAssets("t:LevelDataSO");
        var allLevels = new List<LevelDataSO>(guids.Length);
        foreach (var guid in guids) {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            allLevels.Add(AssetDatabase.LoadAssetAtPath<LevelDataSO>(path));
        }

        if (allLevels.Count == 0) {
            Debug.LogWarning("No LevelDataSO assets found to link.");
            return;
        }

        // Create a lookup dictionary using the BASE name (before any '_').
        var levelLookupByBaseName = new Dictionary<string, LevelDataSO>();
        foreach (var level in allLevels) {
            var baseName = level.name.Split('_')[0].ToLower();
            if (!levelLookupByBaseName.ContainsKey(baseName)) {
                levelLookupByBaseName.Add(baseName, level);
            }
            else {
                Debug.LogWarning($"Duplicate base name found: '{baseName}'. Using '{levelLookupByBaseName[baseName].name}' and ignoring '{level.name}'. Please ensure base level names are unique.");
            }
        }

        Debug.Log($"Found {allLevels.Count} levels. Processing links...");

        // Iterate through each level and link it.
        foreach (var currentLevel in allLevels) {
            SerializedObject so = new SerializedObject(currentLevel);
            var nextLevelProp = so.FindProperty("_nextLevelInSequence");
            var branchProp = so.FindProperty("_branch");

            // Clear old data
            nextLevelProp.objectReferenceValue = null;
            branchProp.FindPropertyRelative("TargetGroupName").stringValue = string.Empty;
            branchProp.FindPropertyRelative("TargetLevelName").stringValue = string.Empty;

            var currentFullName = currentLevel.name;
            var nameParts = currentFullName.Split('_');
            var baseName = nameParts[0];

            // 1: Check if THIS level has a branch ---
            if (nameParts.Length > 1 && !string.IsNullOrEmpty(nameParts[1])) {
                var branchTargetName = nameParts[1];
                var branchGroupName = new string(branchTargetName.TakeWhile(char.IsLetter).ToArray());

                branchProp.FindPropertyRelative("TargetGroupName").stringValue = branchGroupName;
                branchProp.FindPropertyRelative("TargetLevelName").stringValue = branchTargetName;
                Debug.Log($"[{currentLevel.name}] is a branch level. Target: {branchTargetName}. No sequential next level.");
            }
            // 2: If it's NOT a branch level, find its successor ---
            else {
                var groupPart = new string(baseName.TakeWhile(char.IsLetter).ToArray());
                var remainder = baseName.Substring(groupPart.Length);
                var numberPart = new string(remainder.TakeWhile(char.IsDigit).ToArray());
                var bonusPart = new string(remainder.SkipWhile(char.IsDigit).ToArray());

                if (!int.TryParse(numberPart, out var levelNum)) continue;

                LevelDataSO nextLevelAsset = null;


                // 1: Check for the next bonus level in the same series (e.g., A5a -> A5b).
                if (!string.IsNullOrEmpty(bonusPart)) {
                    var nextBonusChar = (char)(bonusPart[0] + 1);
                    var nextBonusName = $"{groupPart}{levelNum}{nextBonusChar}".ToLower();
                    levelLookupByBaseName.TryGetValue(nextBonusName, out nextLevelAsset);
                }
                // 2: If we are a base level (like A5), check for its first bonus level (A5a).
                else // bonusPart is empty
                {
                    var firstBonusName = $"{groupPart}{levelNum}a".ToLower();
                    levelLookupByBaseName.TryGetValue(firstBonusName, out nextLevelAsset);
                }


                // 3: If no next/first bonus was found, look for the next main number (e.g., A5 or A5b -> A6).
                if (nextLevelAsset == null) {
                    var nextMainLevelName = $"{groupPart}{levelNum + 1}".ToLower();
                    levelLookupByBaseName.TryGetValue(nextMainLevelName, out nextLevelAsset);
                }

                // 4: If still no level found, try to find the start of the next group (e.g., A8 -> B1).
                if (nextLevelAsset == null && groupPart.Length == 1) {
                    var nextGroupChar = (char)(groupPart[0] + 1);

                    // Check for B1a first, then B1
                    var nextGroupFirstName = $"{nextGroupChar}1a".ToLower();
                    if (!levelLookupByBaseName.TryGetValue(nextGroupFirstName, out nextLevelAsset)) {
                        var nextGroupBaseName = $"{nextGroupChar}1".ToLower();
                        levelLookupByBaseName.TryGetValue(nextGroupBaseName, out nextLevelAsset);
                    }
                }

                if (nextLevelAsset != null) {
                    nextLevelProp.objectReferenceValue = nextLevelAsset;
                    Debug.Log($"[{currentLevel.name}] --> linked to next: [{nextLevelAsset.name}]");
                }
                else {
                    Debug.Log($"[{currentLevel.name}] is an end-of-sequence level.");
                }
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(currentLevel);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Level linking complete!");
    }
#endif

}

