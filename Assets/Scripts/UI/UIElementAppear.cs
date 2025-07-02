using System.Collections.Generic;
using UnityEngine;

public class UIElementAppear : MonoBehaviour {
    [SerializeField]
    private List<GameObject> targets;

    private void OnEnable() {
        BlockKey.Event_LevelComplete.AddListener(EnableTarget);
    }

    private void OnDisable() {
        BlockKey.Event_LevelComplete.RemoveListener(EnableTarget);
    }

    public void EnableTarget(LevelDataSO levelData) {
        foreach (var target in targets) {
            target.SetActive(true);
        }
    }
}
