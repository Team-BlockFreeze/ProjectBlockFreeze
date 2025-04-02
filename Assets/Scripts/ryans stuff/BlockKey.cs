using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

public class BlockKey : MonoBehaviour
{
    [SerializeField]
    private BlockBehaviour block;

    private void Start()
    {
        if (block == null)
            block = GetComponent<BlockBehaviour>();

    }

    private void OnEnable()
    {
        if (block == null)
            block = GetComponent<BlockBehaviour>();

        block.Event_NextMoveBegan.AddListener(HasKeyReachedGoal);
    }

    private void OnDisable()
    {
        block.Event_NextMoveBegan.RemoveListener(HasKeyReachedGoal);
    }

    private void HasKeyReachedGoal()
    {
        if (block.coord != block.GridRef.GoalCoord) return;

        BlockCoordinator.Coordinator.CancelInvoke();
        Invoke(nameof(LevelCompleteAnimation), 1.5f);
        Debug.Log("Recognised Level Complete");
    }

    public static UnityEvent Event_LevelComplete = new UnityEvent();

    private void LevelCompleteAnimation()
    {
        Event_LevelComplete?.Invoke();
        transform.DOMove(Vector3.forward*.9f, 1.5f).SetRelative();
        Debug.Log("Level Complete Animation Begin");
    }

}
