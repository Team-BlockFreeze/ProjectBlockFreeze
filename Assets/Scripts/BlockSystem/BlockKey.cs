using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

public class BlockKey : LoggerMonoBehaviour {
    [SerializeField]
    private BlockBehaviour block;

    private void Start() {
        if (block == null)
            block = GetComponent<BlockBehaviour>();

    }

    private void OnEnable() {
        if (block == null)
            block = GetComponent<BlockBehaviour>();

        block.Event_NextMoveBegan.AddListener(HasKeyReachedGoal);
    }

    private void OnDisable() {
        block.Event_NextMoveBegan.RemoveListener(HasKeyReachedGoal);
    }

    public void HasKeyReachedGoal() {
        if (block.coord != block.GridRef.GoalCoord) return;

        BlockCoordinator.Coordinator.StopAllCoroutines();
        Invoke(nameof(LevelCompleteAnimation), .25f);
        // Log("Recognised Level Complete");
    }

    public static UnityEvent<LevelDataSO> Event_LevelComplete = new UnityEvent<LevelDataSO>();


    private void LevelCompleteAnimation() {
        var levelData = BlockGrid.Instance.LevelData;
        Event_LevelComplete?.Invoke(levelData);
        transform.DOMove(Vector3.forward * .9f, 1.5f).SetRelative();
        // Log("Level Complete Animation Begin");
    }

}
