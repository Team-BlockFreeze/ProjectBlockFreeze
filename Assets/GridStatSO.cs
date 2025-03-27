using UnityEngine;

[CreateAssetMenu(fileName = "GridStateSO", menuName = "ScriptableObjects/GridStateSO")]
[System.Serializable]
public class GridStateSO : ScriptableObject
{
    [SerializeField]
    private Vector2Int gridSize = new Vector2Int(5, 5);
    public Vector2Int GridSize => gridSize;

    [SerializeField]
    private BlockState[,] gridState = new BlockState[5, 5];
    public BlockState[,] GridState => gridState;
}