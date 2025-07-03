using System;
using Ami.BroAudio;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// UI script that mangages setting time scale, stepping forward and back, and autoplay functionality
/// </summary>
public class SetTimeScale : MonoBehaviour {

    [SerializeField] private bool startSelected;


    [Header("Audio")][SerializeField] private SoundID autoPlaySFX;
    [SerializeField] private SoundID stepForwardSFX;
    [SerializeField] private SoundID undoSFX;

    private void Start() {
        if (startSelected) {
            GameSettings.Instance.IsAutoPlaying = true;

            //EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }



    #region UI Button event function calls

    public void SetAutoplay(bool isAutoplay) {
        if (isAutoplay == GameSettings.Instance.IsAutoPlaying) return;

        BlockCoordinator.Instance.TogglePauseResume();

        GameSettings.Instance.IsAutoPlaying = isAutoplay;


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