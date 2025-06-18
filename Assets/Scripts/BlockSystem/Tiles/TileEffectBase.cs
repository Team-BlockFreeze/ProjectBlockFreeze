using UnityEngine;

public abstract class TileEffectBase : LoggerMonoBehaviour, ITileEffect {
    [SerializeField] protected BlockBehaviour block;

    public BlockBehaviour TileBlock => block;

    protected virtual void Awake() {
        if (block == null)
            block = GetComponent<BlockBehaviour>();
    }

    protected virtual void OnEnable() {
        BlockCoordinator.Instance?.RegisterTileEffect(this);
    }

    protected virtual void OnDisable() {
        BlockCoordinator.Instance?.UnregisterTileEffect(this);
    }

    public abstract void OnBlockEnter(BlockBehaviour enteringBlock);
    public abstract void OnBlockExit(BlockBehaviour exitingBlock);
}
