using UnityEngine;

public class GameSettings : PersistentSingleton<GameSettings> {
    public float gameTickInSeconds = 1f;
    public bool drawPreviewLinesOnStart = false;
    public bool showAllPreviewLinesOnPause = false;
    public bool togglePreviewLine = false;

}
