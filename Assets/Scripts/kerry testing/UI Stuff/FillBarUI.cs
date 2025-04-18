using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

public class FillBarUI : MonoBehaviour, IPointerDownHandler, IDragHandler {
    private Image fillImage;
    private float tweenDuration = 0.2f;

    private RectTransform fillRect;

    private void Awake() {
        fillRect = GetComponent<RectTransform>();
        fillImage = GetComponent<Image>();
    }

    public void OnPointerDown(PointerEventData eventData) {
        UpdateFill(eventData);
    }

    public void OnDrag(PointerEventData eventData) {
        UpdateFill(eventData);
    }

    private void UpdateFill(PointerEventData eventData) {
        Vector2 localPoint;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            fillRect, eventData.position, eventData.pressEventCamera, out localPoint)) {
            float width = fillRect.rect.width;
            float normalizedX = Mathf.Clamp01((localPoint.x / width));

            fillImage.DOKill();
            fillImage.DOFillAmount(normalizedX, tweenDuration).SetEase(Ease.OutQuad);
        }
    }
}
