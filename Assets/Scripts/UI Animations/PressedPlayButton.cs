using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using Ami.BroAudio;
using System;
using Systems.SceneManagement;
using Sirenix.OdinInspector;

public class PressedPlayButton : MonoBehaviour, IPointerDownHandler
{
    public Image playButton;
    public Image titlePanel;
    public Image optionsPanel;
    public float moveDistance = 100f;
    public float scaleFactor = 1.2f;
    public float animationDuration = 1f;
    public float hoverDuration = 0.3f;

    private Button playButtonUI;
    private TextMeshProUGUI playText;

    private Color normalColor;
    private Color highlightedColor = Color.grey;
    private Color clickedColor = Color.blue;

    public ParticleSystem backgroundParticles;
    public GameObject transitionParticlesPrefab;

    //! SFX

    public SoundID playButtonSFX;
    public SoundSource menuAmbienceSFX; // Original Vol: 1
    public SoundSource windAmbienceSFX; // Original Vol: 1

    void Start()
    {
        transitionParticlesPrefab.SetActive(false);

        playText = playButton.GetComponentInChildren<TextMeshProUGUI>();
        playButtonUI = playButton.GetComponent<Button>();

        normalColor = playText.color;

        if (playButtonUI != null)
        {
            playButtonUI.onClick.AddListener(PlayButtonClicked);
        }
    }



    private void PlayButtonClicked()
    {
        playButtonSFX.Play();

        float fadeDuration = 1.25f;

        windAmbienceSFX.SetPitch(3f, fadeDuration);
        DOVirtual.DelayedCall(0.5f, () => windAmbienceSFX.SetVolume(2, fadeDuration)).OnComplete(
            () => DOVirtual.DelayedCall(1.25f, () =>
            {
                windAmbienceSFX.SetPitch(1f, 2f);
                windAmbienceSFX.SetVolume(0.25f, 2f);
            })
        );


        menuAmbienceSFX.SetPitch(0.95f, fadeDuration);
        DOVirtual.DelayedCall(0.25f, () => menuAmbienceSFX.SetVolume(0, fadeDuration));

        AnimateButton();
        SpawnTransitionEffect();

        SceneLoader.Instance.LoadSceneGroup(index: 1, delayInSeconds: 4f);
    }

    public void AnimateButton()
    {
        titlePanel.transform.DOMoveY(titlePanel.transform.position.y - moveDistance, animationDuration).SetEase(Ease.InOutSine);
        optionsPanel.transform.DOMoveY(optionsPanel.transform.position.y - moveDistance, animationDuration).SetEase(Ease.InOutSine);
        titlePanel.GetComponent<CanvasGroup>().DOFade(0, animationDuration).SetEase(Ease.InOutSine);
        optionsPanel.GetComponent<CanvasGroup>().DOFade(0, animationDuration).SetEase(Ease.InOutSine);

        Sequence playButtonSequence = DOTween.Sequence();

        playButtonSequence.Join(playButton.transform.DOScale(Vector3.one * scaleFactor, animationDuration / 2).SetEase(Ease.OutBack))
                          .Join(playButton.GetComponent<CanvasGroup>().DOFade(0, animationDuration + 1f).SetEase(Ease.InOutSine))
                          .Join(playText.DOColor(Color.grey, animationDuration).SetEase(Ease.InOutSine));

        if (backgroundParticles != null)
        {
            var emission = backgroundParticles.emission;
            DOTween.To(() => emission.rateOverTime.constant, x => emission.rateOverTime = x, 0, animationDuration).SetEase(Ease.InOutSine);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        playButton.transform.DOScale(Vector3.one * (scaleFactor - 0.1f), 0.1f).SetEase(Ease.OutQuad);
        playText.DOColor(clickedColor, 0.1f).SetEase(Ease.OutQuad);
    }

    private void SpawnTransitionEffect()
    {
        transitionParticlesPrefab.SetActive(true);
    }
}
