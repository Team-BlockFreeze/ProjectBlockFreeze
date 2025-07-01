using Ami.BroAudio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class DebugPlayerControls : MonoBehaviour {

    [SerializeField] private SoundID soundFX;
    [SerializeField] private SoundID autoPlaySFX;
    [SerializeField] private float holdThreshold = 0.5f;

    private InputSystem_Actions debugControls;

    private float holdTime;
    private bool isHoldingStep;
    private bool autoplayTriggered;

    private SetTimeScale uiController;

    private void Awake() {
        uiController = GameSettings.Instance.setTimeScale;
    }

    private void OnEnable() {
        debugControls = new InputSystem_Actions();

        debugControls.Player.StepForward.started += OnStepForwardStarted;
        debugControls.Player.StepForward.canceled += OnStepForwardCanceled;
        debugControls.Player.Undo.started += _ => UndoOnce();

        debugControls.Player.Enable();
    }

    private void OnDisable() {
        debugControls.Player.StepForward.started -= OnStepForwardStarted;
        debugControls.Player.StepForward.canceled -= OnStepForwardCanceled;
        debugControls.Player.Undo.started -= _ => UndoOnce();

        debugControls.Player.Disable();
    }

    private void Update() {
        if (isHoldingStep && !autoplayTriggered) {
            holdTime += Time.unscaledDeltaTime;
            if (holdTime >= holdThreshold) {
                autoplayTriggered = true;

                if (!GameSettings.Instance.IsAutoPlaying)
                    autoPlaySFX.Play();

                uiController.SetAutoplay(true);
            }
        }
    }

    private void OnStepForwardStarted(InputAction.CallbackContext ctx) {
        isHoldingStep = true;
        holdTime = 0f;
        autoplayTriggered = false;
    }

    private void OnStepForwardCanceled(InputAction.CallbackContext ctx) {
        if (autoplayTriggered) {
            uiController.SetAutoplay(false);
        }
        else {
            StepForwardOnce();
        }

        isHoldingStep = false;
        holdTime = 0f;
        autoplayTriggered = false;
    }

    public void StepForwardOnce() {
        if (!BlockCoordinator.Instance.IsPaused)
            return;

        if (BlockCoordinator.Instance.StepForwardWithUndo()) {
            soundFX.Play();
        }

        EventSystem.current.SetSelectedGameObject(null);
    }

    public void UndoOnce() {
        BlockCoordinator.Instance.UndoLastStep();
        soundFX.Play();
        EventSystem.current.SetSelectedGameObject(null);
    }
}
