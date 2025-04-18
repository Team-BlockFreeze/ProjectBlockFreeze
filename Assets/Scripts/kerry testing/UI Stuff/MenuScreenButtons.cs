using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class MenuScreenButtons : MonoBehaviour {
    [Header("References")]
    [SerializeField] private CanvasGroup buttons;
    [SerializeField] private Image overlayFade;


    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 4f;

    [SerializeField] private GameObject singleUseButton;

    public void FadeInButtons() {
        buttons.alpha = 0f;
        buttons.gameObject.SetActive(true);
        buttons.DOKill();
        buttons.DOFade(1f, fadeDuration)
            .SetEase(Ease.OutQuad);

        // Slightly move the buttons group upward
        RectTransform buttonsRect = buttons.GetComponent<RectTransform>();
        if (buttonsRect != null) {
            buttonsRect.DOKill();
            Vector2 originalPos = buttonsRect.anchoredPosition;
            buttonsRect.anchoredPosition = originalPos - new Vector2(0f, 20f);
            buttonsRect.DOAnchorPosY(originalPos.y, .5f)
                .SetEase(Ease.OutQuad);
        }

        singleUseButton.SetActive(false);
    }



    public void FadeOutOverlay() {
        Color overlayColor = overlayFade.color;
        overlayFade.DOKill();
        overlayFade.DOColor(new Color(overlayColor.r, overlayColor.g, overlayColor.b, 0f), fadeDuration).SetEase(Ease.InQuad);
    }
}
