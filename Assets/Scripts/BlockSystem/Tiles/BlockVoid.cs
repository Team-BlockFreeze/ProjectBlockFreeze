
using System;
using DG.Tweening;
using UnityEngine;

public class BlockVoid : TileEffectBase {

    // It's initially true just for visualization pre-runtime
    private void Start() {
        // GetComponent<SpriteRenderer>().enabled = false;
    }

    public override void OnBlockEnter(BlockBehaviour enteringBlock) {
        Debug.Log("BlockVoid: " + enteringBlock.name + " entered void");

        if (enteringBlock == this.block) return;

        BlockGrid.Instance.ActiveGridState.BlocksList.Remove(enteringBlock);
        AnimateBlockDisappear(enteringBlock);

    }


    public override void OnBlockExit(BlockBehaviour exitingBlock) {

    }

    public void AnimateBlockDisappear(BlockBehaviour enteringBlock) {
        enteringBlock.transform.DOScale(0f, 0.5f).OnComplete(() => {
            enteringBlock.gameObject.SetActive(false);
            enteringBlock.gameObject.GetComponent<BlockPreview>()?.GetEndDotInstance()?.SetActive(false);
            BlockCoordinator.Instance.GridRef.ActiveGridState.UpdateCoordList();
        });
    }

    public static void AnimateBlockAppear(BlockBehaviour block) {
        BlockGrid.Instance.ActiveGridState.BlocksList.Add(block);

        block.gameObject.SetActive(true);
        block.gameObject.GetComponent<BlockPreview>()?.GetEndDotInstance()?.SetActive(true);
        block.transform.localScale = Vector3.zero;
        block.transform.DOScale(1f, 0.5f);
    }

}
