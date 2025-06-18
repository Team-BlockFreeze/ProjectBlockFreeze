
using System;
using DG.Tweening;
using UnityEngine;

public class BlockVoid : TileEffectBase {
    public override void OnBlockEnter(BlockBehaviour enteringBlock) {
        Debug.Log("BlockVoid: " + enteringBlock.name + " entered void");

        if (enteringBlock == this.block) return;

        BlockGrid.Instance.ActiveGridState.BlocksList.Remove(enteringBlock);
        AnimateBlockDisappear(enteringBlock);

    }


    public override void OnBlockExit(BlockBehaviour exitingBlock) {

    }

    private void AnimateBlockDisappear(BlockBehaviour enteringBlock) {
        enteringBlock.transform.DOScale(0f, 0.5f).OnComplete(() => {
            enteringBlock.gameObject.SetActive(false);
            BlockCoordinator.Instance.GridRef.ActiveGridState.UpdateCoordList();
        });
    }

    private void AnimateBlockAppear(BlockBehaviour exitingBlock) {
        exitingBlock.transform.DOScale(1f, 0.5f);
    }

}
