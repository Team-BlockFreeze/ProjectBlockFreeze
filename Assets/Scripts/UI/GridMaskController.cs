using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public class GridMaskController : MonoBehaviour {
    [Header("Mask Panel References")]
    [SerializeField]
    private RectTransform topMaskPanel;
    [SerializeField]
    private RectTransform bottomMaskPanel;
    [SerializeField]
    private RectTransform leftMaskPanel;
    [SerializeField]
    private RectTransform rightMaskPanel;


    [SerializeField] private Camera mainCamera;
    private Vector2Int lastKnownGridSize = new Vector2Int(-1, -1);

    private void OnEnable() {
        mainCamera = Camera.main;
        BlockGrid.Instance.StateLoadedFromSO += UpdateMaskBounds;

        DOVirtual.DelayedCall(0.5f, UpdateMaskBounds);
    }

    private void OnDisable() {
        BlockGrid.Instance.StateLoadedFromSO -= UpdateMaskBounds;
    }

    [Header("Mask Settings")]
    [SerializeField]
    [InfoBox("Higher values = smaller border --- Lower values = larger border.")]
    private float borderDivisor = 10f;

    [Button]
    public void UpdateMaskBounds() {
        if (BlockGrid.Instance == null) {
            Debug.LogWarning("GridMaskController: BlockGrid instance not found.", this);
            return;
        }

        Vector3 worldBottomLeft = BlockGrid.Instance.GetBotLeftOriginPos();
        Vector3 worldTopRight = worldBottomLeft + new Vector3(BlockGrid.Instance.GridSize.x, BlockGrid.Instance.GridSize.y, 0);

        Vector2 screenBottomLeft = mainCamera.WorldToScreenPoint(worldBottomLeft);
        Vector2 screenTopRight = mainCamera.WorldToScreenPoint(worldTopRight);

        RectTransform parentRect = transform as RectTransform;
        float border = parentRect.rect.height / borderDivisor;

        if (topMaskPanel != null) {
            topMaskPanel.offsetMin = new Vector2(0, screenTopRight.y + border);
            topMaskPanel.offsetMax = new Vector2(0, 0);
        }

        if (rightMaskPanel != null) {
            rightMaskPanel.offsetMin = new Vector2(screenTopRight.x + border, 0);
            rightMaskPanel.offsetMax = new Vector2(0, 0);
        }

        if (bottomMaskPanel != null) {
            bottomMaskPanel.offsetMin = new Vector2(0, 0);
            bottomMaskPanel.offsetMax = new Vector2(0, (screenBottomLeft.y - border) - parentRect.rect.height);
        }

        if (leftMaskPanel != null) {
            leftMaskPanel.offsetMin = new Vector2(0, 0);
            leftMaskPanel.offsetMax = new Vector2((screenBottomLeft.x - border) - parentRect.rect.width, 0);
        }

        lastKnownGridSize = BlockGrid.Instance.GridSize;
    }

    private void OnDrawGizmos() {
        Vector3 worldBottomLeft = BlockGrid.Instance.GetBotLeftOriginPos();
        Vector3 worldTopRight = worldBottomLeft + new Vector3(BlockGrid.Instance.GridSize.x, BlockGrid.Instance.GridSize.y, 0);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(worldBottomLeft, Vector3.one);
        Gizmos.DrawWireCube(worldTopRight, Vector3.one);

        Vector2 screenBottomLeft = mainCamera.WorldToScreenPoint(worldBottomLeft);
        Vector2 screenTopRight = mainCamera.WorldToScreenPoint(worldTopRight);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(screenBottomLeft, screenTopRight);
    }
}