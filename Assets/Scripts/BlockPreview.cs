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
        lineRenderer.positionCount = 0;
    }

    private void Start() {
        movePath = block.GetMovePath();
        UpdateLine();
    }

    private void UpdateLine() {
        Vector3 worldPos = block.GridRef.GetWorldSpaceFromCoord(block.coord);
        worldSpaceCoord = new Vector2Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.y));

        DrawPath();
    }

    private void DrawPath() {
        Vector3 startPosition = new Vector3(worldSpaceCoord.x, worldSpaceCoord.y, 0);
        Vector3[] positions = new Vector3[movePath.Length + 1];
        positions[0] = startPosition;

        Vector3 currentPos = startPosition;
        for (int i = 0; i < movePath.Length; i++) {
            currentPos += GetDirectionVector(movePath[i]);
            positions[i + 1] = currentPos;
        }

        lineRenderer.positionCount = positions.Length;
        lineRenderer.SetPositions(positions);
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
        SetTimeScale.OnTimeScaleChanged += HandleTimeScaleChange;


        HandleTimeScaleChange(Time.timeScale); // Initial Case
    }

    private void OnDisable() {
        block.Event_NextMoveBegan.RemoveListener(UpdateLine);
        SetTimeScale.OnTimeScaleChanged -= HandleTimeScaleChange;

    }

    private void HandleTimeScaleChange(float newTimeScale) {
        Debug.Log("Time scale changed to " + newTimeScale);

        float targetAlpha = newTimeScale == 1f ? 1f : 0f;

        Gradient gradient = lineRenderer.colorGradient;
        GradientAlphaKey[] alphaKeys = gradient.alphaKeys;

        float currentAlpha = alphaKeys.Length > 0 ? alphaKeys[0].alpha : 1f;

        DOTween.To(() => currentAlpha, a => UpdateLineAlpha(a), targetAlpha, 0.3f);
    }

    private void UpdateLineAlpha(float alpha) {
        Gradient gradient = lineRenderer.colorGradient;
        GradientColorKey[] colorKeys = gradient.colorKeys;
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[]
        {
        new GradientAlphaKey(alpha, 0f),
        new GradientAlphaKey(alpha, 1f)
        };

        Gradient newGradient = new Gradient();
        newGradient.SetKeys(colorKeys, alphaKeys);
        lineRenderer.colorGradient = newGradient;
    }
}
