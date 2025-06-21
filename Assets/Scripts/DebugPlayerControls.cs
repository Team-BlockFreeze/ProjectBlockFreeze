using Ami.BroAudio;
using UnityEngine;
using UnityEngine.EventSystems;

public class DebugPlayerControls : MonoBehaviour {

    [SerializeField] private SoundID soundFX;

    private InputSystem_Actions debugControls;

    private void OnEnable() {
        debugControls = new InputSystem_Actions();
        debugControls.Player.StepForward.started += _ => StepForwardOnce();
        debugControls.Player.Undo.started += _ => UndoOnce();
        debugControls.Player.Enable();
    }

    private void OnDisable() {
        debugControls.Player.StepForward.started -= _ => StepForwardOnce();
        debugControls.Player.Undo.started -= _ => UndoOnce();
        debugControls.Player.Disable();
    }

    public void StepForwardOnce() {
        if (!BlockCoordinator.Instance.IsPaused)
            return;

        if (BlockCoordinator.Instance.StepForwardWithUndo()) soundFX.Play();

        EventSystem.current.SetSelectedGameObject(null);
    }

    public void UndoOnce() {
        BlockCoordinator.Instance.UndoLastStep();

        soundFX.Play();

        EventSystem.current.SetSelectedGameObject(null);
    }

}
