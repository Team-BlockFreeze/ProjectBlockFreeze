using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Sirenix.OdinInspector;
using Systems.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

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
        foreach (var selector in selectors) selector.RebuildMatrixFromScene();
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
        Debug.Log("Selector: " + selector?.name);

        if (selector != null) {
            var gridPos = selector.GetGridPosition(completedLevel);

            if (gridPos.HasValue) {
                selector.UnlockAdjacentCells(gridPos.Value);
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
        var currentName = currentLevel.name;

        // Split by branch, eg: A2b_C1 → A2b where branch is 'C1'
        var parts = currentName.Split('_');
        var baseName = parts[0];
        var branchTarget = parts.Length > 1 ? parts[1] : null;

        Debug.Log($"baseName: {baseName}, branchTarget: {branchTarget}");

        // Skip if branch detected
        if (!string.IsNullOrEmpty(branchTarget)) {
            TransitionToLevelArea(branchTarget);

            Debug.Log($"Branch transition detected: {baseName} → {branchTarget}");
            return null;
        }

        // Parse baseName: Group (letters), Number (digits), Bonus (optional letter)
        var groupPart = new string(baseName.TakeWhile(char.IsLetter).ToArray());
        var remainder = baseName.Substring(groupPart.Length);
        var numberPart = new string(remainder.TakeWhile(char.IsDigit).ToArray());
        var bonusPart = new string(remainder.SkipWhile(char.IsDigit).ToArray());

        if (!int.TryParse(numberPart, out var levelNum)) return null;

        var currentSelector = GetSelectorForGroup(groupPart);
        if (currentSelector == null) return null;

        var levels = currentSelector.Levels;

        // Try next bonus level (eg., A2a -> A2b)
        if (!string.IsNullOrEmpty(bonusPart)) {
            var nextBonus = (char)(bonusPart[0] + 1);
            var nextBonusName = $"{groupPart}{levelNum}{nextBonus}";
            var nextBonusLevel =
                levels.FirstOrDefault(l => l.name.Equals(nextBonusName, StringComparison.OrdinalIgnoreCase));
            if (nextBonusLevel != null) return nextBonusLevel;

            // Fallback to next base level (eg., A3)
            var nextBaseName = $"{groupPart}{levelNum + 1}";
            var nextBase = levels.FirstOrDefault(l => l.name.Equals(nextBaseName, StringComparison.OrdinalIgnoreCase));
            if (nextBase != null) return nextBase;

            return FindFirstLevelInNextGroup(groupPart);
        }

        // Try bonus levels (A2a, A2b...) first
        for (var c = 'a'; c <= 'z'; c++) {
            var bonusName = $"{groupPart}{levelNum}{c}";
            var bonus = levels.FirstOrDefault(l => l.name.Equals(bonusName, StringComparison.OrdinalIgnoreCase));
            if (bonus != null) return bonus;
        }

        // Try next base level
        var nextBaseLevelName = $"{groupPart}{levelNum + 1}";
        var nextBaseLevel =
            levels.FirstOrDefault(l => l.name.Equals(nextBaseLevelName, StringComparison.OrdinalIgnoreCase));
        if (nextBaseLevel != null) return nextBaseLevel;

        return FindFirstLevelInNextGroup(groupPart);
    }

    private void TransitionToLevelArea(string branchTarget) {
        if (string.IsNullOrEmpty(branchTarget))
            return;


        var branchGroup = new string(branchTarget.TakeWhile(char.IsLetter).ToArray());
        var branchNumber = new string(branchTarget.SkipWhile(char.IsLetter).TakeWhile(char.IsDigit).ToArray());

        // Find the target LevelArea using the branch group
        var targetArea = GetSelectorForGroup(branchGroup);
        if (targetArea == null)
            return;

        CurrentBranch = branchGroup;

        // Unlock level you're transitioning towards
        var targetAreaGridPos = targetArea.GetGridPositionOfLevel(branchTarget);
        targetArea.UnlockCell(targetAreaGridPos);

        var sequence = DOTween.Sequence();
        sequence.Append(transform.DOMove(-targetArea.transform.position + transform.position, 1f)
            .SetEase(Ease.InOutQuad));
        // .Join(transform.DOScale(Vector3.one * 0.8f, 0.5f))
        // .Append(transform.DOScale(Vector3.one, 0.5f));
    }



    private LevelDataSO FindFirstLevelInNextGroup(string currentGroup) {
        if (string.IsNullOrEmpty(currentGroup) || currentGroup.Length != 1)
            return null;

        var nextGroup = (char)(currentGroup[0] + 1);
        var selector = GetSelectorForGroup(nextGroup.ToString());
        if (selector == null) return null;

        for (var i = 1; i <= 10; i++) {
            var candidate = $"{nextGroup}{i}";
            var level =
                selector.Levels.FirstOrDefault(l => l.name.Equals(candidate, StringComparison.OrdinalIgnoreCase));
            if (level != null) return level;
        }

        return null;
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
}