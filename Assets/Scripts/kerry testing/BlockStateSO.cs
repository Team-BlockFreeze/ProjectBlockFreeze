using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "BlockState", menuName = "ScriptableObjects/BlockState")]
public class BlockStateSO : ScriptableObject
{
    [SerializeField] private GameObject fabRef;

    public enum BlockMoveState { Teleport, PingPong, Patrol, Still }

    [SerializeField] private BlockMoveState moveState = BlockMoveState.PingPong;

    [SerializeField] private Vector2Int[] moveList;

    public GameObject FabRef => fabRef;
    public BlockMoveState MoveState => moveState;
    private Vector2Int[] MoveList => moveList;

    public List<MovementDirection> MoveList2 = new List<MovementDirection>();

    public enum MovementDirection
    {
        Up,
        Down,
        Left,
        Right,
        Wait
    }
}
