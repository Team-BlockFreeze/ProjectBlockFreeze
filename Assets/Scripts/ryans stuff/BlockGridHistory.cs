using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[System.Serializable]
public class BlockGridHistory {
    public List<BlockSnapshot> blockSnapshots;

    public BlockGridHistory(List<BlockBehaviour> blocks) {
        blockSnapshots = new List<BlockSnapshot>();
        foreach (var b in blocks) {
            // if (b.gameObject.name.Equals("BlockVariant Wall(Clone)")) continue;
            if (b.canBeFrozen == false && b.blocked == true && b.frozen == true) {
                //! If a block is static, don't record its history
                continue;
            }
            blockSnapshots.Add(new BlockSnapshot(b));
        }
    }
}

[System.Serializable]
public class BlockSnapshot {
    public BlockBehaviour block;
    public Vector2Int previousCoord;
    public int previousMoveIdx;
    public bool pingpongIsForward;

    public bool wasFrozen;
    public bool wasBlocked;

    public BlockSnapshot(BlockBehaviour b) {
        this.block = b;
        pingpongIsForward = b.GetPingpongIsForward();
        previousCoord = b.coord;
        previousMoveIdx = b.GetMoveIdx();
        wasFrozen = b.frozen;
        wasBlocked = b.blocked;
    }


    public void ApplyUndo() {
        block.UpdateMovementVisualiser();
        block.SetPingpongIsForward(pingpongIsForward);
        block.coord = previousCoord;
        block.SetMoveIdx(previousMoveIdx);
        block.frozen = wasFrozen;
        block.TrySetFreeze(wasFrozen);
        block.blocked = wasBlocked;
        block.transform.DOMove(block.GridRef.GetWorldSpaceFromCoord(previousCoord), GameSettings.Instance.gameTickInSeconds / 2f)
            .SetEase(Ease.OutQuad);
        block.GetComponent<BlockPreview>().UpdateLine();
        block.GetComponent<BlockPreview>().DrawPath();

    }
}