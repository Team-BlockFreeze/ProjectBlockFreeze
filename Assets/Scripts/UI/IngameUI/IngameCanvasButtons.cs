using UnityEngine;
using DG.Tweening;

public class IngameCanvasButtons : MonoBehaviour {
    [Header("Canvas Panels")]
    [SerializeField] private GameObject settingsMenuCanvas;
    [SerializeField] private GameObject gameSettingsCanvas;
    [SerializeField] private GameObject soundSettingsCanvas;
    [SerializeField] private GameObject buttonsCanvas;
    [SerializeField] private GameObject blurOverlay;

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
            bool showBlur = targetCanvas != buttonsCanvas;
            CanvasGroup blurGroup = blurOverlay.GetComponent<CanvasGroup>();

            if (blurGroup != null) {
                blurGroup.DOKill();

                if (showBlur) {
                    blurOverlay.SetActive(true);
                    blurGroup.alpha = 0f;
                    blurGroup.DOFade(1f, fadeDuration).SetEase(Ease.OutQuad);
                }
                else {
                    blurGroup.DOFade(0f, fadeDuration).SetEase(Ease.InQuad).OnComplete(() => {
                        blurOverlay.SetActive(false);
                    });
                }
            }
            else {
                blurOverlay.SetActive(showBlur);
            }

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


    private bool hasCalledReset = false;

    public void ReloadLevel() {
        GameObject levelRoot = GetRootObjectByName("Level Design");

        if (levelRoot == null) {
            Debug.LogWarning("Level Design object not found");
            return;
        }

        Transform t = levelRoot.transform;
        Vector3 originalPos = t.position;
        hasCalledReset = false;

        float moveDistance = 100f;
        float moveDuration = 0.5f;
        float overshootDistance = 5f;
        float returnDuration = 0.3f;
        float settleDuration = 0.2f;

        t.DOMoveZ(originalPos.z + moveDistance, moveDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(() => {
                if (!hasCalledReset) {
                    hasCalledReset = true;
                    BlockGrid.Instance.LoadStateFromSO();
                }

                t.DOMoveZ(originalPos.z - overshootDistance, returnDuration)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() => {
                        t.DOMoveZ(originalPos.z, settleDuration)
                            .SetEase(Ease.OutQuad);
                    });
            });
    }



    private GameObject GetRootObjectByName(string name) {
        foreach (GameObject go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()) {
            if (go.name == name)
                return go;
        }
        return null;
    }




}
