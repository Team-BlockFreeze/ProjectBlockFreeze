using Systems.SceneManagement;
using UnityEngine;
using System.Text.RegularExpressions;
using DG.Tweening;
using Ami.BroAudio;
using System;

public class LevelSelectButton : MonoBehaviour {
    [SerializeField]
    private LevelDataSO level;
    public LevelDataSO Level {
        get { return level; }
        set {
            // TODO: fix string part later

            //Match match = Regex.Match(value.name, @"\d+");
            //int levelNum = int.Parse(match.Value);
            //levelNumberText.text = levelNum.ToString("D2");
            // string levelNum = value.name.Substring(5);
            // if (levelNum.Length == 2) levelNum = levelNum.Insert(1, "0");
            // levelNum = levelNum.Insert(1, "-");
            // levelNumberText.text = levelNum;
            // level = value;
        }
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

        if (LevelSelector.Instance.clickParticlePrefab != null) {
            Instantiate(LevelSelector.Instance.clickParticlePrefab, transform.position, Quaternion.identity, transform.parent);
        }

        float animationDuration = 0.3f;
        transform.DOPunchScale(Vector3.one * 0.2f, animationDuration, vibrato: 6, elasticity: 0.8f)
            .OnComplete(() => {
                LevelSelector.Instance.ChosenLevel = level;
                SceneLoader.Instance.LoadSceneGroup(index: 2, delayInSeconds: 0f);
            });

        LevelSelector.Instance.LevelSelectedSFX.Play();
    }

    internal void SetLocked(object value) {
        throw new NotImplementedException();
    }
}