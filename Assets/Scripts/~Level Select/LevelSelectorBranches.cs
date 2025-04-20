/// <summary>
/// Manages the level selection system and branching progression in the game.
/// This singleton class handles:
/// - Level group organization (A1, A2, B1, etc.)
/// - Level progression and unlocking
/// - Branch transitions between level groups
/// - Level selection UI management
/// 
/// Level Naming Convention:
/// - Basic Format: [Group][Number][Bonus]_[BranchTarget]
/// - Example: "A3a_C2" means:
///   * Group A, Level 3, Bonus 'a', with branch to Group C Level 2
/// - Progression:
///   * Within group: A1 -> A2 -> A3 etc.
///   * Bonus levels: A2 -> A2a -> A2b etc.
///   * Cross-group: A3 -> B1 (when no more A levels)
///   * Branch points: A3a_C2 (branches to group C)
/// </summary>
using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Sirenix.OdinInspector;
using Systems.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelectorBranches : PersistentSingleton<LevelSelectorBranches> {

    [SerializeField]
    private List<LevelSelector> selectors;

    private Dictionary<string, LevelSelector> selectorMap;

    [SerializeField, ReadOnly]
    private string currentBranch;

    public string CurrentBranch {
        get => currentBranch;
        set => currentBranch = value;
    }



    [SerializeField, ReadOnly]
    private LevelDataSO chosenLevel;

    public LevelDataSO ChosenLevel {
        get { return chosenLevel; }
        set { chosenLevel = value; }
    }


    protected override void Awake() {
        base.Awake();

        selectors = GetComponentsInChildren<LevelSelector>(includeInactive: true).ToList(); //! Include inactive is a thing apparently
        selectorMap = selectors.ToDictionary(s => s.GroupName, s => s);

#if UNITY_EDITOR
        if (!Application.isPlaying) return;
#endif
        foreach (var selector in selectors) {
            selector.RebuildMatrixFromScene();
        }
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

    private void OnLevelComplete(LevelDataSO completedLevel) {
        Debug.Log($"Level completed: {completedLevel.name}");

        var selector = GetSelectorForLevel(completedLevel);
        Debug.Log("Selector: " + selector?.name);

        if (selector != null) {
            Vector2Int? gridPos = selector.GetGridPosition(completedLevel);
            Debug.Log("Grid pos: " + gridPos);

            if (gridPos.HasValue) {
                selector.UnlockAdjacentCells(gridPos.Value);
            }
        }
    }


    public void HideAllButtons() {
        // Debug.Log("HideAllButtons");
        foreach (var selector in selectors) {
            selector.HideAllButtons();
        }
    }

    public void ShowAllButtons() {
        foreach (var selector in selectors) {
            selector.ShowAllButtons();
        }
    }

    public LevelSelector GetSelectorForGroup(string groupName) {
        if (!string.IsNullOrEmpty(CurrentBranch)) {
            selectorMap.TryGetValue(CurrentBranch, out var branchSelector);
            if (branchSelector != null) return branchSelector;
        }

        selectorMap.TryGetValue(groupName, out var selector);
        return selector;
    }

    public LevelSelector GetSelectorForLevel(LevelDataSO level) {
        string name = level.name;
        string[] parts = name.Split('_');
        string group = new string(parts[0].TakeWhile(char.IsLetter).ToArray());

        return GetSelectorForGroup(group);
    }


    /// <summary>
    /// Example: "A3a_C2"
    /// - Group: "A"
    /// - Level Number: 3
    /// - Bonus Letter: "a"
    /// - Branch Target: "C2"
    /// - Follow-up Level (if not branching): would look for A3b, then A4, etc.
    /// - Since there's a branch ("_C2"), progression halts and logs the transition.
    /// </summary>
    /// <param name="currentLevel"></param>
    public LevelDataSO GetNextLevel(LevelDataSO currentLevel) {
        string currentName = currentLevel.name;

        // Split by branch, eg: A2b_C1 → A2b where branch is 'C1'
        string[] parts = currentName.Split('_');
        string baseName = parts[0];
        string branchTarget = parts.Length > 1 ? parts[1] : null;

        // Skip if branch detected
        if (!string.IsNullOrEmpty(branchTarget)) {
            transform.DOMoveX(transform.position.x - 20, 5f);


            Debug.Log($"Branch transition detected: {baseName} → {branchTarget}");
            return null;
        }

        // Parse baseName: Group (letters), Number (digits), Bonus (optional letter)
        string groupPart = new string(baseName.TakeWhile(char.IsLetter).ToArray());
        string remainder = baseName.Substring(groupPart.Length);
        string numberPart = new string(remainder.TakeWhile(char.IsDigit).ToArray());
        string bonusPart = new string(remainder.SkipWhile(char.IsDigit).ToArray());

        if (!int.TryParse(numberPart, out int levelNum)) return null;

        var currentSelector = GetSelectorForGroup(groupPart);
        if (currentSelector == null) return null;

        var levels = currentSelector.Levels;

        // Try next bonus level (eg., A2a -> A2b)
        if (!string.IsNullOrEmpty(bonusPart)) {
            char nextBonus = (char)(bonusPart[0] + 1);
            string nextBonusName = $"{groupPart}{levelNum}{nextBonus}";
            var nextBonusLevel = levels.FirstOrDefault(l => l.name.Equals(nextBonusName, StringComparison.OrdinalIgnoreCase));
            if (nextBonusLevel != null) return nextBonusLevel;

            // Fallback to next base level (eg., A3)
            string nextBaseName = $"{groupPart}{levelNum + 1}";
            var nextBase = levels.FirstOrDefault(l => l.name.Equals(nextBaseName, StringComparison.OrdinalIgnoreCase));
            if (nextBase != null) return nextBase;

            return FindFirstLevelInNextGroup(groupPart);
        }

        // Try bonus levels (A2a, A2b...) first
        for (char c = 'a'; c <= 'z'; c++) {
            string bonusName = $"{groupPart}{levelNum}{c}";
            var bonus = levels.FirstOrDefault(l => l.name.Equals(bonusName, StringComparison.OrdinalIgnoreCase));
            if (bonus != null) return bonus;
        }

        // Try next base level
        string nextBaseLevelName = $"{groupPart}{levelNum + 1}";
        var nextBaseLevel = levels.FirstOrDefault(l => l.name.Equals(nextBaseLevelName, StringComparison.OrdinalIgnoreCase));
        if (nextBaseLevel != null) return nextBaseLevel;

        return FindFirstLevelInNextGroup(groupPart);
    }


    private LevelDataSO FindFirstLevelInNextGroup(string currentGroup) {
        if (string.IsNullOrEmpty(currentGroup) || currentGroup.Length != 1)
            return null;

        char nextGroup = (char)(currentGroup[0] + 1);
        var selector = GetSelectorForGroup(nextGroup.ToString());
        if (selector == null) return null;

        for (int i = 1; i <= 10; i++) {
            string candidate = $"{nextGroup}{i}";
            var level = selector.Levels.FirstOrDefault(l => l.name.Equals(candidate, StringComparison.OrdinalIgnoreCase));
            if (level != null) return level;
        }

        return null;
    }

    const int baseLevelIdx = 2;
    const int levelSelectIdx = 1;

    public void LoadNextLevel() {
        LevelDataSO currentLevel = LevelSelectorBranches.Instance.ChosenLevel;

        LevelDataSO next = LevelSelectorBranches.Instance.GetNextLevel(currentLevel);

        if (next != null) {
            LevelSelectorBranches.Instance.ChosenLevel = next;
            SceneLoader.Instance.LoadSceneGroup(baseLevelIdx, 0);
        }
        else {
            SceneLoader.Instance.LoadSceneGroup(levelSelectIdx, 0);
        }
    }


}
