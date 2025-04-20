using Systems.SceneManagement;
using UnityEngine;
using System.Text.RegularExpressions;
using DG.Tweening;
using Ami.BroAudio;
using System;
using System.ComponentModel;
using Ami.BroAudio.Runtime;

[SelectionBase] //! When selecting things in the scene view, it will select the parent object and not the stuff inside
public class LevelSelectButton : LoggerMonoBehaviour {
    [SerializeField]
    private LevelDataSO level;
    public LevelDataSO Level {
        get { return level; }
        set {
            // TODO: fix string part later

            //Match match = Regex.Match(value.name, @"\d+");
            //int levelNum = int.Parse(match.Value);
            //levelNumberText.text = levelNum.ToString("D2");
            string levelNum = value.name.Substring(0);
            if (levelNum.Length == 2) levelNum = levelNum.Insert(1, "0");
            levelNum = levelNum.Insert(1, "-");
            levelNumberText.text = levelNum;
            level = value;
        }
    }

    [Sirenix.OdinInspector.ReadOnly, SerializeField]
    private Vector2Int gridPosition;
    public Vector2Int GridPosition {
        get { return gridPosition; }
        set { gridPosition = value; }
    }

    [SerializeField] private bool isUnlocked;
    public bool IsUnlocked {
        get { return isUnlocked; }
        set { isUnlocked = value; }
    }

    [SerializeField]
    private TMPro.TMP_Text levelNumberText;

    private void Awake() {
        //levelNumberText.text = "00";
    }



    private void OnMouseDown() {
        if (level == null) {
            Debug.LogError("level button missing level SO", gameObject);
        }


        // TODO: Spawn particles
        // if (LevelSelector.Instance.clickParticlePrefab != null) {
        //     Instantiate(LevelSelector.Instance.clickParticlePrefab, transform.position, Quaternion.identity, transform.parent);
        // }

        float animationDuration = 0.3f;
        transform.DOPunchScale(Vector3.one * 0.2f, animationDuration, vibrato: 6, elasticity: 0.8f)
            .OnComplete(() => {

                if (!isUnlocked) {
                    Log(gridPosition + " is locked.");
                    return;
                }
                else {
                    // Log(gridPosition + " is unlocked.");
                }

                LevelSelectorBranches.Instance.HideAllButtons();

                LevelSelectorBranches.Instance.ChosenLevel = level;
                SceneLoader.Instance.LoadSceneGroup(groupName: "Empty Level Base", delayInSeconds: 1f);
            });

        // TODO: Play SFX
        // LevelSelector.Instance.LevelSelectedSFX.Play();
    }

}