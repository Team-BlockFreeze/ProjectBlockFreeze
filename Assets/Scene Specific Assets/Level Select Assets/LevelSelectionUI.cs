using Ami.BroAudio;
using UnityEngine;
using UnityEngine.UI;

public class LevelSelectionUI : MonoBehaviour
{
    [SerializeField] private Sprite completedBorder;
    [SerializeField] private Sprite notCompletedBorder;
    [SerializeField] private Sprite lockedBorder;
    [SerializeField] private Sprite currentLevelBorder;
    [SerializeField] private Transform buttonsParent;

    public SoundID levelPressedSFX;



    private void Start()
    {
        AssignButtonListeners();
    }

    private void AssignButtonListeners()
    {
        if (buttonsParent == null)
        {
            Debug.LogError("Buttons parent not assigned!");
            return;
        }

        int levelIndex = 1; // Starts from L1
        foreach (Transform child in buttonsParent)
        {
            Button button = child.GetComponent<Button>();
            if (button != null)
            {
                int index = levelIndex;
                button.onClick.AddListener(() => OnLevelButtonPressed(index));
                levelIndex++;
            }
        }
    }

    private void OnLevelButtonPressed(int levelIndex)
    {
        Debug.Log($"Level {levelIndex} button pressed.");
        // TODO: Add animations, particles, SFX, transitions
    }
}
