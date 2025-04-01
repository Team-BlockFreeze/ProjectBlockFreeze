using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using DG.Tweening;
using UnityEngine.Events;

[System.Serializable]
public class BlockBehaviour : MonoBehaviour
{
    [SerializeField]
    private BlockGrid gridRef;
    public BlockGrid GridRef => gridRef;

    public enum BlockMoveState { teleport, pingpong, patrol, still }
    public BlockMoveState moveMode = BlockMoveState.patrol;

    private bool pingpongIsForward = true;

    private Vector3Int GetNextMoveVec => DirToVec3Int(movePath[moveIdx++]);
    private void AdvanceMoveIdx() { 
        switch(moveMode) {
            case BlockMoveState.pingpong:
                int nextIdx = moveIdx + (pingpongIsForward ? 1 : -1);
                if (nextIdx < 0 || nextIdx >= movePath.Length) pingpongIsForward = !pingpongIsForward;
                else moveIdx = pingpongIsForward ? ++moveIdx : --moveIdx;
                break;
            default:
                moveIdx = (moveIdx+1)% movePath.Length;
                break;
        }
    }

    //private void OnValidate()
    //{
    //    gridRef.TryPlaceOnGrid(this);
    //}

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
        UpdateMovementVisualiser();
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

    private Direction GetOppositeDir(Direction dir)
    {
        if (dir == Direction.up) return Direction.down;
        if (dir == Direction.down) return Direction.up;
        if (dir == Direction.left) return Direction.right;
        if (dir == Direction.right) return Direction.left;
        return Direction.wait;
    }

    public Vector2Int GetMovementIntention()
    {
        Direction moveDir;
        moveDir = movePath[moveIdx];

        switch (moveMode) {
            case BlockMoveState.pingpong:
                moveDir = pingpongIsForward ? moveDir : GetOppositeDir(moveDir);
                break;
            default:
                break;
        }

        return (Vector2Int)DirToVec3Int(moveDir);
    }

    public Vector2Int PeekNextMovementIntention()
    {
        int holdIdx = moveIdx;
        bool holdForward = pingpongIsForward;

        AdvanceMoveIdx();

        var moveIntent = GetMovementIntention();
        moveIdx = holdIdx;
        pingpongIsForward = holdForward;

        return moveIntent;
    }

    Tween moveTween;

    [SerializeField]
    private MeshRenderer cubeRenderer;

    [SerializeField]
    private Material
        normalMat, frozenMat;

    public UnityEvent Event_NextMoveBegan = new UnityEvent();

    public void Move()
    {

        moveTween?.Kill();
        //make sure starting at right point
        //transform.position = gridRef.GetWorldSpaceFromCoord(coord);

        //is cube frozen by player logic
        //cubeRenderer.material = frozen ? frozenMat : normalMat;
        if(frozen) {
            UpdateMovementVisualiser();
            blocked = true;
            Debug.Log($"{gameObject.name} is frozen on {coord}");
            return;
        }

        //update movement visualiser

        Vector2Int movement = lastForces.QueryForce();
        if (!blocked) {
            coord += movement;

            //transform.position += (Vector3)(Vector2)movement;
            //move towards next coord
            moveTween = transform.DOMove(gridRef.GetWorldSpaceFromCoord(coord), 1f).SetEase(Ease.Linear);
        }
        //block animation
        else if (!frozen && blocked && !lastForces.NoInputs() && lastForces.QueryForce() != Vector2Int.zero) {
            //shake on spot
            //moveTween = transform.DOShakePosition(.3f, .1f).OnComplete(
            //    () => transform.position = gridRef.GetWorldSpaceFromCoord(coord)
            //);

            Vector3 bumpTargetPos = ((gridRef.GetWorldSpaceFromCoord(coord)+(Vector3Int)lastForces.QueryForce()) - transform.position) * .15f;
            moveTween = transform.DOMove(bumpTargetPos, .15f).SetRelative().SetLoops(2, LoopType.Yoyo);

            moveTween.Play();
        }
        AdvanceMoveIdx();
        blocked = false;
        //lastForces = new BlockCoordinator.CellForce();

        Event_NextMoveBegan?.Invoke();
        UpdateMovementVisualiser();
        Debug.Log($"{gameObject.name} tried to move from {coord - movement} to {coord}");
    }

    [SerializeField]
    private SpriteRenderer littleDirTriangle;

    [Button]
    private void UpdateMovementVisualiser()
    {
        var colRef = moveIntentionVisual.color;
        if (GetMovementIntention() == Vector2Int.zero) {
            colRef.a = 0;
            moveIntentionVisual.color = colRef;
        }
        else {
            colRef.a = 1;
            moveIntentionVisual.color = colRef;
            moveIntentionVisual.transform.up = (Vector3Int)GetMovementIntention();
        }

        //next move indactor
        colRef = littleDirTriangle.color;
        var nextDir = PeekNextMovementIntention();
        if (nextDir == Vector2Int.zero)
        {
            colRef.a = 0;
            littleDirTriangle.color = colRef;
        }
        else
        {
            colRef.a = 1;
            littleDirTriangle.color = colRef;
            littleDirTriangle.transform.up = (Vector3Int)nextDir;
        }
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

    public bool canBeFrozen = true;
    public bool frozen = false;
    public static UnityEvent OnFreezeBlock;

    //click on block
    private void OnMouseDown()
    {
        if (!canBeFrozen) return;

        frozen = !frozen;
        blocked = frozen;

        cubeRenderer.material = frozen ? frozenMat : normalMat;

        OnFreezeBlock?.Invoke();
    }

    [Button]
    private void OnDestroy()
    {
        moveTween?.Kill();

        bool wasOnList = gridRef.ActiveGridState.BlocksList.Remove(this);
        if(wasOnList && gridRef.isValidGridCoord(coord))
            gridRef.ActiveGridState.GridBlockStates[coord.x, coord.y] = null;
        gridRef.ActiveGridState.UpdateCoordList();
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
