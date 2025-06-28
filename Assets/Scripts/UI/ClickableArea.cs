using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using Ami.BroAudio;

public class ClickableArea : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {
    [Header("Input Timings")]
    [Tooltip("How long the user must hold before it's considered a hold action.")]
    [SerializeField] private float holdThreshold = 0.5f;

    private SetTimeScale uiController;
    private bool isHolding = false;
    private bool multiTouchActionTaken = false;

    private Coroutine holdCheckCoroutine;
    public SoundID autoPlaySFX;

    private void Awake() {
        uiController = GameSettings.Instance.setTimeScale;
    }

    public void OnPointerDown(PointerEventData eventData) {
        if (Input.touchCount >= 2 || Input.GetMouseButtonDown(1)) {
            multiTouchActionTaken = true;
            uiController.UndoOnce();
            return;
        }

        multiTouchActionTaken = false;
        isHolding = false;

        holdCheckCoroutine = StartCoroutine(HoldCheckRoutine());
    }

    public void OnPointerUp(PointerEventData eventData) {
        if (holdCheckCoroutine != null) {
            StopCoroutine(holdCheckCoroutine);
            holdCheckCoroutine = null;
        }

        if (multiTouchActionTaken) {
            multiTouchActionTaken = false;
            return;
        }

        if (isHolding) {
            uiController.SetAutoplay(false);
            isHolding = false;
        }
        else {
            uiController.StepForwardOnce();
        }
    }
    private IEnumerator HoldCheckRoutine() {
        yield return new WaitForSeconds(holdThreshold);

        isHolding = true;
        if (!GameSettings.Instance.IsAutoPlaying) autoPlaySFX.Play();

        uiController.SetAutoplay(true);
        holdCheckCoroutine = null;
    }
}