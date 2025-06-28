using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;
using System.Collections.Generic;

public class BlockTeleportTile : TileEffectBase {
    [Title("Teleport Settings")]
    [InfoBox("Coordinate the entering block teleports to.")]
    [OnValueChanged("ValidateTeleportCoordinate")]
    [SerializeField]
    private Vector2Int teleportDestination;

    [SerializeField]
    private Ease exitEase = Ease.InBack;

    [SerializeField]
    private Ease enterEase = Ease.OutBack;

    private BlockGrid Grid => BlockGrid.Instance;

    // there's a reference to block called 'block' in TileEffectBase parent class

    [SerializeField] private GameObject teleportIndicator;

    private LineRenderer lineRenderer;

    #region Teleport Conflict Resolution

    private struct PendingTeleport {
        public BlockTeleportTile Teleporter;
        public BlockBehaviour BlockToTeleport;
    }

    private static readonly Dictionary<Vector2Int, List<PendingTeleport>> PendingTeleportsByDestination = new Dictionary<Vector2Int, List<PendingTeleport>>();

    /// <summary>
    /// Called by BlockCoordinator at the start of each tick's effect processing to clear old data.
    /// </summary>
    public static void ClearTeleportIntentsForTick() {
        PendingTeleportsByDestination.Clear();
    }

    /// <summary>
    /// Called by BlockCoordinator after all OnBlockEnter events.
    /// It resolves conflicts and executes valid teleports.
    /// </summary>
    public static void ProcessPendingTeleports() {
        foreach (var kvp in PendingTeleportsByDestination) {
            var destination = kvp.Key;
            var requests = kvp.Value;

            if (requests.Count > 1) {
                Debug.LogWarning($"Teleport conflict at {destination}. {requests.Count} blocks attempted to teleport there. Aborting teleport for all involved blocks.");
                foreach (var request in requests) {
                    request.Teleporter.Log($"Teleport of '{request.BlockToTeleport.name}' cancelled due to conflict.");
                }
                continue; // Skip to the next destination.
            }

            var validRequest = requests[0];
            var teleporter = validRequest.Teleporter;
            var blockToTeleport = validRequest.BlockToTeleport;

            if (teleporter.IsDestinationBlocked()) {
                continue;
            }

            teleporter.Log($"Teleporting block '{blockToTeleport.name}' from {teleporter.block.coord} to {teleporter.teleportDestination}.");
            teleporter.StartTeleportSequence(blockToTeleport);
        }
    }

    #endregion

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
        // lineRenderer = null; // Do not nullify component reference on disable
        teleportIndicator.SetActive(false);
    }
    private void OnValidate() {
        UnityEditor.EditorApplication.delayCall += UpdateLineRenderer;
    }
    public void UpdateLineRenderer() {
        if (Grid == null || lineRenderer == null) return;


        teleportIndicator.SetActive(true);

        Vector3 start = transform.position;
        Vector3 end = Grid.GetWorldSpaceFromCoord(teleportDestination);
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        if (teleportIndicator != null)
            teleportIndicator.transform.position = end;
    }

    public void UpdateTeleportDestination() {
        if (block == null || block.GetMovePath() == null) return;

        BlockBehaviour.Direction[] movePath = block.GetMovePath();
        Vector2Int finalTeleportDestination = block.coord;

        for (int i = 0; i < movePath.Length; i++) {
            finalTeleportDestination += (Vector2Int)block.DirToVec3Int(movePath[i]);
        }
        teleportDestination = finalTeleportDestination;
    }

    public override void OnBlockEnter(BlockBehaviour enteringBlock) {
        if (enteringBlock == this.block) return;

        UpdateTeleportDestination();

        if (!PendingTeleportsByDestination.ContainsKey(teleportDestination)) {
            PendingTeleportsByDestination[teleportDestination] = new List<PendingTeleport>();
        }

        PendingTeleportsByDestination[teleportDestination].Add(new PendingTeleport {
            Teleporter = this,
            BlockToTeleport = enteringBlock
        });

        Log($"Block '{enteringBlock.name}' registered intent to teleport to {teleportDestination}.");
    }

    public override void OnBlockExit(BlockBehaviour exitingBlock) {
        if (exitingBlock == this.block) return;

        Log($"Block '{exitingBlock.name}' exited teleport tile at {block.coord}.");
    }

    private void StartTeleportSequence(BlockBehaviour blockToTeleport) {
        blockToTeleport.phaseThrough = true;

        Grid.ActiveGridState.BlockCoordList.Remove(blockToTeleport.coord);
        blockToTeleport.coord = teleportDestination;
        Grid.ActiveGridState.BlockCoordList.Add(teleportDestination);

        float gameTick = GameSettings.Instance.gameTickInSeconds;

        blockToTeleport.transform.DOScale(0, gameTick / 3).SetEase(exitEase)
            .OnComplete(() => {
                blockToTeleport.transform.DOKill();

                blockToTeleport.GetComponent<BlockPreview>()?.UpdateLine();
                blockToTeleport.transform.position = Grid.GetWorldSpaceFromCoord(teleportDestination);

                blockToTeleport.transform.DOScale(1, gameTick / 3).SetEase(enterEase)
                    .OnComplete(() => {
                        blockToTeleport.phaseThrough = false;

                        // In case the block landed on another special tile (e.g., a void).
                        BlockCoordinator.Instance.ProcessTileEffectsAfterTeleport(true);

                        if (blockToTeleport.blockType.Contains("key") && blockToTeleport.coord == Grid.GoalCoord) {
                            Log("Key TP'd to goal");
                            blockToTeleport.GetComponent<BlockKey>().HasKeyReachedGoal();
                        }
                    });
            });
    }

    #region Odin Inspector Validation

    private void ValidateTeleportCoordinate() {
        if (Grid == null) return;

        teleportDestination.x = Mathf.Clamp(teleportDestination.x, 0, Grid.LevelData.GridSize.x - 1);
        teleportDestination.y = Mathf.Clamp(teleportDestination.y, 0, Grid.LevelData.GridSize.y - 1);
    }

    private bool IsDestinationBlocked() {
        if (Grid == null) return false;

        var blocksAtDestination = Grid.QueryGridCoordForAllBlocks(teleportDestination);
        foreach (var block in blocksAtDestination) {
            // A block is only truly "blocked" if the obstacle is not phasing.
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