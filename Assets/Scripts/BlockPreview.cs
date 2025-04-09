using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using static BlockBehaviour;
using static DebugLoggerExtensions;

[RequireComponent(typeof(LineRenderer))]
public class BlockPreview : MonoBehaviour {
    private BlockBehaviour block;
    private Vector2Int worldSpaceCoord;
    private Direction[] movePath;
    private LineRenderer lineRenderer;

    private void Awake() {
        block = GetComponent<BlockBehaviour>();
        lineRenderer = GetComponent<LineRenderer>();
    }

    private void Start() {

        movePath = block.GetMovePath();
        UpdateLine();

        DrawPath(); //! Remove if don't want to draw preview on start 
    }

    private void UpdateLine() {
        Vector3 worldPos = block.GridRef.GetWorldSpaceFromCoord(block.coord);
        worldSpaceCoord = new Vector2Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.y));
    }

    private void DrawPath() {
        // Debug.Log("Draw Path");
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
        ClearPath();
    }

    private void AnimationCompleted() {
        // Debug.Log("Animation Completed");

        if (paused) {
            DrawPath();
        }
    }

    private bool paused = false;

    private void LevelPaused(bool paused) {
        Debug.Log(paused);

        if (paused) {
            this.paused = true;
        }
        else {
            this.paused = false;
            ClearPath();
        }
    }




}
