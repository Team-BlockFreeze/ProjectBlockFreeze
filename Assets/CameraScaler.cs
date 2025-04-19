using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.SceneManagement;

public class CameraScaler : MonoBehaviour {
    [ReadOnly]
    [SerializeField]
    private BlockGrid gridRef;

    [SerializeField]
    private float camBlockMargin = 4f;

    private Camera cam;

    private void Start() {
        cam = GetComponent<Camera>();
        //AdjustCameraSize(10);
    }

    private Vector2 lastScreenSize = Vector2.zero;
    private Vector2 currentScreenSize = Vector2.zero;

    // Update is called once per frame
    void Update() {
        //if (gridRef != null && !gridRef.enabled) {
        //    AdjustCameraSize(10);
        //    return;
        //}
        //currentScreenSize = new Vector2(Screen.width, Screen.height);
        //if (lastScreenSize == currentScreenSize) return;

        GetCamWidth();
    }

    [Button]
    void GetCamWidth() {
        if (gridRef == null)
            gridRef = FindFirstObjectByType<BlockGrid>();



        if (gridRef == null) {

            if (SceneManager.GetActiveScene().name == "Menu" ||
                SceneManager.GetActiveScene().name == "Level Select Blocks" ||
                SceneManager.GetActiveScene().name == "Bootstrapper") {

                return;
            }

            Debug.LogWarning($"Cannot find blockgrid to determine camera size");
            return;
        }

        lastScreenSize = currentScreenSize;
        var width = gridRef.GridSize.x + camBlockMargin;

        AdjustCameraSize(width);
    }

    void AdjustCameraSize(float targetWidth) {
        if (cam == null)
            cam = GetComponent<Camera>();

        if (cam.orthographic) {
            float screenAspect = (float)Screen.width / Screen.height;

            float heightSize = (gridRef.GridSize.y + camBlockMargin * 2f) * .5f;
            cam.orthographicSize = Mathf.Max(targetWidth / (2f * screenAspect), heightSize);
        }
    }
}
