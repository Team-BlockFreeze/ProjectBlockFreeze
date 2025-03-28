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

    [ReadOnly]
    public BlockCoordinator.CellForce lastForces = new BlockCoordinator.CellForce();

    private void Start()
    {
        
        //activeTween = transform.DOMove(GetNextMoveVec, 1f).SetRelative().SetEase(Ease.Linear);
        //AdvanceMoveIdx();

        //InvokeRepeating(nameof(QueueNextTween), .1f, 1f);
    }

    [SerializeField]
    private SpriteRenderer moveIntentionVisual;

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

    Tween moveTween;

    public void Move()
    {
        moveTween?.Kill();
        //make sure starting at right point
        transform.position = gridRef.GetWorldSpaceFromCoord(coord);

        Vector2Int movement = lastForces.QueryForce();
        if (!blocked) {
            coord += movement;

            //transform.position += (Vector3)(Vector2)movement;
            //move towards next coord
            transform.DOMove(gridRef.GetWorldSpaceFromCoord(coord), 1f).SetEase(Ease.Linear);
        }

        //update movement visualiser
        var colRef = moveIntentionVisual.color;
        if(GetMovementIntention() == Vector2Int.zero) {
            colRef.a = 0;
            moveIntentionVisual.color = colRef;
        } else {
            colRef.a = 1;
            moveIntentionVisual.color = colRef;
            moveIntentionVisual.transform.up = (Vector3Int)GetMovementIntention();
        }

        AdvanceMoveIdx();

        //block animation
        if (blocked || !lastForces.NoInputs() && lastForces.QueryForce() == Vector2Int.zero) {
            moveTween = transform.DOShakePosition(.3f, .1f).OnComplete(
                () => transform.position = gridRef.GetWorldSpaceFromCoord(coord)
            );
            moveTween.Play();
        }
        blocked = false;
        //lastForces = new BlockCoordinator.CellForce();

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
#if UNITY_EDITOR

    private void OnDrawGizmos()
    {
        //draw force line
        Gizmos.color = Color.red;
        if (lastForces.AllInputs()) Gizmos.DrawWireCube(transform.position, Vector3.one * .2f); 
        else Gizmos.DrawLine(transform.position, transform.position + (Vector3)(Vector3Int)lastForces.QueryForce() * .35f);
    }

    //    private void OnDrawGizmosSelected()
    //    {
    //        Handles.PositionHandle()
    //        Handles.DrawAAPolyLine
    //    }

#endif
}
