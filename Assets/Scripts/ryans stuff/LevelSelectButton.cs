using Systems.SceneManagement;
using UnityEngine;
using System.Text.RegularExpressions;

public class LevelSelectButton : MonoBehaviour
{
    [SerializeField]
    private LevelDataSO level;
    public LevelDataSO Level { 
        get { return level; } 
        set {
            //Match match = Regex.Match(value.name, @"\d+");
            //int levelNum = int.Parse(match.Value);
            //levelNumberText.text = levelNum.ToString("D2");
            string levelNum = value.name.Substring(5);
            if (levelNum.Length == 2) levelNum = levelNum.Insert(1, "0");
            levelNum = levelNum.Insert(1, "-");
            levelNumberText.text = levelNum;
            level = value;
        } 
    }

    [SerializeField]
    private TMPro.TMP_Text levelNumberText;

    private void Awake() {
        //levelNumberText.text = "00";
    }

    private void OnMouseDown() {
        if(level==null) {
            Debug.LogError("level button missing level SO", gameObject);
        }

        LevelSelector.Instance.ChosenLevel = level;
        SceneLoader.Instance.LoadSceneGroup(index: 2, delayInSeconds: 0f);
    }
}
