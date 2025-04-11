using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[System.Serializable]
public class BlockGridHistory {
    public List<BlockSnapshot> blockSnapshots;

    public BlockGridHistory(List<BlockBehaviour> blocks) {
        blockSnapshots = new List<BlockSnapshot>();
        foreach (var b in blocks) {
            blockSnapshots.Add(new BlockSnapshot(b));
        }
    }
}

[System.Serializable]
public class BlockSnapshot {
    public BlockBehaviour block;
    public Vector2Int previousCoord;
    public bool wasFrozen;
    public bool wasBlocked;

    public BlockSnapshot(BlockBehaviour b) {
        this.block = b;
        previousCoord = b.coord;
        wasFrozen = b.frozen;
        wasBlocked = b.blocked;
    }


    public void ApplyUndo() {
        // block.DecrementMoveIdx();
        block.coord = previousCoord;
        block.frozen = wasFrozen;
        block.blocked = wasBlocked;
        block.transform.DOMove(block.GridRef.GetWorldSpaceFromCoord(previousCoord), GameSettings.Instance.gameTickInSeconds / 2f)
            .SetEase(Ease.OutQuad);

        Debug.Log(block.GetMoveIdx());
    }
}