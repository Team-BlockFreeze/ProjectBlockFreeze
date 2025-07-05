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


    private bool pressStartedOnThisObject = false;

    private void Update() {
        bool inputBegan = false;
        bool inputHeld = false;
        bool inputEnded = false;
        Vector2 inputPosition = Vector2.zero;

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
        if (Input.GetMouseButtonDown(0)) inputBegan = true;
        if (Input.GetMouseButton(0)) inputHeld = true;
        if (Input.GetMouseButtonUp(0)) inputEnded = true;
        if (inputHeld) inputPosition = Input.mousePosition;

#elif UNITY_ANDROID || UNITY_IOS
        if (Input.touchCount > 0) {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began) inputBegan = true;
            if (touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled) inputHeld = true;
            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) inputEnded = true;
            if (inputHeld) inputPosition = touch.position;
        }
#endif

        if (inputBegan) {
            if (IsPositionOverObject(inputPosition)) {
                pressStartedOnThisObject = true;
                isHolding = true;
                longPressTriggered = false;
                holdTimer = 0f;
                OnStartPress?.Invoke();
            }
        }

        if (inputHeld && pressStartedOnThisObject) {
            holdTimer += Time.deltaTime;

            if (!longPressTriggered && holdTimer >= HOLD_THRESHOLD) {
                longPressTriggered = true;
                OnLongPressTriggered?.Invoke();
            }
        }

        if (inputEnded && pressStartedOnThisObject) {
            if (!longPressTriggered) {
                OnShortPressTriggered?.Invoke();
            }

            OnStopTouching?.Invoke();
            isHolding = false;
            pressStartedOnThisObject = false;
            longPressTriggered = false;
            holdTimer = 0f;
        }
    }
    private bool IsPositionOverObject(Vector2 screenPosition) {
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);

        Plane plane = new Plane(-mainCamera.transform.forward, transform.position);
        if (plane.Raycast(ray, out float enter)) {
            Vector3 worldPoint = ray.GetPoint(enter);
            return cubeRenderer.bounds.Contains(worldPoint);
        }

        return false;
    }


    private bool IsInputJustBegan() {
#if UNITY_EDITOR || UNITY_STANDALONE
        return Input.GetMouseButtonDown(0);
#else
    if (Input.touchCount > 0)
        return Input.GetTouch(0).phase == TouchPhase.Began;
    return false;
#endif
    }

    private bool IsInputHeld() {
#if UNITY_EDITOR || UNITY_STANDALONE
        return Input.GetMouseButton(0);
#else
    return Input.touchCount > 0 && 
           Input.GetTouch(0).phase != TouchPhase.Ended &&
           Input.GetTouch(0).phase != TouchPhase.Canceled;
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
