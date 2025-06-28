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


        GameSettings.Instance.gameTickInSeconds = Mathf.Lerp(min, max, speedNormalized);
    }

    public void SetSoundVolume(float volume) {
        AudioListener.volume = volume;
    }

    public void SetMusicVolume(float volume) {
        Debug.Log("Set music volume to " + volume);
        //TODO: Assign music ocntroller
    }
}
