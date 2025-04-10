using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using static BlockBehaviour;
using static DebugLoggerExtensions;

[RequireComponent(typeof(LineRenderer))]
public class BlockPreview : MonoBehaviour {
    private BlockBehaviour block;
    private Vector2 worldSpaceCoord;
    private Direction[] movePath;
    private LineRenderer lineRenderer;

    // End Point
    [SerializeField] private GameObject endDotPrefab;
    private GameObject endDotInstance;

    private void Awake() {
        block = GetComponent<BlockBehaviour>();
        lineRenderer = GetComponent<LineRenderer>();
    }

    private void Start() {

        movePath = block.GetMovePath();
        UpdateLine();

        DrawPath(); //! Remove if don't want to draw preview on start 
    }

    private void FadeOutPreview(float duration) {
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



        SpriteRenderer endDotSpriteRenderer = endDotInstance.GetComponent<SpriteRenderer>();

        endDotSpriteRenderer.color = startColor;
        endDotSpriteRenderer.DOColor(endColor, duration).SetEase(easeType);
    }

    private void FadeInPreview(float duration) {
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

        SpriteRenderer endDotSpriteRenderer = endDotInstance.GetComponent<SpriteRenderer>();
        endDotSpriteRenderer.color = startColor;

        endDotSpriteRenderer.DOColor(endColor, duration).SetEase(easeType);
    }





    private void UpdateLine() {
        Vector3 worldPos = block.GridRef.GetWorldSpaceFromCoord(block.coord);
        worldSpaceCoord = new Vector2(worldPos.x, worldPos.y);
    }
    private void DrawPath() {
        int currentIndex = block.GetMoveIdx();

        Vector3 currentPos = new Vector3(worldSpaceCoord.x, worldSpaceCoord.y, 0);

        for (int i = currentIndex - 1; i >= 0; i--) {
            currentPos -= GetDirectionVector(movePath[i]);
        }

        Vector3 startPosition = currentPos;
        Vector3[] positions = new Vector3[movePath.Length + 1];
        positions[0] = startPosition;

        for (int i = 0; i < movePath.Length; i++) {
            startPosition += GetDirectionVector(movePath[i]);
            positions[i + 1] = startPosition;
        }

        lineRenderer.positionCount = positions.Length;
        lineRenderer.SetPositions(positions);


        //! Dot

        Vector3 endPos = positions[positions.Length - 1];

        if (endDotInstance == null) {
            endDotInstance = Instantiate(endDotPrefab, endPos, Quaternion.identity, transform.parent);
        }
        else {
            endDotInstance.transform.position = endPos;
        }


        //! Overriden by fade in/out
        // SpriteRenderer sr = endDotInstance.GetComponent<SpriteRenderer>();

        // Vector2Int endGridCoord = block.GridRef.GetGridCoordFromWorldPos(endPos);
        // bool isValid = block.GridRef.isValidGridCoord(endGridCoord);
        // Log(endPos);
        // Log(endGridCoord);
        // Log(isValid);

        // sr.color = isValid
        //     ? new Color(1f, 1f, 1f, 1f)           // Liquid hwhite
        //     : new Color(0.666f, 0.766f, 1, 1f);    // Sky blue

        endDotInstance.transform.localScale = Vector3.one * 0.3f;
    }


    private void ClearPath() {
        lineRenderer.positionCount = 0;
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


    private void OnEnable() {
        block.Event_NextMoveBegan.AddListener(UpdateLine);
        BlockCoordinator.Instance.OnPauseToggled += LevelPaused;
        BlockBehaviour.OnAnimationCompleted += AnimationCompleted;
        BlockBehaviour.OnAnimationStarted += AnimationStarted;
    }



    private void OnDisable() {
        block.Event_NextMoveBegan.RemoveListener(UpdateLine);
        BlockCoordinator.Instance.OnPauseToggled -= LevelPaused;
        BlockBehaviour.OnAnimationCompleted -= AnimationCompleted;
        BlockBehaviour.OnAnimationStarted -= AnimationStarted;
    }

    private void AnimationStarted() {

    }

    private void AnimationCompleted() {
        if (paused) {
            ShowPreview();
        }
    }

    private bool paused = false;

    private void LevelPaused(bool paused) {
        Log(paused);

        if (paused) {
            this.paused = true;
        }
        else {
            this.paused = false;
            HidePreview();
        }
    }

    private void RemoveEndDot() {
        endDotInstance.transform.localScale = Vector3.zero;
    }

    private void ShowPreview() {
        FadeInPreview(0.15f);
        UpdateLine();
        DrawPath();
    }

    private void HidePreview() {
        FadeOutPreview(0.15f);
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