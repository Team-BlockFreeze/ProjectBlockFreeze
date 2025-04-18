using UnityEngine;
using DG.Tweening;

public class IngameCanvasButtons : MonoBehaviour {
    [Header("Canvas Panels")]
    [SerializeField] private GameObject settingsMenuCanvas;
    [SerializeField] private GameObject gameSettingsCanvas;
    [SerializeField] private GameObject soundSettingsCanvas;
    [SerializeField] private GameObject buttonsCanvas;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 0.25f;

    private GameObject[] allCanvases;

    private void Awake() {
        allCanvases = new GameObject[] { settingsMenuCanvas, gameSettingsCanvas, soundSettingsCanvas, buttonsCanvas };
        ExitButton();
    }

    public void ExitButton() {
        ShowOnlyCanvas(buttonsCanvas);
    }

    public void ShowSettingsMenu() {
        ShowOnlyCanvas(settingsMenuCanvas);
    }

    public void ShowGameSettings() {
        ShowOnlyCanvas(gameSettingsCanvas);
    }

    public void ShowSoundSettings() {
        ShowOnlyCanvas(soundSettingsCanvas);
    }

    public void ShowButtons() {
        ShowOnlyCanvas(buttonsCanvas);
    }

    private void ShowOnlyCanvas(GameObject targetCanvas) {
        Sequence fadeSequence = DOTween.Sequence();

        foreach (GameObject canvas in allCanvases) {
            if (canvas.activeSelf && canvas != targetCanvas) {
                CanvasGroup cg = canvas.GetComponent<CanvasGroup>();
                if (cg != null) {
                    cg.DOKill();
                    fadeSequence.Append(cg.DOFade(0f, fadeDuration).SetEase(Ease.InQuad).OnComplete(() => {
                        canvas.SetActive(false);
                    }));
                }
                else {
                    canvas.SetActive(false);
                }
            }
        }

        fadeSequence.AppendCallback(() => {
            CanvasGroup targetGroup = targetCanvas.GetComponent<CanvasGroup>();
            if (targetGroup != null) {
                targetCanvas.SetActive(true);
                targetGroup.alpha = 0f;
                targetGroup.DOKill();
                targetGroup.DOFade(1f, fadeDuration).SetEase(Ease.OutQuad);
            }
            else {
                targetCanvas.SetActive(true);
            }
        });
    }
}
