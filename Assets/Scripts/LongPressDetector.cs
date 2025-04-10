using System;
using UnityEngine;
using UnityEngine.Events;

public class LongPressDetector : MonoBehaviour {
    public const float HOLD_THRESHOLD = 0.3f;

    private float holdTimer = 0f;
    private bool isHolding = false;
    private bool longPressTriggered = false;

    private Camera mainCamera;
    private Renderer cubeRenderer;

    private BlockBehaviour block;
    private BlockPreview blockPreview;

    private bool wasTouching = false;

    // Events
    public event Action OnStartPress;
    public event Action OnStopTouching;
    public event Action OnLongPressTriggered;
    public event Action OnShortPressTriggered;

    private void LongPressTriggered() {
        OnLongPressTriggered.Invoke();
    }

    private void ShortPressTriggered() {
        OnShortPressTriggered.Invoke();
    }

    void Awake() {
        mainCamera = Camera.main;
        cubeRenderer = GetComponent<Renderer>();
        block = GetComponent<BlockBehaviour>();
        blockPreview = GetComponent<BlockPreview>();
    }

    void Update() {
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS
        bool isCurrentlyTouching = IsTouchingThisObject();

        if (isCurrentlyTouching) {
            if (!isHolding) {
                isHolding = true;
                holdTimer = 0f;
                longPressTriggered = false;

                OnStartPress.Invoke();
            }

            holdTimer += Time.deltaTime;

            if (!longPressTriggered && holdTimer >= HOLD_THRESHOLD) {
                longPressTriggered = true;
                LongPressTriggered();
            }
        }
        else {
            if (wasTouching) {
                if (isHolding && !longPressTriggered) {
                    ShortPressTriggered();
                }

                OnStopTouching.Invoke();
            }

            isHolding = false;
            holdTimer = 0f;
            longPressTriggered = false;
        }

        wasTouching = isCurrentlyTouching;
#endif
    }

    private bool IsTouchingThisObject() {
        Vector2 inputPos;

#if UNITY_EDITOR || UNITY_STANDALONE
        if (!Input.GetMouseButton(0)) return false;
        inputPos = Input.mousePosition;
#else
        if (Input.touchCount == 0) return false;
        Touch touch = Input.GetTouch(0);
        if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) return false;
        inputPos = touch.position;
#endif

        Ray ray = mainCamera.ScreenPointToRay(inputPos);

        Vector3 worldPoint;
        Plane plane = new Plane(-mainCamera.transform.forward, transform.position);
        if (plane.Raycast(ray, out float enter)) {
            worldPoint = ray.GetPoint(enter);
            return cubeRenderer.bounds.Contains(worldPoint);
        }

        return false;
    }
}
