using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using static BlockBehaviour;

[RequireComponent(typeof(LineRenderer))]
public class BlockPreview : LoggerMonoBehaviour {
    private BlockBehaviour block;
    private Vector2 worldSpaceCoord;
    private Direction[] movePath;
    private LineRenderer lineRenderer;

    // End Point
    [SerializeField] private GameObject endDotPrefab;
    private GameObject? endDotInstance;
    public GameObject? GetEndDotInstance() {
        return endDotInstance;
    }


    private LongPressDetector longPressDetector;
    private const float fadeDuration = 0.1f;



    private void OnShortPressTriggered() {
        block.TrySetFreeze();
    }

    private void OnLongPressTriggered() {
        if (GameSettings.Instance.togglePreviewLine) {
            ToggleFadePreview(fadeDuration);
            return;
        }

        FadeInPreview(fadeDuration);
    }

    private void OnStopTouching() {
        if (!GameSettings.Instance.togglePreviewLine) {
            FadeOutPreview(fadeDuration);
        }
    }

    private void OnStartPress() {

    }



    private void OnEnable() {
        block.Event_NextMoveBegan.AddListener(UpdateLine);
        BlockCoordinator.Instance.OnPauseToggled += LevelPaused;
        BlockCoordinator.Instance.OnStepForward += OnStepForward;
        BlockBehaviour.OnAnimationCompleted += AnimationCompleted;
        BlockBehaviour.OnAnimationStarted += AnimationStarted;


        if (longPressDetector != null) {
            longPressDetector.OnStartPress += OnStartPress;
            longPressDetector.OnStopTouching += OnStopTouching;
            longPressDetector.OnLongPressTriggered += OnLongPressTriggered;
            longPressDetector.OnShortPressTriggered += OnShortPressTriggered;
        }
    }



    private void OnDisable() {
        block.Event_NextMoveBegan.RemoveListener(UpdateLine);
        BlockCoordinator.Instance.OnPauseToggled -= LevelPaused;
        BlockCoordinator.Instance.OnStepForward -= OnStepForward;
        BlockBehaviour.OnAnimationCompleted -= AnimationCompleted;
        BlockBehaviour.OnAnimationStarted -= AnimationStarted;


        if (longPressDetector != null) {
            longPressDetector.OnStartPress -= OnStartPress;
            longPressDetector.OnStopTouching -= OnStopTouching;
            longPressDetector.OnLongPressTriggered -= OnLongPressTriggered;
            longPressDetector.OnShortPressTriggered -= OnShortPressTriggered;
        }
    }

    private void Awake() {
        longPressDetector = GetComponent<LongPressDetector>();
        block = GetComponent<BlockBehaviour>();
        lineRenderer = GetComponent<LineRenderer>();
    }

    private void Start() {
        movePath = block.GetMovePath();
        UpdateLine();

        if (GameSettings.Instance.drawPreviewLinesOnStart) DrawPath();
    }

    public void ToggleFadePreview(float duration) {
        if (previewShown) {
            previewShown = false;
        }
        else {
            previewShown = true;
        }

        if (endDotInstance == null) return;

        SpriteRenderer endDotSpriteRenderer = endDotInstance.GetComponent<SpriteRenderer>();
        Color currentColor = endDotSpriteRenderer.color;

        if (currentColor.a == 0f) {
            FadeInPreview(duration);
        }
        else {
            FadeOutPreview(duration);
        }
    }


    public void FadeOutPreview(float duration) {
        previewShown = false;

        Ease easeType = Ease.Linear;

        Color startColor = new Color(1f, 1f, 1f, 1f);
        Color endColor = new Color(1f, 1f, 1f, 0f);

        DOTween.To(
            () => lineRenderer.startColor,
            color => {
                lineRenderer.startColor = color;
                lineRenderer.endColor = color;
            },
            endColor,
            duration
        ).SetEase(easeType);



        if (endDotInstance != null) {
            SpriteRenderer endDotSpriteRenderer = endDotInstance.GetComponent<SpriteRenderer>();
            endDotSpriteRenderer.DOColor(endColor, duration).SetEase(easeType);
        }
    }

    private bool previewShown = false;

    public void FadeInPreview(float duration) {
        previewShown = true;

        Ease easeType = Ease.Linear;

        Color startColor = new Color(1f, 1f, 1f, 0f);
        Color endColor = new Color(1f, 1f, 1f, 1f);

        DOTween.To(
            () => lineRenderer.startColor,
            color => {
                lineRenderer.startColor = color;
                lineRenderer.endColor = color;
            },
            endColor,
            duration
        ).SetEase(easeType);

        if (endDotInstance != null) {
            SpriteRenderer endDotSpriteRenderer = endDotInstance.GetComponent<SpriteRenderer>();
            endDotSpriteRenderer.DOColor(endColor, duration).SetEase(easeType);
        }
    }


    public void UpdateLine() {
        Vector3 worldPos = block.GridRef.GetWorldSpaceFromCoord(block.coord);
        worldSpaceCoord = new Vector2(worldPos.x, worldPos.y);
    }

    public void DrawPath() {
        int currentIndex = block.GetMoveIdx();
        Vector3 currentPos = new Vector3(worldSpaceCoord.x, worldSpaceCoord.y, 0);

        List<Vector3> positions = new List<Vector3>();
        positions.Add(currentPos);

        if (block.moveMode == BlockMoveState.pingpong) {
            bool goingForward = block.GetPingpongIsForward();

            positions.Clear();
            positions.Add(currentPos);

            Vector3 backPos = currentPos;
            if (goingForward) {
                for (int i = currentIndex - 1; i >= 0; i--) {
                    backPos -= GetDirectionVector(movePath[i]);
                    positions.Insert(0, backPos);
                }
            }
            else {
                for (int i = currentIndex + 1; i < movePath.Length; i++) {
                    backPos -= GetDirectionVector(GetOppositeDir(movePath[i]));
                    positions.Insert(0, backPos);
                }
            }

            Vector3 forwardPos = currentPos;
            if (goingForward) {
                for (int i = currentIndex; i < movePath.Length; i++) {
                    forwardPos += GetDirectionVector(movePath[i]);
                    positions.Add(forwardPos);
                }
            }
            else {
                for (int i = currentIndex; i >= 0; i--) {
                    forwardPos += GetDirectionVector(GetOppositeDir(movePath[i]));
                    positions.Add(forwardPos);
                }
            }
        }

        else if (block.moveMode == BlockMoveState.loop) {
            Vector3 backPos = currentPos;
            for (int i = currentIndex - 1; i >= 0; i--) {
                backPos -= GetDirectionVector(movePath[i]);
            }

            Vector3 startPosition = backPos;
            positions.Clear();
            positions.Add(startPosition);

            for (int i = 0; i < movePath.Length; i++) {
                startPosition += GetDirectionVector(movePath[i]);
                positions.Add(startPosition);
            }
        }

        lineRenderer.positionCount = positions.Count;
        lineRenderer.SetPositions(positions.ToArray());

        Vector3 endPos = positions[positions.Count - 1];

        if (endDotInstance == null) {
            endDotInstance = Instantiate(endDotPrefab, endPos, Quaternion.identity, transform.parent);
        }
        else {
            endDotInstance.transform.position = endPos;
        }

        endDotInstance.transform.localScale = Vector3.one * 0.3f;
    }


    private Direction GetOppositeDir(Direction dir) {
        switch (dir) {
            case Direction.left: return Direction.right;
            case Direction.right: return Direction.left;
            case Direction.up: return Direction.down;
            case Direction.down: return Direction.up;
            default: return dir;
        }
    }



    private Vector3 GetDirectionVector(Direction dir) {
        switch (dir) {
            case Direction.left: return Vector3.left;
            case Direction.right: return Vector3.right;
            case Direction.up: return Vector3.up;
            case Direction.down: return Vector3.down;
            default: return Vector3.zero;
        }
    }


    private void OnStepForward() {
        UpdateLine();
    }


    private void AnimationStarted() {
    }

    private void AnimationCompleted() {
        DrawPath();
        if (paused) {
            if (GameSettings.Instance.showAllPreviewLinesOnPause) {
                ShowPreview();
            }

        }
    }

    private bool paused = false;

    private void LevelPaused(bool paused) {
        if (!paused) {
            this.paused = false;
            HidePreview();
        }
        else {
            this.paused = true;
            if (previewShown) {
                HidePreview();
            }
        }
    }


    //! Show preview FROM not shown
    [Button]
    public void ShowPreview() {
        FadeInPreview(fadeDuration);
        UpdateLine();
    }

    //! Hide preview FROM shown
    [Button]
    public void HidePreview() {
        FadeOutPreview(fadeDuration);
    }

}




//! Extra smooth pathing
// private void DrawPath() {
//     int currentIndex = block.GetMoveIdx();
//     Vector3 currentPos = new Vector3(worldSpaceCoord.x, worldSpaceCoord.y, 0);

//     for (int i = currentIndex - 1; i >= 0; i--) {
//         currentPos -= GetDirectionVector(movePath[i]);
//     }

//     List<Vector3> smoothPositions = new List<Vector3>();
//     smoothPositions.Add(currentPos);

//     int segmentsPerStep = 8;

//     for (int i = 0; i < movePath.Length; i++) {
//         Vector3 direction = GetDirectionVector(movePath[i]);
//         Vector3 stepStart = currentPos;
//         Vector3 stepEnd = stepStart + direction;

//         for (int j = 1; j <= segmentsPerStep; j++) {
//             float t = j / (float)segmentsPerStep;
//             Vector3 interpolated = Vector3.Lerp(stepStart, stepEnd, t);
//             smoothPositions.Add(interpolated);
//         }

//         currentPos = stepEnd;
//     }

//     lineRenderer.positionCount = smoothPositions.Count;
//     lineRenderer.SetPositions(smoothPositions.ToArray());
// }