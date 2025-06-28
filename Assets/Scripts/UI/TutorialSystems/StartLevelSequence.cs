using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// The script to control the intro sequence when loading a new level the first time - not reloading a level. Also triggers tutorial prompts.
/// </summary>
public class StartLevelSequence : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private TMP_Text titleTMP;
    [SerializeField]
    private Image 
        rayBlockPanel,
        titleBannerPanel;
    [SerializeField]
    private IngameCanvasButtons canvasTransitionScript;
    private IngameCanvasButtons cVT => canvasTransitionScript;

    [Header("TitleAnimValues")]
    [SerializeField]
    private float titleAnimLength = 2f;
    [SerializeField]
    private float stallPercent = .2f;

    #region subbing

    private void OnEnable() {
        Sub();
    }

    private void OnDisable() {
        Unsub();
    }

    private void Sub() {
        BlockGrid.Event_LevelFirstLoad.AddListener(StartSequence);
    }

    private void Unsub() {
        BlockGrid.Event_LevelFirstLoad.RemoveListener(StartSequence);
    }

    #endregion

    private void Start() {

    }

    private void SequencePrepare() {
        //setting title offscreen
        var tRT = titleTMP.rectTransform;
        tRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Screen.width*2f);
        Vector2 pos = tRT.anchoredPosition;
        pos.x = Screen.width*3f;
        tRT.anchoredPosition = pos;

        rayBlockPanel.gameObject.SetActive(true);
        //cVT.ShowOnlyCanvasOfGO()
    }

    private void SequenceComplete() {
        titleTweenSeq?.Kill();
        rayBlockPanel.gameObject.SetActive(false);
    }

    private Sequence titleTweenSeq;

    const bool DEBUG = true;
    const string DEFAULT_TITLE = "This Is The Level Title";

    [Button]
    private void StartSequence(LevelDataSO lvlDataSO = null) {
        SequencePrepare();

        //getting title text
        if (!lvlDataSO.LevelTitle.Equals(""))
            titleTMP.text = lvlDataSO.LevelTitle;
        else if(DEBUG)
            titleTMP.text = DEFAULT_TITLE;
        else 
            titleTMP.text = "";

        float inOutTime = (titleAnimLength * (1-stallPercent)) * .5f;

        titleTweenSeq?.Kill();
        titleTweenSeq = DOTween.Sequence();

        var tRT = titleTMP.rectTransform;

        titleTweenSeq.Append(tRT.DOAnchorPosX(0f, inOutTime).SetEase(Ease.OutSine));
        titleTweenSeq.AppendInterval(titleAnimLength * stallPercent);
        titleTweenSeq.Append(tRT.DOAnchorPosX(-Screen.width*3f, inOutTime).SetEase(Ease.InSine));
        titleTweenSeq.AppendCallback(() => SequenceComplete());

        titleTweenSeq.Play();

    }
}
