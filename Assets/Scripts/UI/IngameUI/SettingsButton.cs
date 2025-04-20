using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SettingsButton : MonoBehaviour {
    [Header("References")]
    public CanvasGroup settingsUI;

    [Header("Animation Settings")]
    public float bobDuration = 0.15f;
    public float bobScale = 0.9f;
    public float fadeDuration = 0.3f;
    public float showScaleDuration = 0.3f;

    private bool isSettingsOpen = false;

    public void ShowButton() {
        gameObject.SetActive(true);
        transform.localScale = Vector3.zero;
        transform.DOScale(1f, showScaleDuration).SetEase(Ease.OutBack);
    }

    public void OnSettingsPressed() {
        transform.DOKill();
        transform
            .DOScale(bobScale, bobDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => {
                transform
                    .DOScale(1f, bobDuration)
                    .SetEase(Ease.OutBack);
            });

        if (!isSettingsOpen) {
            settingsUI.gameObject.SetActive(true);
            settingsUI.alpha = 0f;
            settingsUI.DOFade(1f, fadeDuration).SetEase(Ease.InOutQuad);
            isSettingsOpen = true;
        }
    }
}
