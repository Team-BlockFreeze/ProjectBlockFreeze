using System;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

[SelectionBase]
public class BlockBehaviour : LoggerMonoBehaviour {
    public enum BlockMoveState {
        loop,
        pingpong,
        teleport,
        still
    }


    public enum Direction {
        up,
        down,
        left,
        right,
        wait
    }

    public static UnityEvent OnFreezeBlock;

    [Title("Grid Reference")]
    [SerializeField]
    [InlineEditor]
    private BlockGrid gridRef;

    [Title("Movement Settings")]
    [EnumToggleButtons]
    public BlockMoveState moveMode = BlockMoveState.loop;


    [BoxGroup("Positioning")] public Vector2Int coord = Vector2Int.zero;

    [SerializeField] private Direction[] movePath;

    [Title("Visuals")]
    [SerializeField]
    [FoldoutGroup("Renderers")]
    private MeshRenderer cubeRenderer;

    [SerializeField]
    [FoldoutGroup("Materials")]
    private Material normalMat, frozenMat, pingpongMAT;

    [SerializeField]
    [FoldoutGroup("Renderers")]
    private SpriteRenderer moveIntentionVisual;

    [SerializeField]
    [FoldoutGroup("Renderers")]
    private SpriteRenderer littleDirTriangle;
    [FoldoutGroup("Renderers")]
    [SerializeField] public SpriteRenderer blockTypeIcon_topleft;
    [FoldoutGroup("Renderers")]
    [SerializeField] public SpriteRenderer blockTypeIcon_topright;

    public void SetBlockTypeIcons(Dictionary<string, Sprite> icons) {
        if (icons.ContainsKey("topleft")) blockTypeIcon_topleft.sprite = icons["topleft"];
        if (icons.ContainsKey("topright")) blockTypeIcon_topright.sprite = icons["topright"];
        // if (icons.ContainsKey("bottomleft")) blockTypeIcon_bottomleft.sprite = icons["bottomleft"];
        // if (icons.ContainsKey("bottomright")) blockTypeIcon_bottomright.sprite = icons["bottomright"];
    }

    [ReadOnly] public string blockType;

    [FoldoutGroup("Debug")] public bool frozen;

    [FoldoutGroup("Debug")] public bool blocked;

    [FoldoutGroup("Special Properties")] public bool pushableWhenFrozen = false;
    [FoldoutGroup("Special Properties")] public bool canBeFrozen = true;
    [FoldoutGroup("Special Properties")] public bool phaseThrough = false;


    [ReadOnly] public BlockCoordinator.CellForce lastForces = new();


    public UnityEvent Event_NextMoveBegan = new();


    [SerializeField] private Material blockMAT;


    private Tween activeTween;

    [ShowInInspector][ReadOnly] private int moveIdx;

    private Tween moveTween;

    private bool pingpongIsForward = true;
    public BlockGrid GridRef => gridRef;
    private Vector3Int GetNextMoveVec => DirToVec3Int(movePath[moveIdx++]);

    public Material BlockMaterial {
        get => blockMAT;
        set => blockMAT = value;
    }

    private void Start() {
        UpdateMovementVisualiser();
        if (frozen) blocked = true;
    }

    [Button]
    private void OnDestroy() {
        moveTween?.Kill();

        if (gridRef == null) return;

        var wasOnList = gridRef.ActiveGridState.BlocksList.Remove(this);
        if (wasOnList && gridRef.isValidGridCoord(coord) && gridRef.ActiveGridState.GridBlockStates != null) {

            gridRef.ActiveGridState.GridBlockStates[coord.x, coord.y] = null;
        }
        gridRef.ActiveGridState.UpdateCoordList();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos() {
        //draw force line
        Gizmos.color = Color.red;
        if (lastForces.AllInputs()) Gizmos.DrawWireCube(transform.position, Vector3.one * .2f);
        else
            Gizmos.DrawLine(transform.position,
                transform.position + (Vector3)(Vector3Int)lastForces.QueryForce() * .35f);
    }

#endif

    public void SetGridRef(BlockGrid grid) {
        gridRef = grid;
    }

    public bool GetPingpongIsForward() {
        return pingpongIsForward;
    }

    public void SetPingpongIsForward(bool value) {
        pingpongIsForward = value;
    }

    public int GetMoveIdx() {
        return moveIdx;
    }

    public void SetMoveIdx(int idx) {
        moveIdx = idx;
    }

    public void SetMovePath(Direction[] newPath) {
        movePath = newPath;
    }

    public Direction[] GetMovePath() {
        return movePath;
    }


    private void AdvanceMoveIdx() {
        switch (moveMode) {
            case BlockMoveState.pingpong:
                var nextIdx = moveIdx + (pingpongIsForward ? 1 : -1);
                if (nextIdx < 0 || nextIdx >= movePath.Length) pingpongIsForward = !pingpongIsForward;
                else moveIdx = pingpongIsForward ? ++moveIdx : --moveIdx;
                break;
            default:
                moveIdx = (moveIdx + 1) % movePath.Length;
                break;
        }
    }


    //! Kerry: For undoing
    public void ReverseMoveIdx() {
        switch (moveMode) {
            case BlockMoveState.pingpong:
                var prevIdx = moveIdx + (pingpongIsForward ? -1 : 1);
                if (prevIdx < 0 || prevIdx >= movePath.Length)
                    pingpongIsForward = !pingpongIsForward;
                else
                    moveIdx = pingpongIsForward ? --moveIdx : ++moveIdx;
                break;

            default:
                moveIdx = (moveIdx - 1 + movePath.Length) % movePath.Length;
                break;
        }
    }


    [Button]
    public void TryAddToGrid() {
        gridRef.TryPlaceOnGrid(this);
    }


    private Direction GetOppositeDir(Direction dir) {
        if (dir == Direction.up) return Direction.down;
        if (dir == Direction.down) return Direction.up;
        if (dir == Direction.left) return Direction.right;
        if (dir == Direction.right) return Direction.left;
        return Direction.wait;
    }

    public Vector2Int GetMovementIntention() {
        if (frozen) return Vector2Int.zero;

        Direction moveDir;
        moveDir = movePath != null ? movePath[moveIdx] : Direction.wait;

        switch (moveMode) {
            case BlockMoveState.still:
                moveDir = Direction.wait;
                break;
            case BlockMoveState.pingpong:
                moveDir = pingpongIsForward ? moveDir : GetOppositeDir(moveDir);
                break;
        }

        return (Vector2Int)DirToVec3Int(moveDir);
    }

    public Vector2Int PeekNextMovementIntention() {
        var holdIdx = moveIdx;
        var holdForward = pingpongIsForward;

        AdvanceMoveIdx();

        var moveIntent = GetMovementIntention();
        moveIdx = holdIdx;
        pingpongIsForward = holdForward;

        return moveIntent;
    }

    //! Animation events 
    //! Note: Will get called multiple times because all animations play on their instance
    public static event Action OnAnimationCompleted;
    public static event Action OnAnimationStarted;


    public void Move() {
        moveTween?.Kill();
        if (frozen && !pushableWhenFrozen) {
            UpdateMovementVisualiser();
            blocked = true;
            Log($"{gameObject.name} is immovably frozen on {coord}");
            return;
        }


        //update movement visualiser

        var currentForceVec2I = lastForces.QueryForce();

        // Debug.Log($"move called on {gameObject.name} with force {currentForceVec2I}" + "blocked: " + blocked);
        if (!blocked && currentForceVec2I != Vector2Int.zero) { //! Regular movement
            Log("regular movement anim", gameObject);
            coord += currentForceVec2I;

            OnAnimationStarted?.Invoke();
            moveTween = transform.DOMove(gridRef.GetWorldSpaceFromCoord(coord), GameSettings.Instance.gameTickInSeconds)
                .SetEase(Ease.Linear)
                .OnComplete(() => { OnAnimationCompleted?.Invoke(); });
        }
        else if ((!frozen && lastForces.YLocked()) || //! Squishage
                 (lastForces.XLocked() && lastForces.XLocked() != lastForces.YLocked())) {
            Log("triggering fall back animation", gameObject);
            var bumpTargetPos =
                (gridRef.GetWorldSpaceFromCoord(coord) + (Vector3Int)lastForces.firstDir - transform.position) * .15f -
                Vector3.back * coord.y * .01f;

            moveTween = transform.DOMove(bumpTargetPos, .15f).SetRelative().SetLoops(2, LoopType.Yoyo)
                .OnComplete(() => { OnAnimationCompleted?.Invoke(); });

            moveTween.Play();
        }
        else { //! Regular block animation
            Log($"{gameObject.name} blocked bump anim");
            var bumpTargetPos =
                (gridRef.GetWorldSpaceFromCoord(coord) + (Vector3Int)currentForceVec2I - transform.position) * .15f -
                Vector3.back * coord.y * .01f;

            moveTween = transform.DOMove(bumpTargetPos, .15f).SetRelative().SetLoops(2, LoopType.Yoyo)
                .OnComplete(() => { OnAnimationCompleted?.Invoke(); });

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
        // === Main Movement Indicator ===
        var mainIndicatorTransform = moveIntentionVisual.transform;
        var mainIndicatorSprite = moveIntentionVisual; // Assuming SpriteRenderer or similar

        // Call the NEW visual-only method.
        Vector2Int currentVisualIntent = GetVisualMovementIntention();

        // The logic now only cares if the block has a programmed move, regardless of freeze state.
        if (currentVisualIntent == Vector2Int.zero) {
            mainIndicatorSprite.enabled = false;
        }
        else {
            mainIndicatorSprite.enabled = true;
            mainIndicatorTransform.up = (Vector3Int)currentVisualIntent;
        }

        // === Next Movement Indicator ===
        var nextIndicatorTransform = littleDirTriangle.transform;
        var nextIndicatorSprite = littleDirTriangle;

        // Call the NEW visual-only "peek" method.
        Vector2Int nextVisualIntent = PeekNextVisualMovementIntention();

        if (nextVisualIntent == Vector2Int.zero) {
            nextIndicatorSprite.enabled = false;
        }
        else {
            nextIndicatorSprite.enabled = true;
            nextIndicatorTransform.up = (Vector3Int)nextVisualIntent;
        }
    }

    // In BlockBehaviour.cs

    /// <summary>
    /// Calculates the movement intention purely for VISUAL purposes, ignoring the frozen state.
    /// The BlockCoordinator should NEVER call this.
    /// </summary>
    /// <returns>The direction the block would move if it were not frozen.</returns>
    public Vector2Int GetVisualMovementIntention() {
        // NOTICE: The 'if (frozen)' check is intentionally removed from this version.
        Direction moveDir;
        moveDir = movePath != null && movePath.Length > 0 ? movePath[moveIdx] : Direction.wait;

        switch (moveMode) {
            case BlockMoveState.still:
                moveDir = Direction.wait;
                break;
            case BlockMoveState.pingpong:
                moveDir = pingpongIsForward ? moveDir : GetOppositeDir(moveDir);
                break;
        }

        return (Vector2Int)DirToVec3Int(moveDir);
    }

    /// <summary>
    /// Peeks at the next movement intention purely for VISUAL purposes, ignoring the frozen state.
    /// </summary>
    public Vector2Int PeekNextVisualMovementIntention() {
        // This method simulates a step forward to see the next visual.
        var holdIdx = moveIdx;
        var holdForward = pingpongIsForward;

        AdvanceMoveIdx();

        // It calls our new visual method, not the physics one.
        var moveIntent = GetVisualMovementIntention();

        // Restore the state so this method has no side effects.
        moveIdx = holdIdx;
        pingpongIsForward = holdForward;

        return moveIntent;
    }

    private Vector3Int DirToVec3Int(Direction dir) {
        var dirVec = Vector3Int.zero;
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

    public void TrySetFreeze(bool? freezeState = null) {
        if (!canBeFrozen) return;


        // Check if state is actually changing before updatgn
        bool requestedState = freezeState ?? !frozen;
        if (requestedState == frozen) return;

        frozen = requestedState;
        // blocked = frozen; // Dont do this anymore because blocks new pushableWhenFrozen variable


        if (frozen)
            cubeRenderer.material.SetFloat("_BlendFactor", 1f);
        else
            cubeRenderer.material.SetFloat("_BlendFactor", 0f);

        // if (moveMode == BlockMoveState.pingpong && GetComponent<BlockKey>() == null)
        //     cubeRenderer.material = frozen ? gridRef.frozenMAT : gridRef.pingpongMAT;
        // else
        //     cubeRenderer.material = frozen ? gridRef.frozenMAT : blockMAT;

        OnFreezeBlock?.Invoke();

        UpdateMovementVisualiser();
    }
}