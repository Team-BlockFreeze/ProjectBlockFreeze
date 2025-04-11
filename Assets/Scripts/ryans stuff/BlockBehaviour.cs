using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using DG.Tweening;
using UnityEngine.Events;
using UnityEditor.Build.Pipeline;
using System.Runtime.CompilerServices;
using System;

[System.Serializable]
public class BlockBehaviour : LoggerMonoBehaviour {
    [Title("Grid Reference")]
    [SerializeField, InlineEditor]
    private BlockGrid gridRef;
    public BlockGrid GridRef => gridRef;

    public void SetGridRef(BlockGrid grid) { gridRef = grid; }

    public enum BlockMoveState { loop, pingpong, teleport, still }
    [Title("Movement Settings")]
    [EnumToggleButtons]
    public BlockMoveState moveMode = BlockMoveState.loop;

    private bool pingpongIsForward = true;
    public bool GetPingpongIsForward() => pingpongIsForward;

    public bool canBeFrozen = true;
    private Vector3Int GetNextMoveVec => DirToVec3Int(movePath[moveIdx++]);

    [BoxGroup("Positioning")]
    public Vector2Int coord = Vector2Int.zero;

    [ShowInInspector, ReadOnly]
    private int moveIdx;
    public int GetMoveIdx() => moveIdx;
    public void IncrementMoveIdx() => moveIdx++;
    public void DecrementMoveIdx() => moveIdx--;



    public enum Direction { up, down, left, right, wait }
    [SerializeField]
    private Direction[] movePath;

    public void SetMovePath(Direction[] newPath) {
        movePath = newPath;
    }

    public Direction[] GetMovePath() => movePath;

    [Title("Visuals")]
    [SerializeField, FoldoutGroup("Renderers")]
    private MeshRenderer cubeRenderer;

    [SerializeField, FoldoutGroup("Materials")]
    private Material normalMat, frozenMat;

    [SerializeField, FoldoutGroup("Renderers")]
    private SpriteRenderer moveIntentionVisual;

    [SerializeField, FoldoutGroup("Renderers")]
    private SpriteRenderer littleDirTriangle;

    [FoldoutGroup("Debug")]
    public bool frozen = false;

    [FoldoutGroup("Debug")]
    public bool blocked = false;


    private void AdvanceMoveIdx() {
        switch (moveMode) {
            case BlockMoveState.pingpong:
                int nextIdx = moveIdx + (pingpongIsForward ? 1 : -1);
                if (nextIdx < 0 || nextIdx >= movePath.Length) pingpongIsForward = !pingpongIsForward;
                else moveIdx = pingpongIsForward ? ++moveIdx : --moveIdx;
                break;
            default:
                moveIdx = (moveIdx + 1) % movePath.Length;
                break;
        }
    }

    [Button]
    public void TryAddToGrid() {
        gridRef.TryPlaceOnGrid(this);
    }




    private Tween activeTween;

    [ReadOnly]
    public BlockCoordinator.CellForce lastForces = new BlockCoordinator.CellForce();

    private void Start() {
        UpdateMovementVisualiser();
        if (frozen) blocked = true;
    }


    private Direction GetOppositeDir(Direction dir) {
        if (dir == Direction.up) return Direction.down;
        if (dir == Direction.down) return Direction.up;
        if (dir == Direction.left) return Direction.right;
        if (dir == Direction.right) return Direction.left;
        return Direction.wait;
    }

    public Vector2Int GetMovementIntention() {
        Direction moveDir;
        moveDir = movePath != null ? movePath[moveIdx] : Direction.wait;

        switch (moveMode) {
            case BlockMoveState.still:
                moveDir = Direction.wait;
                break;
            case BlockMoveState.pingpong:
                moveDir = pingpongIsForward ? moveDir : GetOppositeDir(moveDir);
                break;
            default:
                break;
        }

        return (Vector2Int)DirToVec3Int(moveDir);
    }

    public Vector2Int PeekNextMovementIntention() {
        int holdIdx = moveIdx;
        bool holdForward = pingpongIsForward;

        AdvanceMoveIdx();

        var moveIntent = GetMovementIntention();
        moveIdx = holdIdx;
        pingpongIsForward = holdForward;

        return moveIntent;
    }

    Tween moveTween;



    public UnityEvent Event_NextMoveBegan = new UnityEvent();

    //! Animation events 
    //! Note: Will get called multiple times because all animations play on their instance
    public static event Action OnAnimationCompleted;
    public static event Action OnAnimationStarted;



    public void Move() {

        moveTween?.Kill();
        if (frozen) {
            UpdateMovementVisualiser();
            blocked = true;
            Log($"{gameObject.name} is frozen on {coord}");
            return;
        }

        //update movement visualiser

        Vector2Int currentForceVec2I = lastForces.QueryForce();
        if (!blocked && currentForceVec2I != Vector2Int.zero) {
            Log("regular movement anim", gameObject);
            coord += currentForceVec2I;

            OnAnimationStarted?.Invoke();
            moveTween = transform.DOMove(gridRef.GetWorldSpaceFromCoord(coord), GameSettings.Instance.gameTickInSeconds)
                     .SetEase(Ease.Linear)
                     .OnComplete(() => {
                         OnAnimationCompleted?.Invoke();
                     });
        }
        else if (!frozen && lastForces.YLocked() || lastForces.XLocked() && lastForces.XLocked() != lastForces.YLocked()) {
            Log("triggering fall back animation", gameObject);
            Vector3 bumpTargetPos = (gridRef.GetWorldSpaceFromCoord(coord) + (Vector3Int)lastForces.firstDir - transform.position) * .15f - Vector3.back * coord.y * .01f;

            moveTween = transform.DOMove(bumpTargetPos, .15f).SetRelative().SetLoops(2, LoopType.Yoyo).OnComplete(() => {
                OnAnimationCompleted?.Invoke();
            });

            moveTween.Play();
        }
        //block animation
        else {

            Log($"{gameObject.name} blocked bump anim");
            Vector3 bumpTargetPos = ((gridRef.GetWorldSpaceFromCoord(coord) + (Vector3Int)currentForceVec2I) - transform.position) * .15f - Vector3.back * coord.y * .01f;

            moveTween = transform.DOMove(bumpTargetPos, .15f).SetRelative().SetLoops(2, LoopType.Yoyo).OnComplete(() => {
                OnAnimationCompleted?.Invoke();
            });

            moveTween.Play();
        }


        AdvanceMoveIdx();
        blocked = false;

        UpdateMovementVisualiser();
        Event_NextMoveBegan?.Invoke();
        UpdateMovementVisualiser();
        Log($"{gameObject.name} tried to move from {coord - currentForceVec2I} to {coord}");
    }



    [Button]
    public void UpdateMovementVisualiser() {
        var colRef = moveIntentionVisual.color;
        var moveIntent = GetMovementIntention();


        if (moveIntent == Vector2Int.zero) {
            colRef.a = 0;
            moveIntentionVisual.color = colRef;
        }
        else {
            colRef.a = 1;
            moveIntentionVisual.color = colRef;
            moveIntentionVisual.transform.up = (Vector3Int)moveIntent;
        }

        //next move indactor
        colRef = littleDirTriangle.color;
        var nextDir = PeekNextMovementIntention();
        if (nextDir == Vector2Int.zero) {
            colRef.a = 0;
            littleDirTriangle.color = colRef;
        }
        else {
            colRef.a = 1;
            littleDirTriangle.color = colRef;
            littleDirTriangle.transform.up = (Vector3Int)nextDir;
        }
    }

    private Vector3Int DirToVec3Int(Direction dir) {
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

    public static UnityEvent OnFreezeBlock;


    public void TrySetFreeze(bool? freezeState = null) {
        if (!canBeFrozen) return;

        if (freezeState == null) freezeState = !frozen;

        frozen = freezeState.Value;
        blocked = frozen;

        cubeRenderer.material = frozen ? frozenMat : normalMat;

        OnFreezeBlock?.Invoke();
    }

    [Button]
    private void OnDestroy() {
        moveTween?.Kill();

        if (gridRef == null) return;

        bool wasOnList = gridRef.ActiveGridState.BlocksList.Remove(this);
        if (wasOnList && gridRef.isValidGridCoord(coord))
            gridRef.ActiveGridState.GridBlockStates[coord.x, coord.y] = null;
        gridRef.ActiveGridState.UpdateCoordList();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos() {
        //draw force line
        Gizmos.color = Color.red;
        if (lastForces.AllInputs()) Gizmos.DrawWireCube(transform.position, Vector3.one * .2f);
        else Gizmos.DrawLine(transform.position, transform.position + (Vector3)(Vector3Int)lastForces.QueryForce() * .35f);
    }

#endif

}
