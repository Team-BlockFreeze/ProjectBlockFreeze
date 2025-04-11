using UnityEngine;
using Sirenix.OdinInspector;
using UnityEditor;
using System.Collections.Generic;
using Systems.SceneManagement;
using Ami.BroAudio;

public class LevelSelector : PersistentSingleton<LevelSelector> {
    [SerializeField]
    [Sirenix.OdinInspector.FolderPath]
    private string levelsPath;

    [SerializeField]
    private Vector2Int layoutXY = new Vector2Int(4, 5);

    [SerializeField]
    private LevelSelectButton buttonFab;

    [SerializeField]
    private List<LevelDataSO> levels = new List<LevelDataSO>();

    public LevelDataSO ChosenLevel { get; set; }

    public ParticleSystem clickParticlePrefab;
    public SoundID LevelSelectedSFX;

    [ReadOnly]
    [SerializeField]
    private List<GameObject> LevelButtons = new List<GameObject>();

    protected override void Awake() {
        base.Awake();
    }

    [BoxGroup("Buttons")]
    [Button]
    private void LoadLevelsFromPath() {
        if (levelsPath == null || levelsPath.Length == 0) {
            Debug.LogWarning("No levels path");
            return;
        }

        levels.Clear();

        string[] guids = AssetDatabase.FindAssets("t:LevelDataSO", new[] { levelsPath });

        foreach (string guid in guids) {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            LevelDataSO level = AssetDatabase.LoadAssetAtPath<LevelDataSO>(path);
            if (level != null) {
                levels.Add(level);
            }
        }
    }

    [BoxGroup("Buttons")]
    [Button]
    private void PopulateSceneLevelButtons() {
        if (levels == null || levels.Count == 0) {
            Debug.LogWarning("No levels");
            return;
        }

        foreach (var b in LevelButtons)
            GameObject.DestroyImmediate(b);
        LevelButtons.Clear();

        Vector2 topLeft = new Vector2(-layoutXY.x + 1, layoutXY.y - 1);

        int y = 0;
        int x = 0;
        foreach (var l in levels) {
            var newButton = GameObject.Instantiate(buttonFab);
            //newButton.transform.parent = transform;
            Vector2 pos = topLeft + (Vector2.right * x + Vector2.down * y) * 2f;
            newButton.transform.position = pos;

            newButton.Level = l;

            LevelButtons.Add(newButton.gameObject);

            x++;
            if (x >= layoutXY.x)
                y++;
            x = x % layoutXY.x;
        }

        EditorUtility.SetDirty(this);
    }

    const int baseLevelIdx = 2;
    const int levelSelectIdx = 1;


    public void LoadNextLevel() {
        int curIdx = levels.IndexOf(ChosenLevel);
        if (curIdx >= 0 && curIdx < levels.Count - 1) {
            ChosenLevel = levels[curIdx + 1];
            SceneLoader.instance.LoadSceneGroup(baseLevelIdx, 0);
            return;
        }
        SceneLoader.instance.LoadSceneGroup(levelSelectIdx, 0);
    }
}
