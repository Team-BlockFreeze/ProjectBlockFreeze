using UnityEngine;
using Sirenix.OdinInspector;

[System.Serializable]
public class BlockState
{
    [BoxGroup("Block Settings")]
    [SerializeField]
    [PreviewField(100)]
    private GameObject fabRef;

    public enum BlockMoveState { Teleport, PingPong, Patrol, Still }

    [BoxGroup("Movement Settings")]
    [SerializeField, EnumToggleButtons]
    private BlockMoveState moveState = BlockMoveState.PingPong;
    public BlockMoveState MoveState => moveState;

    [BoxGroup("Movement Path")]
    [ListDrawerSettings(ShowFoldout = true)]
    [SerializeField]
    private Vector2Int[] moveList;
}
