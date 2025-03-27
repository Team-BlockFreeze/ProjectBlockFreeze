using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using DG.Tweening;

[System.Serializable]
public class BlockBehaviour : MonoBehaviour
{
    [SerializeField]
    private BlockGrid gridRef;

    private Vector3Int GetNextMoveVec => DirToVec3Int(movePath[moveIdx++]);
    private void AdvanceMoveIdx() { moveIdx = (moveIdx+1)% movePath.Length; }

    [Button]
    public void TryAddToGrid()
    {
        gridRef.TryPlaceOnGrid(this);
    }

    [SerializeField]
    public Vector2Int coord = Vector2Int.zero;

    [ShowInInspector]
    int moveIdx;

    public bool blocked = false;

    private Tween activeTween;

    public BlockCoordinator.CellForce lastForces = new BlockCoordinator.CellForce();

    private void Start()
    {
        
        //activeTween = transform.DOMove(GetNextMoveVec, 1f).SetRelative().SetEase(Ease.Linear);
        //AdvanceMoveIdx();

        //InvokeRepeating(nameof(QueueNextTween), .1f, 1f);
    }

    private void QueueNextTween()
    {
        //Debug.Log("Queueing tween");
        Tween nextTween = transform.DOMove(DirToVec3Int(movePath[moveIdx++]), 1f).SetRelative().SetEase(Ease.Linear).Pause();
        activeTween.OnComplete(() => nextTween.Play());
        activeTween = nextTween;
        AdvanceMoveIdx();
    }

    private enum Direction { up, down, left, right, wait}
    [SerializeField]
    private Direction[] movePath;

    public Vector2Int GetMovementIntention()
    {
        var vec3 = DirToVec3Int(movePath[moveIdx]);
        return (Vector2Int)vec3;
    }

    public void Move()
    {
        //make sure starting at right point
        transform.position = gridRef.GetWorldSpaceFromCoord(coord);

        Vector2Int movement = lastForces.QueryForce();
        coord += movement;

        //transform.position += (Vector3)(Vector2)movement;
        //move towards next coord
        transform.DOMove(gridRef.GetWorldSpaceFromCoord(coord), 1f).SetEase(Ease.Linear);

        lastForces = new BlockCoordinator.CellForce();
        AdvanceMoveIdx();
        blocked = false;

        Debug.Log($"{gameObject.name} tried to move from {coord - movement} to {coord}");
    }

    private Vector3Int DirToVec3Int(Direction dir)
    {
        Vector3Int dirVec = Vector3Int.zero;
        switch (dir) {
            case Direction.up:
                dirVec = Vector3Int.up;
                break;
            case Direction.down:
                dirVec = Vector3Int.down;
                break;
            case Direction.left:
                dirVec = Vector3Int.left;
                break;
            case Direction.right:
                dirVec = Vector3Int.right;
                break;
        }
        return dirVec;
    }

//    private void OnDrawGizmosSelected()
//    {
//        Handles.PositionHandle()
//        Handles.DrawAAPolyLine
//    }
}
