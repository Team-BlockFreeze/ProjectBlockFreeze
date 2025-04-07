using Ami.BroAudio;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SetTimeScale : MonoBehaviour {
    [SerializeField]
    private bool startSelected = false;

    [SerializeField]
    private bool soloSelectToggle = false;

    public static event Action<float> OnTimeScaleChanged;

    private Color defaultCol;
    private Color selectedCol;

    private bool selected;

    private Button button;

    private static UnityEvent Event_TimeScaleButtonPressed = new UnityEvent();

    [Header("Audio")]
    [SerializeField]
    private SoundID soundFX;

    private void OnEnable() {
        if (soloSelectToggle) return;
        Event_TimeScaleButtonPressed.AddListener(UnselectButton);
    }

    private void OnDisable() {
        if (soloSelectToggle) return;
        Event_TimeScaleButtonPressed.RemoveListener(UnselectButton);
    }

    private void UnselectButton() {
        SetColor(false);
    }

    private void Start() {
        button = GetComponent<Button>();

        defaultCol = button.colors.normalColor;
        selectedCol = new Color(defaultCol.r, defaultCol.g * .5f, defaultCol.b * .5f);

        if (startSelected) {
            selected = true;
            SetColor();

            //EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }

    private void SetColor(bool? forceState = null) {
        if (forceState.HasValue)
            selected = forceState.Value;

        var colors = button.colors;
        colors.normalColor = selected? selectedCol : defaultCol;
        button.colors = colors;
    }

    [SerializeField]
    private float timeScaleToSet = 1f;
    public void DoSetTimeScale() {
        Time.timeScale = timeScaleToSet;
        OnTimeScaleChanged?.Invoke(timeScaleToSet);
        Event_TimeScaleButtonPressed?.Invoke();
        SetColor(true);

        EventSystem.current.SetSelectedGameObject(null);
    }

    public void TogglePause() {
        BlockCoordinator.Instance.TogglePauseResume();

        selected = !selected;
        SetColor();

        if(!selected) soundFX.Play();

        EventSystem.current.SetSelectedGameObject(null);
    }

    public void StepForwardOnce() {
        if (!BlockCoordinator.Instance.IsPaused)
            return;

        if(BlockCoordinator.Instance.StepForwardOnce())
            soundFX.Play();

        EventSystem.current.SetSelectedGameObject(null);
    }
}
