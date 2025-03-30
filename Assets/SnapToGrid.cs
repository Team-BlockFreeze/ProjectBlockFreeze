using UnityEngine;
using Sirenix.OdinInspector;

public class SnapToGrid : MonoBehaviour
{
    [SerializeField]
    private Vector3 offset;
    private BlockGrid blockGrid;

    [Button]
    public void SnapToGridWorldPos()
    {
        blockGrid = FindFirstObjectByType<BlockGrid>();
        transform.position = blockGrid.GetWorldPosSnappedToGrid(transform.position) + offset;
    }
}
