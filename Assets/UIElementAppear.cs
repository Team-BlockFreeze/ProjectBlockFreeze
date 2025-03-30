using UnityEngine;

public class UIElementAppear : MonoBehaviour
{
    [SerializeField]
    private GameObject target;

    private void OnEnable()
    {
        BlockKey.Event_LevelComplete.AddListener(EnableTarget);
    }

    private void OnDisable()
    {
        BlockKey.Event_LevelComplete.RemoveListener(EnableTarget);
    }

    public void EnableTarget()
    {
        target.SetActive(true);
    }
}
