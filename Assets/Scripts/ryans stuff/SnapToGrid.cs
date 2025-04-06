using UnityEngine;
using Sirenix.OdinInspector;

public class SnapToGrid : LoggerMonoBehaviour {
    [SerializeField]
    private Vector3 offset;
    private BlockGrid blockGrid;

    [ReadOnly]
    [SerializeField]
    private Vector2Int internalCoord;

    [Button]
    public void SnapToGridWorldPos() {
        blockGrid = FindFirstObjectByType<BlockGrid>();
        transform.position = blockGrid.GetWorldPosSnappedToGrid(transform.position) + offset;
    }

    [Button]
    public void SnapGoalToGrid() {
        blockGrid = FindFirstObjectByType<BlockGrid>();
        transform.position = blockGrid.GetWorldPosSnappedToGrid(transform.position) + offset;
        var tempCoord = blockGrid.GetGridCoordFromWorldPos(transform.position);

        if (blockGrid.isValidGridCoord(tempCoord)) {
            internalCoord = tempCoord;
            blockGrid.SetGoalCoord(tempCoord);
            Log($"Set goal coord to {tempCoord}");
        }
        else LogWarning("failed to set goal coord");
    }
}
