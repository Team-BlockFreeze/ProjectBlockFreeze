using System;
using Sirenix.OdinInspector;
using UnityEngine;

public class DynamicGridBorder : MonoBehaviour {
    [Tooltip("The prefab to use for each floor tile.")]
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private float zPos = 0f;
    [SerializeField] private float zScale = 12f;

    private GameObject floorContainer;

    /// <summary>
    /// DEPRECATED: Original method that looks kinda shit. Stretches a single object.
    /// </summary>
    public void SetGridSizeStretch(Vector2Int gridSize, int borderSize) {
        if (tilePrefab) {
            tilePrefab.transform.localScale = new Vector3(gridSize.x + borderSize * 2, gridSize.y + borderSize * 2, tilePrefab.transform.localScale.z);
        }
    }

    [Button("Test Generate")]
    private void TestGenerate() {
        SetGridSize(new Vector2Int(5, 16), 1);
    }



    /// <summary>
    /// Generates a tiled floor with greedy algorithm.
    /// </summary>
    /// <param name="gridSize">Sisze of inner game grid.</param>
    /// <param name="borderSize">Size of border around grid.</param>
    [Button("Generate Floor")]
    public void SetGridSize(Vector2Int gridSize, float borderSize) {
        // Force only 1 instance of floorContainer
        if (floorContainer != null) {
            Transform parent = floorContainer.transform.parent;

            // Destroy the main reference
            if (Application.isPlaying)
                Destroy(floorContainer);
            else
                DestroyImmediate(floorContainer);

            // Dirty fix to destroy any other instances
            if (parent != null) {
                foreach (Transform child in parent) {
                    if (child.name == "floorContainer") {
                        if (Application.isPlaying)
                            Destroy(child.gameObject);
                        else
                            DestroyImmediate(child.gameObject);
                    }
                }
            }
        }

        floorContainer = new GameObject("FloorContainer");
        floorContainer.transform.SetParent(this.transform, false);

        if (tilePrefab == null) {
            Debug.LogError("Tile Prefab is not assigned in the inspector!");
            return;
        }

        // Floor Dimensions
        float totalWidth = gridSize.x + borderSize * 2;
        float totalHeight = gridSize.y + borderSize * 2;

        if (totalWidth <= 0 || totalHeight <= 0) {
            Debug.LogWarning("Grid size and border result in a zero or negative area. No floor generated.");
            return;
        }

        //! --- Greedily Find Optimal Chunk Size ---
        float largerSide = Mathf.Max(totalWidth, totalHeight);
        float smallerSide = Mathf.Min(totalWidth, totalHeight);

        float closestDifference = float.MaxValue;
        float bestScaleFactor = largerSide;

        for (int i = 1; i <= Mathf.CeilToInt(largerSide); i++) {
            float currentScaleFactor = largerSide / i;
            float difference = Mathf.Abs(currentScaleFactor - smallerSide);

            if (difference < closestDifference) {
                closestDifference = difference;
                bestScaleFactor = currentScaleFactor;
            }
        }

        if (bestScaleFactor <= 0) bestScaleFactor = 1;

        int numChunksX = Mathf.CeilToInt(totalWidth / bestScaleFactor);
        int numChunksY = Mathf.CeilToInt(totalHeight / bestScaleFactor);

        float actualChunkWidth = totalWidth / numChunksX;
        float actualChunkHeight = totalHeight / numChunksY;

        float startX = -totalWidth / 2f + actualChunkWidth / 2f;
        float startY = -totalHeight / 2f + actualChunkHeight / 2f;

        for (int y = 0; y < numChunksY; y++) {
            for (int x = 0; x < numChunksX; x++) {
                Vector3 tilePosition = new Vector3(
                    startX + x * actualChunkWidth,
                    startY + y * actualChunkHeight,
                    zPos
                );

                GameObject tileInstance = Instantiate(tilePrefab, floorContainer.transform);

                tileInstance.transform.localPosition = tilePosition;
                tileInstance.transform.localScale = new Vector3(
                    actualChunkWidth,
                    actualChunkHeight,
                    zScale
                );
            }
        }
    }
}