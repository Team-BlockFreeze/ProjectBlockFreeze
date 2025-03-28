using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class PlayButtonAnimation : MonoBehaviour, IPointerDownHandler
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

    public ParticleSystem backgroundParticles;  // Reference to background particle system
    public GameObject transitionParticlesPrefab; // Prefab for transition effect

    void Start()
    {
        transitionParticlesPrefab.SetActive(false);

        playText = playButton.GetComponentInChildren<TextMeshProUGUI>();
        playButtonUI = playButton.GetComponent<Button>();

        normalColor = playText.color;

        if (playButtonUI != null)
        {
            playButtonUI.onClick.AddListener(AnimateButton);
        }
    }

    public void AnimateButton()
    {
        // Move and fade out title and options panels
        titlePanel.transform.DOMoveY(titlePanel.transform.position.y - moveDistance, animationDuration).SetEase(Ease.InOutSine);
        optionsPanel.transform.DOMoveY(optionsPanel.transform.position.y - moveDistance, animationDuration).SetEase(Ease.InOutSine);
        titlePanel.GetComponent<CanvasGroup>().DOFade(0, animationDuration).SetEase(Ease.InOutSine);
        optionsPanel.GetComponent<CanvasGroup>().DOFade(0, animationDuration).SetEase(Ease.InOutSine);

        // Create sequence for play button animation
        Sequence playButtonSequence = DOTween.Sequence();

        playButtonSequence.Join(playButton.transform.DOScale(Vector3.one * scaleFactor, animationDuration / 2).SetEase(Ease.OutBack))
                          .Join(playButton.GetComponent<CanvasGroup>().DOFade(0, animationDuration + 1f).SetEase(Ease.InOutSine))
                          .Join(playText.DOColor(Color.grey, animationDuration).SetEase(Ease.InOutSine))
                          .OnComplete(SpawnTransitionEffect);  // Call transition effect after animation

        // Fade out background particles
        if (backgroundParticles != null)
        {
            var emission = backgroundParticles.emission;
            DOTween.To(() => emission.rateOverTime.constant, x => emission.rateOverTime = x, 0, animationDuration).SetEase(Ease.InOutSine);
        }
    }

    // Handle click effect when mouse button is pressed
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
