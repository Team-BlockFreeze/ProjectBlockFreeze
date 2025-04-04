using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.SceneManagement;

public class CameraScaler : MonoBehaviour {
    [ReadOnly]
    [SerializeField]
    private BlockGrid gridRef;

    [SerializeField]
    private float widthMargin = 4f;

    private Camera cam;

    private void Start() {
        cam = GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update() {
        GetCamWidth();
    }

    [Button]
    void GetCamWidth() {
        if (gridRef == null)
            gridRef = FindFirstObjectByType<BlockGrid>();



        if (gridRef == null) {

            if (SceneManager.GetActiveScene().name == "Menu" ||
                SceneManager.GetActiveScene().name == "LevelSelect" ||
                SceneManager.GetActiveScene().name == "Bootstrapper") {

                return;
            }

            Debug.LogWarning($"Cannot find blockgrid to determine camera size");
            return;
        }

        var width = gridRef.GridSize.x + widthMargin;

        AdjustCameraSize(width);
    }

    void AdjustCameraSize(float targetWidth) {
        if (cam == null)
            cam = GetComponent<Camera>();

        if (cam.orthographic) {
            float screenAspect = (float)Screen.width / Screen.height;
            cam.orthographicSize = targetWidth / (2f * screenAspect);
        }
    }
}
