using UnityEngine;

public class ChangeIngameSettings : MonoBehaviour {
    public void TogglePreviews() {
        GameSettings.Instance.togglePreviewLine = !GameSettings.Instance.togglePreviewLine;
    }

    public void ShowPreviewsOnPause() {
        GameSettings.Instance.showAllPreviewLinesOnPause = !GameSettings.Instance.showAllPreviewLinesOnPause;
    }

    public void SetBlockSpeed(float speedNormalized) {
        float min = 0.2f;
        float max = 0.5f;
        float targetSpeed = Mathf.Lerp(max, min, speedNormalized);

        Debug.Log($"Setting block speed to {targetSpeed}");
        GameSettings.Instance.gameTickInSeconds = targetSpeed;
    }

    public void SetSoundVolume(float volume) {
        Debug.Log("Set sound volume to " + volume);
        AudioListener.volume = volume;
    }

    public void SetMusicVolume(float volume) {
        Debug.Log("Set music volume to " + volume);
        //TODO: Assign music ocntroller
    }
}
