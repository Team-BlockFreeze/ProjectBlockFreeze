using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;
using Sirenix.Utilities;

public class BlockTeleportTile : TileEffectBase {
    [Title("Teleport Settings")]
    [InfoBox("Coordinate the entering block teleports to.")]
    [OnValueChanged("ValidateTeleportCoordinate")]
    [SerializeField]
    private Vector2Int teleportDestination;



    [Title("Visuals & Timing")]
    [SerializeField, Range(0f, 1f)]
    private float exitAnimationTime = 0.25f;

    [SerializeField, Range(0f, 1f)]
    private float enterAnimationTime = 0.25f;

    [SerializeField]
    private Ease exitEase = Ease.InBack;

    [SerializeField]
    private Ease enterEase = Ease.OutBack;

    private BlockGrid Grid => BlockGrid.Instance;

    // there's a reference to block called 'block' in TileEffectBase parent class

    [SerializeField] private GameObject teleportIndicator;


    private LineRenderer lineRenderer;

    protected override void Awake() {
        base.Awake();
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled = true;
    }

    protected override void OnEnable() {
        base.OnEnable();
        UpdateLineRenderer();
        teleportIndicator.SetActive(true);
    }
    protected override void OnDisable() {
        base.OnDisable();
        lineRenderer.enabled = false;
        lineRenderer = null;
        teleportIndicator.SetActive(false);
    }
    private void OnValidate() {
        UpdateLineRenderer();
    }
    private void UpdateLineRenderer() {
        if (Grid == null || lineRenderer == null) return;

        Vector3 start = transform.position;
        Vector3 end = Grid.GetWorldSpaceFromCoord(teleportDestination);
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        teleportIndicator.transform.position = end;
        // teleportIndicator.transform.localScale = Vector3.one * 0.3f;
    }

    public void UpdateTeleportDestination() {
        BlockBehaviour.Direction[] movePath = block.GetMovePath();


        Vector2Int finalTeleportDestination = block.coord;

        for (int i = 0; i < movePath.Length; i++) {
            finalTeleportDestination += (Vector2Int)block.DirToVec3Int(movePath[i]);
        }

        teleportDestination = finalTeleportDestination;


    }

    public override void OnBlockEnter(BlockBehaviour enteringBlock) {
        if (enteringBlock == this.block) return;

        if (IsDestinationBlocked()) return;

        Log($"Teleporting block '{enteringBlock.name}' from {block.coord} to {teleportDestination}.");


        UpdateTeleportDestination();
        StartTeleportSequence(enteringBlock);
    }

    public override void OnBlockExit(BlockBehaviour exitingBlock) {
        if (exitingBlock == this.block) return;

        Log($"Block '{exitingBlock.name}' exited teleport tile at {block.coord}.");
    }

    private void StartTeleportSequence(BlockBehaviour blockToTeleport) {

        blockToTeleport.phaseThrough = true;

        blockToTeleport.transform.DOScale(0, exitAnimationTime).SetEase(exitEase)
            .OnComplete(() => {
                blockToTeleport.transform.DOKill();
                blockToTeleport.transform.DOComplete();

                Grid.ActiveGridState.BlockCoordList.Remove(blockToTeleport.coord);
                Grid.ActiveGridState.BlockCoordList.Add(teleportDestination);
                blockToTeleport.coord = teleportDestination;
                blockToTeleport.GetComponent<BlockPreview>()?.UpdateLine();
                blockToTeleport.transform.position = Grid.GetWorldSpaceFromCoord(teleportDestination);

                blockToTeleport.transform.DOScale(1, enterAnimationTime).SetEase(enterEase)
                    .OnComplete(() => {
                        blockToTeleport.phaseThrough = false;

                        // In case the block landed on another special tile (e.g., a void).
                        BlockCoordinator.Instance.ProcessTileEffectsAfterTeleport();

                        if (blockToTeleport.blockType.Contains("key") && blockToTeleport.coord == Grid.GoalCoord) {
                            Log("Key Tp'd to goal");
                            blockToTeleport.GetComponent<BlockKey>().HasKeyReachedGoal();
                        }
                    });
            });
    }

    #region Odin Inspector Validation

    // [OnValueChanged] 
    private void ValidateTeleportCoordinate() {
        if (Grid == null) return;

        teleportDestination.x = Mathf.Clamp(teleportDestination.x, 0, Grid.LevelData.GridSize.x - 1);
        teleportDestination.y = Mathf.Clamp(teleportDestination.y, 0, Grid.LevelData.GridSize.y - 1);
    }

    private bool IsDestinationBlocked() {
        if (Grid == null) return false;

        var blocksAtDestination = Grid.QueryGridCoordForAllBlocks(teleportDestination);
        foreach (var block in blocksAtDestination) {
            if (!block.phaseThrough) {
                LogWarning($"Teleport failed: Destination {teleportDestination} is blocked by '{block.name}'.");
                return true;
            }
        }
        return false;
    }

    private void OnDrawGizmos() {
        if (Grid == null) return;

        Gizmos.color = Color.cyan;
        Vector3 startPos = transform.position;
        Vector3 endPos = Grid.GetWorldSpaceFromCoord(teleportDestination);
        Gizmos.DrawLine(startPos, endPos);

        Gizmos.color = new Color(0, 1, 1, 0.5f);
        Gizmos.DrawCube(endPos, Vector3.one);
    }

    #endregion
}