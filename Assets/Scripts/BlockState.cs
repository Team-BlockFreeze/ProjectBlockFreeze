using UnityEngine;

[System.Serializable]
public class BlockState
{
    [SerializeField]
    private GameObject fabRef;

    public enum BlockMoveState { teleport, pingpong, patrol, still }
    public BlockMoveState moveState = BlockMoveState.pingpong;

    [SerializeField]
    Vector2Int[] moveList;
}