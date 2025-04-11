using UnityEditor;
using UnityEngine;
using Sirenix.OdinInspector;
using Ami.BroAudio;
using Unity.VisualScripting;
using System.Collections;
using DG.Tweening;
using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

public class BlockCoordinator : UnityUtils.Singleton<BlockCoordinator> {
    public static BlockCoordinator Coordinator => coordinator;
    private static BlockCoordinator coordinator;


    [SerializeField] private bool startLevelPaused = true;


    [Header("References")]
    [SerializeField]
    private BlockGrid gridRef;
    public BlockGrid GridRef => gridRef;

    [Header("Audio")]
    [SerializeField]
    private SoundID bellSoundSFX;

    /// <summary>
    /// Class to hold the forces acting upon a grid cell, stored as 4 booleans, one for each direction. 
    /// Forces are stored as booleans so that forces to do not prematurely cancel out with Vector Math and create unpredicatable outcomes.
    /// </summary>
    [System.Serializable]
    public class CellForce {
        public bool
            up, down, left, right;

        public Vector2Int firstDir = Vector2Int.zero;

        public Vector2Int QueryForce() {
            Vector2Int finalForce = Vector2Int.zero;

            if (up) finalForce += Vector2Int.up;
            if (down) finalForce += Vector2Int.down;
            if (left) finalForce += Vector2Int.left;
            if (right) finalForce += Vector2Int.right;

            return finalForce;
        }

        public CellForce() {

        }

        public CellForce(Vector2Int inVec2Int) {
            this.SetForceFromVector2Int(inVec2Int);
        }

        public CellForce(CellForce original) {
            SetForceFromVector2Int(original.QueryForce());
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
                firstDir = this.QueryForce();
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

    public CellForce[,] forceGrid;

    #region History Stack


    [ShowInInspector, ReadOnly]
    private Stack<BlockGridHistory> undoStack = new Stack<BlockGridHistory>();






    [Button]
    public void StepForwardWithUndo() {
        undoStack.Push(new BlockGridHistory(gridRef.ActiveGridState.BlocksList));
        StepForwardOnce();
    }

    [Button]
    public void UndoLastStep() {
        if (undoStack.Count == 0) {
            LogWarning("Undo stack is empty!");
            return;
        }

        var snapshot = undoStack.Pop();
        foreach (var snap in snapshot.blockSnapshots) {
            if (snap.block != null) {
                snap.ApplyUndo();
            }
            else {
                LogError("Snapshot block reference missing!");
            }
        }

        // Rebuild state grid
        gridRef.ActiveGridState.ClearBlockStateGrid();
        gridRef.ActiveGridState.UpdateCoordList();
    }





    #endregion












    private bool isStepping = false;
    public event Action OnStepForward;

    public bool StepForwardOnce() {
        if (isStepping) return false;

        isStepping = true;
        IterateBlockMovement();
        DOVirtual.DelayedCall(GameSettings.Instance.gameTickInSeconds, () => isStepping = false);
        OnStepForward?.Invoke();
        return true;
    }

    protected override void Awake() {
        base.Awake();
        if (coordinator == null)
            coordinator = this;
        else Destroy(this);

        if (gridRef == null)
            gridRef = GetComponent<BlockGrid>();
    }

    private void Start() {
        DOVirtual.DelayedCall(2.05f, () => { if (!isPaused) bellSoundSFX.Play(); });
    }


    public void ManualStart() {
        forceGrid = new CellForce[gridRef.LevelData.GridSize.x, gridRef.LevelData.GridSize.y];
        InitilizeEmptyForceGrid();

        isPaused = true;

        if (!startLevelPaused) Invoke(nameof(TogglePauseResume), 2f);
        //DOVirtual.DelayedCall(2f, () => bellSoundSFX.Play());

        // Invoke(nameof(RingBell), 1.4f);

        // DOVirtual.DelayedCall(delay: 2, () => StartGameTickLoop());
    }



    // private void OnEnable() {
    //     TogglePauseResume();
    // }


    private Coroutine gameTickCoroutine;
    public event Action OnGameTickStarted;
    private void StartGameTickLoop() {
        if (gameTickCoroutine != null)
            StopCoroutine(gameTickCoroutine);

        gameTickCoroutine = StartCoroutine(GameTickLoop());
    }

    private IEnumerator GameTickLoop() {
        while (true) {
            while (isPaused) {
                yield return null;
            }

            OnGameTickStarted?.Invoke();
            IterateBlockMovement();

            yield return new WaitForSeconds(GameSettings.Instance.gameTickInSeconds);
        }
    }


    private bool isPaused = true;
    public bool IsPaused => isPaused;
    public event Action<bool> OnPauseToggled;



    [Button]
    public void TogglePauseResume() {
        if (isPaused) {
            StartGameTickLoop();
            //bellSoundSFX.Play();
        }

        Log(isPaused ? "Resuming Autoplay" : "Pause at next tick");

        isPaused = !isPaused;
        OnPauseToggled?.Invoke(isPaused);
    }




    private void RingBell() {
        bellSoundSFX.Play();
    }

    /// <summary>
    /// the main loop of the force system, steps one iteration forward and then begins the animations.
    /// </summary>
    public void IterateBlockMovement() {
        InitilizeEmptyForceGrid();

        gridRef.ActiveGridState.UpdateCoordList();

        AddInitialForcesToForceGrid();
        //ReadForcesOnForceGrid();

        int timeout = 0;
        while (ReadForcesOnForceGrid() && timeout <= 10) {
            AddDerivedForcesToForceGrid();
            timeout++;
            if (timeout >= 10)
                LogError("force caluclation infinite loop");
        }
        Log($"grid force iteration looped {timeout} times");

        timeout = 0;
        int longerGridDimension = Mathf.Max(forceGrid.GetLength(0), forceGrid.GetLength(1));

        CheckBlockedBlocks();
        while (CheckBlockedBlocks() && timeout <= longerGridDimension) {
            timeout++;
        }
        Log($"grid blocked check iteration looped {timeout} times");

        gridRef.ActiveGridState.ClearBlockStateGrid();
        foreach (BlockBehaviour b in gridRef.ActiveGridState.BlocksList) {
            b.Move();
            //add back onto gridblockstate
            gridRef.ActiveGridState.GridBlockStates[b.coord.x, b.coord.y] = b;
            Log($"moving {b.gameObject.name}");
        }

        gridRef.ActiveGridState.UpdateCoordList();
        Log("Full block grid iteration");
    }



    [Button]
    private void InitilizeEmptyForceGrid() {
        forceGrid = new CellForce[gridRef.LevelData.GridSize.x, gridRef.LevelData.GridSize.y];

        for (int x = 0; x < forceGrid.GetLength(0); x++) {
            for (int y = 0; y < forceGrid.GetLength(1); y++) {
                forceGrid[x, y] = new CellForce();
            }
        }
    }

    public void AddInitialForcesToForceGrid() {
        foreach (BlockBehaviour b in gridRef.ActiveGridState.BlocksList) {
            gridRef.ActiveGridState.GridBlockStates[b.coord.x, b.coord.y] = b;
            if (b.frozen) continue;


            Vector2Int moveIntent = b.GetMovementIntention();
            Vector2Int targetCell = b.coord + moveIntent;

            b.lastForces = new CellForce();
            b.lastForces.SetForceFromVector2Int(moveIntent);

            //block target cell isnt on grid (at edge)
            if (!gridRef.isValidGridCoord(targetCell)) {
                continue;
            }
            Log($"{b.name} just set force to {b.lastForces.QueryForce()} at {targetCell} from moveIntent {b.GetMovementIntention()}");

            forceGrid[targetCell.x, targetCell.y].AddForceFromCell(b.lastForces);
        }
    }

    public void AddDerivedForcesToForceGrid() {
        foreach (BlockBehaviour b in gridRef.ActiveGridState.BlocksList) {
            if (b.frozen) continue;

            Vector2Int collapsedForce = b.lastForces.QueryForce();
            Vector2Int targetCell = b.coord + collapsedForce;

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
        bool changes = false;

        foreach (BlockBehaviour b in gridRef.ActiveGridState.BlocksList) {
            if (b.frozen) continue;

            Vector2Int moveIntent = b.lastForces.QueryForce();
            Vector2Int targetCell = b.coord + moveIntent;

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

    public bool CheckBlockedBlocks() {
        bool changes = false;

        foreach (BlockBehaviour b in gridRef.ActiveGridState.BlocksList) {
            if (b.frozen) continue;
            if (b.blocked || b.lastForces.NoInputs()) continue;

            Vector2Int moveIntent = b.lastForces.QueryForce();
            if (moveIntent == Vector2Int.zero) continue;
            Vector2Int targetCell = b.coord + moveIntent;

            //progressive checks for different block conditions
            //if target cell is on grid
            if (!gridRef.isValidGridCoord(targetCell)) {
                b.blocked = true;
                Log($"{b.name} is blocked, target cell {targetCell} is not valid");
                //b.lastForces = new CellForce();
            }
            //if recieving forces in all directions
            else if (b.lastForces.AllInputs()) {
                b.blocked = true;
                Log($"{b.name} is blocked, recieving all forces, deadlocked");
            }
            //if target cell is occupied and that block is blocked or cant be pushed
            else {
                var otherB = gridRef.QueryGridCoordBlockState(targetCell);

                if (otherB == null) {

                }
                else if (otherB.blocked || otherB.lastForces.AllInputs()) {
                    b.blocked = true;
                }
                else {
                    //check if block at target cell can move out your way
                    var otherForce = otherB.lastForces.QueryForce();

                    bool xBlocked = otherB.lastForces.XLocked() || otherForce.x != moveIntent.x;
                    bool yBlocked = otherB.lastForces.YLocked() || otherForce.y != moveIntent.y;

                    if (moveIntent.x != 0 && xBlocked || moveIntent.y != 0 && yBlocked) {
                        b.blocked = true;
                    }
                }

                //check extra for diagonals
                //TODO
                //if (!b.blocked && moveIntent.x != 0 && moveIntent.y != 0) {  //diagonal
                var xBlock = gridRef.QueryGridCoordBlockState(b.coord + new Vector2Int(moveIntent.x, 0));
                Log($"block {b.name} queried {b.coord + new Vector2Int(moveIntent.x, 0)} and found {xBlock?.gameObject.name}");
                var yBlock = gridRef.QueryGridCoordBlockState(b.coord + new Vector2Int(0, moveIntent.y));

                bool blockedOnX = false;
                if (xBlock != null && moveIntent.x != 0)
                    blockedOnX = xBlock.blocked || xBlock.lastForces.XLocked() || moveIntent.x != xBlock.lastForces.QueryForce().x;
                bool blockedOnY = false;
                if (yBlock != null && moveIntent.y != 0)
                    blockedOnY = yBlock.blocked || yBlock.lastForces.YLocked() || moveIntent.y != yBlock.lastForces.QueryForce().y;

                if (blockedOnX || blockedOnY) b.blocked = true;
                //}
            }

            //if now blocked, update other variables
            if (b.blocked) {
                changes = true;
                //b.lastForces = new CellForce();
            }
        }

        return changes;
    }

    #region debug
#if UNITY_EDITOR

    private void OnDrawGizmos() {
        gridRef.ForEachCellAtCellCenter((Vector2Int coord, Vector3 pos) => DrawDirectionalSquares(coord, pos));
    }

    /// <summary>
    /// Draws all Forces a grid coord is recieving as gizmo
    /// </summary>
    /// <param name="coord"></param>
    /// <param name="center"></param>
    public void DrawDirectionalSquares(Vector2Int coord, Vector3 center) {
        // Half the length of the square (0.5f makes total size 1 unit)
        float dist = 0.25f;
        float size = .2f;

        //get cell force
        CellForce cellForce = forceGrid != null ? forceGrid[coord.x, coord.y] : new CellForce();

        // Default color (grey)
        Color defaultColor = Color.grey;
        Color highlightColor = Color.blue;

        Handles.Label(center, $"{coord}", new GUIStyle() { alignment = TextAnchor.MiddleCenter });

        // Up square
        Gizmos.color = cellForce.up ? highlightColor : defaultColor;
        Gizmos.DrawWireCube(center + Vector3.up * dist, Vector3.one * size);

        // Left square
        Gizmos.color = cellForce.left ? highlightColor : defaultColor;
        Gizmos.DrawWireCube(center + Vector3.left * dist, Vector3.one * size);

        // Right square
        Gizmos.color = cellForce.right ? highlightColor : defaultColor;
        Gizmos.DrawWireCube(center + Vector3.right * dist, Vector3.one * size);

        // Down square
        Gizmos.color = cellForce.down ? highlightColor : defaultColor;
        Gizmos.DrawWireCube(center + Vector3.down * dist, Vector3.one * size);
    }
#endif
    #endregion


}
