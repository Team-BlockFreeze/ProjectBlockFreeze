using UnityEngine;
using UnityEngine.EventSystems;

public class SetTimeScale : MonoBehaviour
{
    [SerializeField]
    private bool startSelected = false;

    private void Start()
    {
        if(startSelected)
            EventSystem.current.SetSelectedGameObject(gameObject);
    }

    [SerializeField]
    private float timeScaleToSet = 1f;
    public void DoSetTimeScale()
    {
        Time.timeScale = timeScaleToSet;
        EventSystem.current.SetSelectedGameObject(gameObject);
    }
}
