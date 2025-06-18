using System;
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
            if (b.canBeFrozen == false && b.blocked == true && b.frozen == true && b.pushableWhenFrozen == false) {
                //! If a block is static, don't record its history
                continue;
            }
            blockSnapshots.Add(new BlockSnapshot(b));
        }
    }


    // Obsolete
    public BlockBehaviour GetBlockAtCoord(Vector2Int tileCoord) {
        foreach (var snapshot in blockSnapshots) {
            if (snapshot.block.coord == tileCoord) {
                return snapshot.block;
            }
        }
        return null;
    }

    public List<BlockBehaviour> GetBlocksAtCoord(Vector2Int tileCoord) {
        List<BlockBehaviour> blocksFound = new List<BlockBehaviour>();
        foreach (var snapshot in blockSnapshots) {
            if (snapshot.previousCoord == tileCoord) {
                blocksFound.Add(snapshot.block);
            }
        }
        return blocksFound;
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
    public bool wasActive;

    public BlockSnapshot(BlockBehaviour b) {
        this.block = b;
        pingpongIsForward = b.GetPingpongIsForward();
        previousCoord = b.coord;
        previousMoveIdx = b.GetMoveIdx();
        wasFrozen = b.frozen;
        wasBlocked = b.blocked;
        wasActive = b.gameObject.activeSelf;
    }


    // If a block was 'consumed' by a tile effect, bring it back
    private void BlockReappearFromUndo() {
        BlockGrid.Instance.ActiveGridState.BlocksList.Add(block);

        block.gameObject.SetActive(true);
        block.transform.localScale = Vector3.zero;
        block.transform.DOScale(1f, 0.5f);
    }

    public void ApplyUndo() {
        if (block == null) return;


        // If the block WAS active in the snapshot from previously, reactivate it
        if (wasActive && !block.gameObject.activeSelf) {
            BlockReappearFromUndo();
        }
        else if (!wasActive && block.gameObject.activeSelf) {
            BlockGrid.Instance.ActiveGridState.BlocksList.Remove(block);
            block.gameObject.SetActive(false);
        }

        block.SetPingpongIsForward(pingpongIsForward);
        block.coord = previousCoord;
        block.SetMoveIdx(previousMoveIdx);
        block.frozen = wasFrozen;
        block.TrySetFreeze(wasFrozen);
        block.blocked = wasBlocked;

        if (block.GridRef != null) {
            block.transform.DOMove(block.GridRef.GetWorldSpaceFromCoord(previousCoord), GameSettings.Instance.gameTickInSeconds / 2f)
                .SetEase(Ease.OutQuad);
        }

        var preview = block.GetComponent<BlockPreview>();
        if (preview != null) {
            preview.UpdateLine();
            preview.DrawPath();
        }

        block.UpdateMovementVisualiser();
    }
}