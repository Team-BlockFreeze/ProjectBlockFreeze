using Sirenix.OdinInspector;
using UnityEngine;

public class LevelSelectButtonFloor : MonoBehaviour {


    [SerializeField] private LevelArea levelArea;
    [SerializeField] private GameObject FloorTilePrefab_1x1;
    [SerializeField] private float zOffset = 6.25f;

    [Button(ButtonSizes.Large)]
    [GUIColor(0.4f, 0.8f, 1f)]
    public void GenerateFloor() {
        if (levelArea == null) {
            Debug.LogError("LevelArea is not assigned.", this);
            return;
        }
        if (FloorTilePrefab_1x1 == null) {
            Debug.LogError("FloorTilePrefab_1x1 is not assigned.", this);
            return;
        }

        // Ensure matrix is up to date
        levelArea.RebuildMatrixFromScene();

        var buttonMatrix = levelArea.buttonMatrix;
        if (buttonMatrix == null || buttonMatrix.Length == 0) {
            Debug.LogWarning("LevelArea button matrix is empty. Cannot generate floor.", levelArea);
            return;
        }

        // Clean Up Previous
        Transform existingContainer = transform.Find("FloorContainer");
        if (existingContainer != null) {
            DestroyImmediate(existingContainer.gameObject);
        }

        var floorContainer = new GameObject("FloorContainer");
        floorContainer.transform.SetParent(this.transform, false);

        int width = buttonMatrix.GetLength(0);
        int height = buttonMatrix.GetLength(1);

        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                LevelButton currentButton = buttonMatrix[x, y];

                // Skip empty cells
                if (currentButton == null) {
                    continue;
                }

                Vector3 tilePosition = currentButton.transform.position;
                tilePosition.z += zOffset;

                Instantiate(FloorTilePrefab_1x1, tilePosition, Quaternion.identity, floorContainer.transform);

                // if (x + 1 < width) {
                //     LevelButton rightNeighbor = buttonMatrix[x + 1, y];
                //     if (rightNeighbor != null) {
                //         Vector3 midPoint = (currentButton.transform.position + rightNeighbor.transform.position) / 2f;
                //         midPoint.z += zOffset;
                //         Instantiate(FloorTilePrefab_1x1, midPoint, Quaternion.identity, floorContainer.transform);
                //     }
                // }

                // if (y + 1 < height) {
                //     LevelButton downNeighbor = buttonMatrix[x, y + 1];
                //     if (downNeighbor != null) {
                //         Vector3 midPoint = (currentButton.transform.position + downNeighbor.transform.position) / 2f;
                //         midPoint.z += zOffset;
                //         Instantiate(FloorTilePrefab_1x1, midPoint, Quaternion.identity, floorContainer.transform);
                //     }
                // }
            }
        }

        Debug.Log($"Successfully generated floor with tiles.", this);
    }
}