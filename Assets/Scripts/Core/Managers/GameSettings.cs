using Sirenix.OdinInspector;
using UnityEngine;

public class GameSettings : PersistentSingleton<GameSettings> {
    public float gameTickInSeconds = 1f;
    public bool drawPreviewLinesOnStart;
    public bool showAllPreviewLinesOnPause;
    public bool togglePreviewLine;

    // Status vars

    [ReadOnly]
    public bool isAutoPlaying;
    public bool IsAutoPlaying { get => isAutoPlaying; set => isAutoPlaying = value; }

    public SetTimeScale setTimeScale;
    [Range(0f, 2f)]
    public float reloadAnimationTime = 1f;

    protected override void Awake() {
        base.Awake();
        setTimeScale = GetComponent<SetTimeScale>();
    }
}