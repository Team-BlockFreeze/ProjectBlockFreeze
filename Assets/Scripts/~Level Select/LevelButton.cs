using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using Systems.SceneManagement;
using TMPro;
using UnityEngine;

[SelectionBase] //! When selecting things in the scene view, it will select the parent object and not the stuff inside
public class LevelButton : LoggerMonoBehaviour {
    [BoxGroup("Level Info")]
    [SerializeField]
    private LevelDataSO level;

    [BoxGroup("Grid Info")]
    [ReadOnly]
    [SerializeField]
    private Vector2Int gridPosition;

    [BoxGroup("Grid Info")]
    [SerializeField]
    private bool isUnlocked;

    [BoxGroup("Grid Info")]
    [SerializeField]
    private bool isCompleted;

    [BoxGroup("UI References")]
    [Required]
    [SerializeField]
    private TMP_Text levelNumberText;

    public LevelDataSO Level {
        get => level;
        set {
            level = value;

            if (value != null) {
                // TODO: parse level name better if needed
                var levelNum = value.name;

                if (levelNum.Length == 2)
                    levelNum = levelNum.Insert(1, "0");

                levelNum = levelNum.Insert(1, "-");

                if (levelNumberText != null)
                    levelNumberText.text = levelNum;
            }
        }
    }

    public Vector2Int GridPosition {
        get => gridPosition;
        set => gridPosition = value;
    }

    public bool IsUnlocked {
        get => isUnlocked;
        set => isUnlocked = value;
    }

    public bool IsCompleted {
        get => isCompleted;
        set => isCompleted = value;
    }

    [SerializeField] private Material completedColor;
    [SerializeField] private Material unlockedColor;
    [SerializeField] private Material lockedColor;

    public void UpdateVisuals() {
        if (isCompleted) { // Completed Lvl

            if (completedColor != null) {
                meshRenderer.material = completedColor;
            }
            return;
        }


        if (isUnlocked) { // Unlocked Lvl
            if (unlockedColor != null) {
                meshRenderer.material = unlockedColor;
            }
        }
        else { // Locked lvl
            if (lockedColor != null) {
                meshRenderer.material = lockedColor;
            }
        }
    }

    private MeshRenderer meshRenderer;

    private void Awake() {
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        UpdateVisuals();


        //levelNumberText.text = "00";
    }


    private void OnMouseDown() {
        if (level == null) Debug.LogError("level button missing level SO", gameObject);


        // TODO: Spawn particles
        // if (LevelSelector.Instance.clickParticlePrefab != null) {
        //     Instantiate(LevelSelector.Instance.clickParticlePrefab, transform.position, Quaternion.identity, transform.parent);
        // }

        var animationDuration = 0.3f;
        transform.DOPunchScale(Vector3.one * 0.2f, animationDuration, 6, 0.8f)
            .OnComplete(() => {
                if (!isUnlocked) {
                    Log(gridPosition + " is locked.");
                    return;
                }

                // Log(gridPosition + " is unlocked.");
                LevelAreaController.Instance.HideAllButtons();

                LevelAreaController.Instance.ChosenLevel = level;
                SceneLoader.Instance.LoadSceneGroup("Empty Level Base", 1f);
            });

        // TODO: Play SFX
        // LevelSelector.Instance.LevelSelectedSFX.Play();
    }
}