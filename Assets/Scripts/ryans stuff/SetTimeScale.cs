using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class SetTimeScale : MonoBehaviour {
    [SerializeField]
    private bool startSelected = false;

    public static event Action<float> OnTimeScaleChanged;

    private void Start() {
        if (startSelected)
            EventSystem.current.SetSelectedGameObject(gameObject);
    }

    [SerializeField]
    private float timeScaleToSet = 1f;
    public void DoSetTimeScale() {
        Time.timeScale = timeScaleToSet;
        OnTimeScaleChanged?.Invoke(timeScaleToSet);
        EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void TogglePause() {
        BlockCoordinator.Instance.TogglePauseResume();
    }

    public void StepForwardOnce() {
        BlockCoordinator.Instance.StepForwardOnce();
    }
}
