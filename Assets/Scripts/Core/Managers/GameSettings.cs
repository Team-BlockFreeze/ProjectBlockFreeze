public class GameSettings : PersistentSingleton<GameSettings>
{
    public float gameTickInSeconds = 1f;
    public bool drawPreviewLinesOnStart;
    public bool showAllPreviewLinesOnPause;
    public bool togglePreviewLine;

    // Status vars
    public bool IsAutoPlaying { get; set; }
}