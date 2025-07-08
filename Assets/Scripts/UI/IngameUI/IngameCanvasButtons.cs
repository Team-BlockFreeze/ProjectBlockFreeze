using System;
using System.Collections.Generic;
using Ami.BroAudio;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class IngameCanvasButtons : MonoBehaviour {
    [Header("Canvas Panels")]
    [SerializeField]
    private GameObject settingsMenuCanvas;

    [SerializeField] private GameObject gameSettingsCanvas;
    [SerializeField] private GameObject soundSettingsCanvas;
    [SerializeField] private GameObject interrupterCanvas;
    [SerializeField] private GameObject buttonsCanvas;
    [SerializeField] private GameObject levelCompleteCanvas;

    [SerializeField] private GameObject blurOverlay;

    [Header("Fade Settings")]
    [SerializeField]
    private float fadeDuration = 0.25f;

    private GameObject[] allCanvases;

    private CanvasGroup buttonsCanvasGroup;
    public bool buttonsActive => buttonsCanvas.activeSelf && GetButtonAlpha();
    /// <summary>
    /// wrapper so the canvas can be cached
    /// </summary>
    /// <returns></returns>
    private bool GetButtonAlpha() {
        if (buttonsCanvas == null)
            buttonsCanvasGroup = buttonsCanvas.GetComponent<CanvasGroup>();

        return buttonsCanvasGroup.alpha == 1f;
    }

    public static UnityEvent<bool> Event_OnCanvasFadeButtonState = new UnityEvent<bool>();


    private bool hasCalledReset;

    private void Awake() {
        allCanvases = new[] { settingsMenuCanvas, gameSettingsCanvas, soundSettingsCanvas, buttonsCanvas, interrupterCanvas, levelCompleteCanvas };
        ExitButton();
    }
    private void OnEnable() {
        BlockKey.Event_LevelComplete.AddListener(OnLevelComplete);
    }

    private void OnDisable() {
        BlockKey.Event_LevelComplete.RemoveListener(OnLevelComplete);
    }

    private bool isLevelCompleteShowing = false;

    private void OnLevelComplete(LevelDataSO levelData) {
        ShowLevelComplete();
        isLevelCompleteShowing = true;
    }


    public void ExitButton() {
        ShowOnlyCanvas(buttonsCanvas);
    }

    //public void ShowOnlyCanvasOfGO(GameObject canvasGO) {
    //    ShowOnlyCanvas(canvasGO);
    //}

    //Title and Tutorial Messages
    public void ShowInterrupter() {
        ShowOnlyCanvas(interrupterCanvas);
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

    public void ShowLevelComplete() {
        ShowOnlyCanvas(levelCompleteCanvas);
    }

    private Sequence FadeSequence;

    public void SetBlur(bool showBlur) {
        var blurGroup = blurOverlay.GetComponent<CanvasGroup>();

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
                    DOVirtual.DelayedCall(0.5f, () => {
                        GetComponentInChildren<GridMaskController>().UpdateMaskBounds();
                    });
                });
            }
        }
        else {
            blurOverlay.SetActive(showBlur);
        }
    }

    public SoundID buttonClickedSFX;

    private void ShowOnlyCanvas(GameObject targetCanvas) {
        buttonClickedSFX.Play();

        FadeSequence?.Kill();
        FadeSequence = DOTween.Sequence();

        foreach (var canvas in allCanvases)
            if (canvas.activeSelf && canvas != targetCanvas) {
                var cg = canvas.GetComponent<CanvasGroup>();
                if (cg != null) {
                    //cg.DOKill();
                    FadeSequence.Join(cg.DOFade(0f, fadeDuration).SetEase(Ease.InQuad).OnComplete(() => {
                        canvas.SetActive(false);
                    }));
                }
                else {
                    canvas.SetActive(false);
                }
            }

        //FadeSequence.AppendInterval(0f);

        FadeSequence.AppendCallback(() => {
            var showBlur = targetCanvas != buttonsCanvas;
            var blurGroup = blurOverlay.GetComponent<CanvasGroup>();

            if (blurGroup != null) {
                //blurGroup.DOKill();

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

            //var targetGroup = targetCanvas.GetComponent<CanvasGroup>();
            //if (targetGroup != null) {
            //    targetCanvas.SetActive(true);
            //    targetGroup.alpha = 0f;
            //    //targetGroup.DOKill();
            //    //FadeSequence.Join(targetGroup.DOFade(1f, fadeDuration).SetEase(Ease.OutQuad));
            //    targetGroup.DOFade(1f, fadeDuration).SetEase(Ease.OutQuad);
            //}
            //else {
            //    targetCanvas.SetActive(true);
            //}
        });

        var targetGroup = targetCanvas.GetComponent<CanvasGroup>();
        if (targetGroup != null) {
            targetCanvas.SetActive(true);
            targetGroup.alpha = 0f;
            //targetGroup.DOKill();
            FadeSequence.Join(targetGroup.DOFade(1f, fadeDuration).SetEase(Ease.OutQuad));
            //targetGroup.DOFade(1f, fadeDuration).SetEase(Ease.OutQuad);
        }
        else {
            targetCanvas.SetActive(true);
        }

        DOVirtual.DelayedCall(1f, () => {
            GetComponentInChildren<GridMaskController>().UpdateMaskBounds();
        });

        //broadcasts whether buttons are visible and therefore if controls should be enabled
        Event_OnCanvasFadeButtonState?.Invoke(targetCanvas == buttonsCanvas);
    }


    public SoundID reloadLevelSFX;

    private bool cooldown = false;
    public void ReloadLevel() {
        if (cooldown) return;

        StartCoroutine(ReloadButtonCooldownCoroutine());

        var levelRoot = GetRootObjectByName("Level Design");

        if (levelRoot == null) {
            Debug.LogWarning("Level Design object not found");
            return;
        }

        reloadLevelSFX.Play();


        BlockCoordinator.Instance.SetAutoplay(false);

        var t = levelRoot.transform;
        var originalPos = t.position;
        hasCalledReset = false;

        var moveDistance = 100f;
        var moveDuration = GameSettings.Instance.reloadAnimationTime / 2f;
        var overshootDistance = 5f;
        var returnDuration = GameSettings.Instance.reloadAnimationTime / 10 * 3f;
        var settleDuration = GameSettings.Instance.reloadAnimationTime / 10 * 2f;

        foreach (var blockPreview in BlockGrid.Instance.BlocksListTransform.GetComponentsInChildren<BlockPreview>()) {
            blockPreview.GetEndDotInstance()?.SetActive(false);
            blockPreview.FadeOutPreview(0.1f);
        }

        foreach (BlockBehaviour block in BlockGrid.Instance.ActiveGridState.BlocksList) {
            block.blockTrail.Clear();
        }

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

    private System.Collections.IEnumerator ReloadButtonCooldownCoroutine() {
        if (cooldown) yield break;

        cooldown = true;
        yield return new WaitForSeconds(GameSettings.Instance.reloadAnimationTime + 0.5f);
        cooldown = false;
    }

    private GameObject GetRootObjectByName(string name) {
        List<Scene> validScenes = new();

        // Add scenes only if loaded and valid
        Scene gridScene = SceneManager.GetSceneByName("Empty Grid Level");
        if (gridScene.IsValid() && gridScene.isLoaded)
            validScenes.Add(gridScene);

        Scene debugScene = SceneManager.GetSceneByName("Kerry's Scene for level creating");
        if (debugScene.IsValid() && debugScene.isLoaded)
            validScenes.Add(debugScene);

        foreach (var scene in validScenes) {
            foreach (var go in scene.GetRootGameObjects()) {
                if (go.name == name) {
                    return go;
                }
            }
        }

        return null;
    }

}