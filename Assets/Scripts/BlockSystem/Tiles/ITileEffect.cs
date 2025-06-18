

public interface ITileEffect {
    /// <summary>
    /// Called by the BlockCoordinator when a block moves onto this tile's coordinate.
    /// </summary>
    /// <param name="enteringBlock">The block that has just entered the tile.</param>
    void OnBlockEnter(BlockBehaviour enteringBlock);

    /// <summary>
    /// Called by the BlockCoordinator when a block moves off of this tile's coordinate.
    /// </summary>
    /// <param name="exitingBlock">The block that has just left the tile.</param>
    void OnBlockExit(BlockBehaviour exitingBlock);

    /// <summary>
    /// The BlockBehaviour component associated with this tile effect.
    /// Used by the Coordinator to get the tile's coordinate.
    /// </summary>
    BlockBehaviour TileBlock { get; }
}