using UnityEngine;

public class SetTimeScale : MonoBehaviour
{

    [SerializeField]
    private float timeScaleToSet = 1f;
    public void DoSetTimeScale()
    {
        Time.timeScale = timeScaleToSet;
    }
}
