using UnityEngine;
using UnityEditor;

public class BlockCoordinator : MonoBehaviour
{
    [SerializeField]
    private BlockGrid gridRef;

    [System.Serializable]
    public class CellForce
    {
        public bool
            up, down, left, right;

        Vector2Int finalForce = Vector2Int.zero;

        public Vector2Int QueryForce()
        {
            finalForce = Vector2Int.zero;

            if (up) finalForce += Vector2Int.up;
            if (down) finalForce += Vector2Int.down;
            if (left) finalForce += Vector2Int.left;
            if (right) finalForce += Vector2Int.right;

            return finalForce;
        }

        public CellForce()
        {

        }

        public CellForce(CellForce original)
        {
            up = original.up;
            down = original.down;
            left = original.left;
            right = original.right;
        }

        public void SetForceFromVector2Int(Vector2Int inForceVec2)
        {
            if (inForceVec2.x != 0)
                _ = inForceVec2.x > 0 ? right=true : left=true ;
            if (inForceVec2.y != 0)
                _ = inForceVec2.y > 0 ? up = true : down = true;
        }

        public void AddForceFromCell(CellForce otherCell)
        {
            up = up || otherCell.up;
            down = down || otherCell.down;
            left = left || otherCell.left;
            right = right || otherCell.right;
        }

        public bool Equals(CellForce otherCell)
        {
            if (up == otherCell.up && down == otherCell.down && 
                left == otherCell.left && right == otherCell.right)
                return true;
            return false;
        }

        public bool NoInputs()
        {
            return !up && !down && !left && !right;
        }
    }

    public CellForce[,] forceGrid;

    private void Awake()
    {
        if (gridRef == null)
            gridRef = GetComponent<BlockGrid>();
    }

    private void Start()
    {
        forceGrid = new CellForce[gridRef.StartGridState.GridSize.x, gridRef.StartGridState.GridSize.y];
        InitilizeEmptyForceGrid();

        InvokeRepeating(nameof(IterateBlockMovement), 1f, 1f);
    }

    public void IterateBlockMovement()
    {
        InitilizeEmptyForceGrid();

        AddInitialForcesToForceGrid();
        ReadForcesOnForceGrid();

        int timeout = 0;
        while (ReadForcesOnForceGrid() && timeout <= 5) {
            AddDerivedForcesToForceGrid();
            timeout++;
            if (timeout >= 5)
                Debug.LogError("force caluclation infinite loop");
        }
        Debug.Log($"grid force iteration looped {timeout} times");

        timeout = 0;
        int longerGridDimension = forceGrid.GetLength(0) > forceGrid.GetLength(1) ? 
            forceGrid.GetLength(0) : forceGrid.GetLength(1);

        CheckBlockedBlocks();
        while (CheckBlockedBlocks() && timeout <= longerGridDimension) {
            timeout++;
        }
        Debug.Log($"grid blocked check iteration looped {timeout} times");

        foreach (BlockBehaviour b in gridRef.ActiveGridState.BlocksList) {
            b.Move();
            Debug.Log($"moving {b.gameObject.name}");
        }

        gridRef.ActiveGridState.UpdateCoordList();
        Debug.Log("Full block grid iteration");
    }

    private void InitilizeEmptyForceGrid()
    {
        for(int x=0; x<forceGrid.GetLength(0); x++) {
            for(int y=0; y<forceGrid.GetLength(1); y++) {
                forceGrid[x, y] = new CellForce();
            }
        }
    }

    public void AddInitialForcesToForceGrid()
    {
        foreach (BlockBehaviour b in gridRef.ActiveGridState.BlocksList) {
            Vector2Int moveIntent = b.GetMovementIntention();
            Vector2Int targetCell = b.coord + moveIntent;

            b.lastForces.SetForceFromVector2Int(b.GetMovementIntention());
            Debug.Log($"{b.name} just set force to {b.lastForces.QueryForce()} from moveIntent {b.GetMovementIntention()}");

            //block target cell isnt on grid (at edge)
            if(!gridRef.isValidGridCoord(targetCell)) {
                continue;
            }

            forceGrid[targetCell.x, targetCell.y].SetForceFromVector2Int(moveIntent);
        }
    }

    public void AddDerivedForcesToForceGrid()
    {
        foreach (BlockBehaviour b in gridRef.ActiveGridState.BlocksList) {;
            Vector2Int targetCell = b.coord + b.lastForces.QueryForce();

            //block target cell isnt on grid (at edge)
            if (!gridRef.isValidGridCoord(targetCell) || targetCell == b.coord) {
                continue;
            }

            forceGrid[targetCell.x, targetCell.y] = new CellForce(b.lastForces);
        }
    }

    public bool ReadForcesOnForceGrid()
    {
        bool changes = false;

        foreach (BlockBehaviour b in gridRef.ActiveGridState.BlocksList) {
            Vector2Int moveIntent = b.lastForces.QueryForce();
            Vector2Int targetCell = b.coord + moveIntent;

            var forces = new CellForce();

            //add force on current cell
            forces.AddForceFromCell(forceGrid[b.coord.x, b.coord.y]);
            //add force from target cell
            if (!gridRef.isValidGridCoord(targetCell)) {
                Debug.LogWarning($"tried to read forces from invalid cell {targetCell}");
            }
            else {
                forces.AddForceFromCell(forceGrid[targetCell.x, targetCell.y]);
            }

            if (!b.lastForces.Equals(forces))
                changes = true;

            b.lastForces.AddForceFromCell(forces);
        }

        return changes;
    }

    public bool CheckBlockedBlocks()
    {
        bool changes = false;

        foreach (BlockBehaviour b in gridRef.ActiveGridState.BlocksList) {
            if (b.blocked) continue;

            Vector2Int moveIntent = b.lastForces.QueryForce();
            Vector2Int targetCell = b.coord + moveIntent;



            if (!gridRef.isValidGridCoord(targetCell)) {
                Debug.Log($"{b.name} move target cell is not valid and is now blocked");
                changes = true;
                b.blocked = true;
                b.lastForces = new CellForce();
            }

            //temp, check more intelligently by coord instead of looping later
            if(!b.blocked) {
                foreach(BlockBehaviour otherB in gridRef.ActiveGridState.BlocksList) {
                    if (b.GetInstanceID() == otherB.GetInstanceID()) continue;

                    if(targetCell == otherB.coord && otherB.blocked) {
                        changes = true;
                        b.blocked = true;
                        b.lastForces = new CellForce();
                    }
                }
            }
        }

        return changes;
    }

#if UNITY_EDITOR

    private void OnDrawGizmos()
    {
        gridRef.ForEachCellAtCellCenter((Vector2Int coord, Vector3 pos) => DrawDirectionalSquares(coord, pos));
    }

    public void DrawDirectionalSquares(Vector2Int coord, Vector3 center)
    {
        // Half the length of the square (0.5f makes total size 1 unit)
        float dist = 0.25f;
        float size = .2f;

        //get cell force
        CellForce cellForce = forceGrid!=null ? forceGrid[coord.x, coord.y] : new CellForce();

        // Default color (grey)
        Color defaultColor = Color.grey;
        Color highlightColor = Color.blue;

        Handles.Label(center, $"{coord}", new GUIStyle() { alignment = TextAnchor.MiddleCenter});

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
}
