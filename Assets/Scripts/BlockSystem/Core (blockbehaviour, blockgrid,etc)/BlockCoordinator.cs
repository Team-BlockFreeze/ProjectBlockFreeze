using System;
using System.Collections;
using System.Collections.Generic;
using Ami.BroAudio;
using DG.Tweening;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class BlockCoordinator : UnityUtils.Singleton<BlockCoordinator> {
    [SerializeField] private bool startLevelPaused = true;



    [Header("References")]
    [SerializeField]
    private BlockGrid gridRef;

    [Header("Audio")][SerializeField] private SoundID bellSoundSFX;


    private bool finishGameLoop;

    public CellForce[,] forceGrid;

    private Coroutine gameTickCoroutine;


    private bool isStepping;
    public static BlockCoordinator Coordinator { get; private set; }

    public BlockGrid GridRef => gridRef;
    public bool IsPaused { get; private set; } = true;

    protected override void Awake() {
        base.Awake();
        if (Coordinator == null)
            Coordinator = this;
        else Destroy(this);

        if (gridRef == null)
            gridRef = GetComponent<BlockGrid>();
    }

    private void Start() {
        DOVirtual.DelayedCall(2.05f, () => {
            if (!IsPaused) bellSoundSFX.Play();
        });
    }


    // private void OnEnable() {
    //     TogglePauseResume();
    // }




    private void OnTriggerEnter(Collider other) {
    }

    public event Action OnStepForward;

    public bool StepForwardOnce() {
        if (isStepping) return false;

        isStepping = true;
        IterateBlockMovement();
        DOVirtual.DelayedCall(GameSettings.Instance.gameTickInSeconds, () => isStepping = false);
        OnStepForward?.Invoke();
        return true;
    }


    public void ManualStart() {
        forceGrid = new CellForce[gridRef.LevelData.GridSize.x, gridRef.LevelData.GridSize.y];
        InitilizeEmptyForceGrid();

        IsPaused = true;

        if (!startLevelPaused) Invoke(nameof(TogglePauseResume), 2f);
        //DOVirtual.DelayedCall(2f, () => bellSoundSFX.Play());

        // Invoke(nameof(RingBell), 1.4f);

        // DOVirtual.DelayedCall(delay: 2, () => StartGameTickLoop());
    }

    public event Action OnGameTickStarted;

    private void StartGameTickLoop() {
        if (gameTickCoroutine != null)
            StopCoroutine(gameTickCoroutine);

        gameTickCoroutine = StartCoroutine(GameTickLoop());
    }


    private void RaycastToPoint(Transform transform) {
        var ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(ray, out var hit)) {
            Debug.DrawLine(ray.origin, hit.point, Color.red);
            Debug.Log($"Hit object: {hit.collider.gameObject.name} at distance: {hit.distance}");
        }
    }


    private IEnumerator GameTickLoop() {
        while (true) {
            if (finishGameLoop)
                yield break;

            while (IsPaused) yield return null;

            OnGameTickStarted?.Invoke();
            IterateBlockMovement();

            yield return new WaitForSeconds(GameSettings.Instance.gameTickInSeconds);
        }
    }

    public event Action<bool> OnPauseToggled;


    public void SetAutoplay(bool isPaused) {
        GameObject.FindGameObjectWithTag("PausePlayButtons").GetComponent<SetTimeScale>().SetAutoplay(isPaused);
    }

    public void TogglePauseResume() {
        if (IsPaused) StartGameTickLoop();
        //bellSoundSFX.Play();
        Log(IsPaused ? "Resuming Autoplay" : "Pause at next tick");

        IsPaused = !IsPaused;
        OnPauseToggled?.Invoke(IsPaused);
    }


    private void RingBell() {
        bellSoundSFX.Play();
    }

    #region Tile Effect System

    // A list to keep track of all special tiles in the level.
    private readonly List<ITileEffect> tileEffects = new List<ITileEffect>();

    public void RegisterTileEffect(ITileEffect tileEffect) {
        if (!tileEffects.Contains(tileEffect)) {
            tileEffects.Add(tileEffect);
        }
    }

    public void UnregisterTileEffect(ITileEffect tileEffect) {
        if (tileEffects.Contains(tileEffect)) {
            tileEffects.Remove(tileEffect);
        }
    }

    public void ProcessTileEffectsAfterTeleport() {
        Log("Re-processing tile effects after a teleport event.");
        ProcessTileEffects();
    }

    /// <summary>
    /// This method will check all tile effects after a move.
    /// </summary>
    private void ProcessTileEffects() {
        if (undoStack.Count == 0) return;

        var previousGridState = undoStack.Peek();

        foreach (var effect in tileEffects) {
            var tileCoord = effect.TileBlock.coord;
            var effectBlock = effect.TileBlock; // The block that IS the tile effect.

            List<BlockBehaviour> currentBlocksOnTile = gridRef.QueryGridCoordForAllBlocks(tileCoord);
            List<BlockBehaviour> previousBlocksOnTile = previousGridState.GetBlocksAtCoord(tileCoord);

            // OnBlockEnter ---
            foreach (var currentBlock in currentBlocksOnTile) {
                if (currentBlock == effectBlock) continue;

                if (!previousBlocksOnTile.Contains(currentBlock)) {
                    Log($"Block entered tile {effectBlock.name}: {currentBlock.name}");
                    effect.OnBlockEnter(currentBlock);
                }
            }

            // OnBlockExit ---
            foreach (var previousBlock in previousBlocksOnTile) {
                if (previousBlock == effectBlock) continue;

                if (!currentBlocksOnTile.Contains(previousBlock)) {
                    Log($"Block exited tile {effectBlock.name}: {previousBlock.name}");
                    effect.OnBlockExit(previousBlock);
                }
            }
        }
    }

    #endregion

    #region Game Tick Logic

    /// <summary>
    /// 1. Save the current grid state to the undo stack.
    /// 2. Initialize a new, empty `forceGrid`.
    /// 3. Add initial forces based on each block's inherent movement intention.
    /// 4. Iteratively propagate forces across the grid until the system is stable (no new forces are being created).
    /// 5. Iteratively check for and flag any blocks that are blocked by walls or other blocks.
    /// 6. Command each un-blocked block to execute its calculated move.
    /// 7. Process tile effects.
    /// </summary>
    public void IterateBlockMovement() {
        PushGridStateToStack();

        InitilizeEmptyForceGrid();

        gridRef.ActiveGridState.UpdateCoordList();

        AddInitialForcesToForceGrid();
        //ReadForcesOnForceGrid();

        var timeout = 0;
        while (ReadForcesOnForceGrid() && timeout <= 10) {
            AddDerivedForcesToForceGrid();
            timeout++;
            if (timeout >= 10)
                LogError("force caluclation infinite loop");
        }

        //! Initialize the current tick's blocked state to false. // Previously was done in CheckBlockedBlocks -Kerry
        foreach (var b in gridRef.ActiveGridState.BlocksList) {
            b.blocked = false;
        }

        Log($"grid force iteration looped {timeout} times");

        timeout = 0;
        var longerGridDimension = Mathf.Max(forceGrid.GetLength(0), forceGrid.GetLength(1));

        CheckBlockedBlocks();
        while (CheckBlockedBlocks() && timeout <= longerGridDimension) timeout++;
        Log($"grid blocked check iteration looped {timeout} times");

        gridRef.ActiveGridState.ClearBlockStateGrid();
        foreach (var b in gridRef.ActiveGridState.BlocksList) {
            b.Move();
            //add back onto gridblockstate
            gridRef.ActiveGridState.GridBlockStates[b.coord.x, b.coord.y] = b;
            Log($"moving {b.gameObject.name}");
        }

        gridRef.ActiveGridState.UpdateCoordList();


        //! Process tile effects after the move
        ProcessTileEffects();


        Log("Full block grid iteration");
    }


    [Button]
    private void InitilizeEmptyForceGrid() {
        forceGrid = new CellForce[gridRef.LevelData.GridSize.x, gridRef.LevelData.GridSize.y];

        for (var x = 0; x < forceGrid.GetLength(0); x++)
            for (var y = 0; y < forceGrid.GetLength(1); y++)
                forceGrid[x, y] = new CellForce();
    }

    public void AddForceToBlock(BlockBehaviour block, Vector2Int force) {
        if (!gridRef.isValidGridCoord(block.coord)) return;
        forceGrid[block.coord.x, block.coord.y].AddForceFromCell(new CellForce(force));
    }

    public void AddInitialForcesToForceGrid() {
        foreach (var b in gridRef.ActiveGridState.BlocksList) {
            gridRef.ActiveGridState.GridBlockStates[b.coord.x, b.coord.y] = b;
            if (b.frozen && !b.pushableWhenFrozen) continue;
            if (b.phaseThrough) continue;

            var moveIntent = b.GetMovementIntention();
            var targetCell = b.coord + moveIntent;

            b.lastForces = new CellForce();
            b.lastForces.SetForceFromVector2Int(moveIntent);

            //block target cell isnt on grid (at edge)
            if (!gridRef.isValidGridCoord(targetCell)) continue;
            Log(
                $"{b.name} just set force to {b.lastForces.QueryForce()} at {targetCell} from moveIntent {b.GetMovementIntention()}");

            forceGrid[targetCell.x, targetCell.y].AddForceFromCell(b.lastForces);
        }
    }

    public void AddDerivedForcesToForceGrid() {
        foreach (var b in gridRef.ActiveGridState.BlocksList) {
            if (b.frozen && !b.pushableWhenFrozen) continue;
            if (b.phaseThrough) continue;

            var collapsedForce = b.lastForces.QueryForce();
            var targetCell = b.coord + collapsedForce;

            //block target cell isnt on grid (at edge)
            if (!gridRef.isValidGridCoord(targetCell) || targetCell == b.coord) {
                Log($"{gameObject.name} can't add force to grid, target cell out of bounds");
                continue;
            }

            forceGrid[targetCell.x, targetCell.y].AddForceFromCell(new CellForce(collapsedForce));
            Log($"{gameObject.name} adding force {collapsedForce} to grid at {targetCell}");
            //diagonal force
            if (collapsedForce.x != 0 && collapsedForce.y != 0) {
                var xTarget = b.coord + new Vector2Int(collapsedForce.x, 0);
                forceGrid[xTarget.x, xTarget.y].AddForceFromCell(new CellForce(new Vector2Int(collapsedForce.x, 0)));
                var yTarget = b.coord + new Vector2Int(0, collapsedForce.y);
                forceGrid[yTarget.x, yTarget.y].AddForceFromCell(new CellForce(new Vector2Int(0, collapsedForce.y)));
                Log($"{gameObject} is moving diagonally, adding orthogonal forces at {xTarget} and {yTarget}");
            }
        }
    }

    public bool ReadForcesOnForceGrid() {
        var changes = false;

        foreach (var b in gridRef.ActiveGridState.BlocksList) {
            if (b.frozen && !b.pushableWhenFrozen) continue;
            if (b.phaseThrough) continue;

            var moveIntent = b.lastForces.QueryForce();
            var targetCell = b.coord + moveIntent;

            var forces = new CellForce(b.lastForces);

            //add force from target cell
            //target cell is added first because otherwise other forces would change it before it could read them
            if (!gridRef.isValidGridCoord(targetCell)) {
                //LogWarning($"tried to read forces from invalid cell {targetCell}");
            }
            else {
                forces.AddForceFromCell(forceGrid[targetCell.x, targetCell.y]);
            }

            //add force on current cell
            forces.AddForceFromCell(forceGrid[b.coord.x, b.coord.y]);

            if (!b.lastForces.Equals(forces))
                changes = true;

            b.lastForces.AddForceFromCell(forces);
        }

        return changes;
    }

    #endregion

    #region Blocked block logic
    public bool CheckBlockedBlocks() {
        var changes = false;

        foreach (var b in gridRef.ActiveGridState.BlocksList) {
            bool isCurrentlyBlocked = false;


            // b.blocked = false; //! Moved to main loop -Kerry

            // Skip blocks that shouldn't be checked.

            if (b.blockType == "wall") continue;


            //! Never skip any assumed blocked blocks anymore
            // if (b.blocked || (b.frozen && !b.pushableWhenFrozen) || b.lastForces.NoInputs()) {
            //     continue;
            // }

            if (b.frozen && !b.pushableWhenFrozen) {
                b.blocked = true;
                isCurrentlyBlocked = true;
            }

            var moveIntent = b.lastForces.QueryForce();
            if (moveIntent == Vector2Int.zero) continue;
            var targetCell = b.coord + moveIntent;


            // 1. Is the target cell occupied?
            var otherB = gridRef.QueryGridCoordBlockState(targetCell);

            // Debug.Log(otherB?.name);


            // 2. If occupied, hceck if it's phaseTrhgouh block. --> If it is, ignore it.
            if (otherB != null && otherB.phaseThrough) {
                continue;
            }
            if (otherB != null && otherB.blocked) {
                isCurrentlyBlocked = true;
            }

            // 3. If !phaseThrough, check other conditions
            if (!gridRef.isValidGridCoord(targetCell)) {
                isCurrentlyBlocked = true; // Blocked by grid bounds
            }
            else if (b.lastForces.AllInputs()) {
                isCurrentlyBlocked = true; // Blocked by all forces deadlock
            }
            else if (otherB != null && (otherB.blocked || otherB.lastForces.AllInputs())) {
                isCurrentlyBlocked = true; // Blocked by immovable obejct
            }
            else {
                // Check for squishages  eg. (left) --> (block) <-- (right)
                if (otherB != null) {
                    var otherForce = otherB.lastForces.QueryForce();
                    var xBlocked = otherB.lastForces.XLocked() || otherForce.x != moveIntent.x;
                    var yBlocked = otherB.lastForces.YLocked() || otherForce.y != moveIntent.y;
                    if ((moveIntent.x != 0 && xBlocked) || (moveIntent.y != 0 && yBlocked)) {
                        isCurrentlyBlocked = true;
                    }
                }

                // Check for diagonial obstruction
                if (!isCurrentlyBlocked && moveIntent.x != 0 && moveIntent.y != 0) {
                    var xBlock = gridRef.QueryGridCoordBlockState(b.coord + new Vector2Int(moveIntent.x, 0));
                    var yBlock = gridRef.QueryGridCoordBlockState(b.coord + new Vector2Int(0, moveIntent.y));

                    // Check if adjacent blocks block diagonal movement (also checks for phaseThrough)
                    var blockedOnX = xBlock != null && !xBlock.phaseThrough && (xBlock.blocked || xBlock.lastForces.XLocked() || moveIntent.x != xBlock.lastForces.QueryForce().x);
                    var blockedOnY = yBlock != null && !yBlock.phaseThrough && (yBlock.blocked || yBlock.lastForces.YLocked() || moveIntent.y != yBlock.lastForces.QueryForce().y);

                    if (blockedOnX || blockedOnY) {
                        isCurrentlyBlocked = true;
                    }
                }
            }

            // 4. Final touches

            if (isCurrentlyBlocked) {
                // Debug.Log($"Block {b.name} is currently blocked");
            }

            if (isCurrentlyBlocked) {
                b.blocked = true;
                changes = true;
            }
        }

        return changes;
    }

    #endregion

    #region CellForce Class

    /// <summary>
    ///     Class to hold the forces acting upon a grid cell, stored as 4 booleans, one for each direction.
    ///     Forces are stored as booleans so that forces to do not prematurely cancel out with Vector Math and create
    ///     unpredicatable outcomes.
    /// </summary>
    [Serializable]
    public class CellForce {
        public bool
            up, down, left, right;

        public Vector2Int firstDir = Vector2Int.zero;

        public CellForce() {
        }

        public CellForce(Vector2Int inVec2Int) {
            SetForceFromVector2Int(inVec2Int);
        }

        public CellForce(CellForce original) {
            SetForceFromVector2Int(original.QueryForce());
        }

        public Vector2Int QueryForce() {
            var finalForce = Vector2Int.zero;

            if (up) finalForce += Vector2Int.up;
            if (down) finalForce += Vector2Int.down;
            if (left) finalForce += Vector2Int.left;
            if (right) finalForce += Vector2Int.right;

            return finalForce;
        }

        public void SetForceFromVector2Int(Vector2Int inForceVec2) {
            right = inForceVec2.x > 0;
            left = inForceVec2.x < 0;
            up = inForceVec2.y > 0;
            down = inForceVec2.y < 0;
        }

        public void AddForceFromCell(CellForce otherCell) {
            up = up || otherCell.up;
            down = down || otherCell.down;
            left = left || otherCell.left;
            right = right || otherCell.right;

            if (firstDir == Vector2.zero)
                firstDir = QueryForce();
        }

        public bool Equals(CellForce otherCell) {
            if (up == otherCell.up &&
                down == otherCell.down &&
                left == otherCell.left &&
                right == otherCell.right)
                return true;
            return false;
        }

        public bool NoInputs() {
            return !up && !down && !left && !right;
        }

        public bool AllInputs() {
            return up && down && left && right;
        }

        public bool XLocked() {
            return left && right;
        }

        public bool YLocked() {
            return up && down;
        }

        public override string ToString() {
            return $"CellForce: (Up: {up}, Down: {down}, Left: {left}, Right: {right})";
        }
    }
    #endregion

    #region History Stack

    [ShowInInspector][ReadOnly] private Stack<BlockGridHistory> undoStack = new();


    public void ClearUndoStack() {
        undoStack.Clear();
    }


    private void PushGridStateToStack() {
        undoStack.Push(new BlockGridHistory(gridRef.ActiveGridState.BlocksList));
    }

    public bool StepForwardWithUndo() {
        if (isStepping) return false;


        isStepping = true;
        IterateBlockMovement();
        DOVirtual.DelayedCall(GameSettings.Instance.gameTickInSeconds, () => isStepping = false);
        OnStepForward?.Invoke();
        return true;
    }

    public void UndoLastStep() {
        if (undoStack.Count == 0) {
            LogWarning("Undo stack is empty!");
            return;
        }

        var snapshot = undoStack.Pop();
        foreach (var snap in snapshot.blockSnapshots)
            if (snap.block != null)
                snap.ApplyUndo();
            else
                LogError("Snapshot block reference missing!");

        // Rebuild state grid
        gridRef.ActiveGridState.ClearBlockStateGrid();
        gridRef.ActiveGridState.UpdateCoordList();
    }

    #endregion

    #region debug

#if UNITY_EDITOR

    private void OnDrawGizmos() {
        gridRef.ForEachCellAtCellCenter((coord, pos) => DrawDirectionalSquares(coord, pos));
    }

    /// <summary>
    ///     Draws all Forces a grid coord is recieving as gizmo
    /// </summary>
    /// <param name="coord"></param>
    /// <param name="center"></param>
    public void DrawDirectionalSquares(Vector2Int coord, Vector3 center) {
        // Increased distance and size for more visibility
        var dist = 0.35f;
        var size = .27f;

        //get cell force
        var cellForce = forceGrid != null ? forceGrid[coord.x, coord.y] : new CellForce();

        // More contrasting colors
        var defaultColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);  // Darker grey
        var highlightColor = new Color(0.2f, 0.4f, 1f, 1f);    // Brighter blue

        // Larger, bold coordinate label
        var style = new GUIStyle {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12,
            fontStyle = FontStyle.Bold
        };
        Handles.Label(center, $"{coord}", style);

        // Draw filled cubes with wire outlines for better visibility
        void DrawEnhancedCube(Vector3 pos, bool isActive) {
            Gizmos.color = isActive ? highlightColor : defaultColor;
            Gizmos.DrawCube(pos, Vector3.one * size * 0.8f);
            Gizmos.color = isActive ? Color.white : Color.grey;
            Gizmos.DrawWireCube(pos, Vector3.one * size);
        }

        // Draw directional indicators
        DrawEnhancedCube(center + Vector3.up * dist, cellForce.up);
        DrawEnhancedCube(center + Vector3.left * dist, cellForce.left);
        DrawEnhancedCube(center + Vector3.right * dist, cellForce.right);
        DrawEnhancedCube(center + Vector3.down * dist, cellForce.down);
    }
#endif

    #endregion
}