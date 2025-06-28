using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.Events;

public class FillBarUI : MonoBehaviour, IPointerDownHandler, IDragHandler {
    [SerializeField] private UnityEvent<float> onFillChanged;

    private Image fillImage;
    private float tweenDuration = 0.2f;
    private RectTransform fillRect;
    private float fillAmount;

    private void Awake() {
        fillRect = GetComponent<RectTransform>();
        fillImage = GetComponent<Image>();
    }

    public void OnPointerDown(PointerEventData eventData) => UpdateFill(eventData);

    public void OnDrag(PointerEventData eventData) => UpdateFill(eventData);

    private void UpdateFill(PointerEventData eventData) {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            fillRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint)) {
            float width = fillRect.rect.width;
            float normalizedX = Mathf.Clamp01(localPoint.x / width);
            fillAmount = normalizedX;

            fillImage.DOKill();
            fillImage.DOFillAmount(normalizedX, tweenDuration).SetEase(Ease.OutQuad);
            fillAmount = normalizedX;

            onFillChanged?.Invoke(normalizedX);
        }
    }

    public void AddListener(UnityAction<float> callback) {
        onFillChanged.AddListener(callback);
    }
}
