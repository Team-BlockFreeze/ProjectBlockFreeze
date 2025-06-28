using System;
using Ami.BroAudio;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SetTimeScale : MonoBehaviour {
    private static readonly UnityEvent Event_TimeScaleButtonPressed = new();

    [SerializeField] private bool startSelected;

    [SerializeField] private bool soloSelectToggle;

    [Header("Audio")][SerializeField] private SoundID autoPlaySFX;
    [SerializeField] private SoundID stepForwardSFX;
    [SerializeField] private SoundID undoSFX;

    [SerializeField] private float timeScaleToSet = 1f;

    private Button button;

    private Color defaultCol;

    private Color selectedCol;

    private void Awake() {
        UndoOnce();
    }

    private void Start() {
        button = GetComponent<Button>();

        defaultCol = button.colors.normalColor;
        selectedCol = new Color(defaultCol.r, defaultCol.g * .5f, defaultCol.b * .5f);

        if (startSelected) {
            GameSettings.Instance.IsAutoPlaying = true;

            SetColor();

            //EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }


    private void OnEnable() {
        if (soloSelectToggle) return;
        Event_TimeScaleButtonPressed.AddListener(UnselectButton);

    }

    private void OnDisable() {
        if (soloSelectToggle) return;
        Event_TimeScaleButtonPressed.RemoveListener(UnselectButton);

    }

    public static event Action<float> OnTimeScaleChanged;

    private void UnselectButton() {
        SetColor(false);
    }

    private void SetColor(bool? forceState = null) {
        if (forceState.HasValue)
            GameSettings.Instance.IsAutoPlaying = forceState.Value;

        var colors = button.colors;
        colors.normalColor = GameSettings.Instance.IsAutoPlaying ? selectedCol : defaultCol;
        button.colors = colors;
    }

    public void DoSetTimeScale() {
        Time.timeScale = timeScaleToSet;
        OnTimeScaleChanged?.Invoke(timeScaleToSet);
        Event_TimeScaleButtonPressed?.Invoke();
        SetColor(true);

        EventSystem.current.SetSelectedGameObject(null);
    }



    #region UI Button event function calls

    public void SetAutoplay(bool isAutoplay) {
        if (isAutoplay == GameSettings.Instance.IsAutoPlaying) return;

        BlockCoordinator.Instance.TogglePauseResume();

        GameSettings.Instance.IsAutoPlaying = isAutoplay;
        SetColor();


        var pauseSprite = transform.Find("PauseButton");
        var playSprite = transform.Find("PlayButton");

        if (pauseSprite != null)
            pauseSprite.gameObject.SetActive(GameSettings.Instance.IsAutoPlaying); // Show when unpaused
        if (playSprite != null)
            playSprite.gameObject.SetActive(!GameSettings.Instance.IsAutoPlaying); // Show when paused

        EventSystem.current.SetSelectedGameObject(null);
    }

    public void ClickableArea() {
        Debug.Log("Clickable Area");
    }

    public void TogglePause() {
        BlockCoordinator.Instance.TogglePauseResume();

        GameSettings.Instance.IsAutoPlaying = !GameSettings.Instance.IsAutoPlaying;
        SetColor();

        if (!GameSettings.Instance.IsAutoPlaying) autoPlaySFX.Play();

        var pauseSprite = transform.Find("PauseButton");
        var playSprite = transform.Find("PlayButton");

        if (pauseSprite != null)
            pauseSprite.gameObject.SetActive(GameSettings.Instance.IsAutoPlaying); // Show when unpaused
        if (playSprite != null)
            playSprite.gameObject.SetActive(!GameSettings.Instance.IsAutoPlaying); // Show when paused

        EventSystem.current.SetSelectedGameObject(null);
    }



    public void StepForwardOnce() {
        if (!BlockCoordinator.Instance.IsPaused)
            return;


        if (BlockCoordinator.Instance.StepForwardWithUndo()) stepForwardSFX.Play();

        EventSystem.current.SetSelectedGameObject(null);
    }

    public void UndoOnce() {
        BlockCoordinator.Instance.UndoLastStep();
        undoSFX.Play();

        EventSystem.current.SetSelectedGameObject(null);
    }

    #endregion
}