using UnityEngine;
using UnityEngine.EventSystems;

public class LevelBranchTransition : MonoBehaviour, IPointerClickHandler {
    private const string NAME_PREFIX = "Arrow_from_";

    public bool unlocked = false;

    public void UnlockBranch() {
        unlocked = true;
    }

    public void ActivateBranchTransition() {
        this.gameObject.SetActive(true);
    }
    public void DeactivateBranchTransition() {
        this.gameObject.SetActive(false);
    }


    public void OnPointerClick(PointerEventData eventData) {
        if (!TryParseBranchTarget(out LevelDataSO.BranchTarget branchTarget)) {
            return;
        }

        if (LevelAreaController.Instance == null) {
            Debug.LogError("LevelAreaController.Instance is not found in the scene. Cannot transition.", this);
            return;
        }

        Debug.Log($"Transitioning to Level: {branchTarget.TargetLevelName} in Group: {branchTarget.TargetGroupName}");
        LevelAreaController.Instance.TransitionToLevelArea(branchTarget);
    }

    private bool TryParseBranchTarget(out LevelDataSO.BranchTarget branchTarget) {
        branchTarget = null;
        string objectName = transform.parent.name;
        if (!objectName.StartsWith(NAME_PREFIX)) {
            Debug.LogError($"GameObject name '{objectName}' does not have the required prefix '{NAME_PREFIX}'.", this);
            return false;
        }

        string levelPartsString = objectName.Substring(NAME_PREFIX.Length);
        string[] levelParts = levelPartsString.Split('_');
        if (levelParts.Length != 2) {
            Debug.LogError($"Invalid name format for '{objectName}'. Expected format: '{NAME_PREFIX}[FromLevel]_[ToLevel]'.", this);
            return false;
        }

        string targetLevelFullName = levelParts[1];
        if (string.IsNullOrEmpty(targetLevelFullName)) {
            Debug.LogError($"Target level name is missing in '{objectName}'.", this);
            return false;
        }

        string targetGroupName = targetLevelFullName.Substring(0, 1);
        branchTarget = new LevelDataSO.BranchTarget {
            TargetGroupName = targetGroupName,
            TargetLevelName = targetLevelFullName
        };

        return true;
    }
}