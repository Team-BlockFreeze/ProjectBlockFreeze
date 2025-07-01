using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using System.Collections;

using TutMessage = LevelDataSO.TutorialMessage;
using UnityEditor;

/// <summary>
/// The script to control the intro sequence when loading a new level the first time - not reloading a level. Also triggers tutorial prompts.
/// </summary>
public class StartLevelSequence : MonoBehaviour {
    [Header("References")]
    [SerializeField]
    private TMP_Text titleTMP;
    [SerializeField]
    private Image
        rayBlockPanel,
        titleBannerPanel;
    [SerializeField]
    private IngameCanvasButtons canvasTransitionScript;
    [SerializeField]
    private IngameCanvasButtons cVT => canvasTransitionScript;
    [SerializeField]
    private CanvasGroup tutCanvasGroup;
    [SerializeField]
    private TMP_Text tutMessageTMP;

    [Header("TitleAnimValues")]
    [SerializeField]
    private float titleAnimLength = 2f;
    [SerializeField]
    private float stallPercent = .2f;

    private enum SequenceState { ActiveTitle, Inactive, TitlePaused, ActiveTutorial };

    [ReadOnly]
    [SerializeField]
    private SequenceState state = SequenceState.Inactive;

    [Header("settings")]
    [SerializeField]
    private bool manualTitleClear = false;

    #region subbing

    private void OnEnable() {
        Sub();
    }

    private void OnDisable() {
        Unsub();
    }

    private void Sub() {
        BlockGrid.Event_LevelFirstLoad.AddListener(StartTitleSequence);
        BlockGrid.Event_LevelSubsequentLoad.AddListener(TryRetriggerTitleSequence);
    }

    private void Unsub() {
        BlockGrid.Event_LevelFirstLoad.RemoveListener(StartTitleSequence);
        BlockGrid.Event_LevelSubsequentLoad.RemoveListener(TryRetriggerTitleSequence);
    }

    #endregion

    private void Start() {

    }

    private void TryRetriggerTitleSequence(LevelDataSO lvlDataSO) {
        if (!lvlDataSO.RetriggerSequenceOnReload)
            return;

        StartTitleSequence(lvlDataSO);
    }

    /// <summary>
    /// set initial state before title animation
    /// </summary>
    private void TitleSequencePrepare() {
        //rayBlockPanel.transform.parent.gameObject.SetActive(true);

        //cVT.ShowInterrupter();
        state = SequenceState.ActiveTitle;

        //setting title offscreen
        var tRT = titleTMP.rectTransform;
        tRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Screen.width * 2f);
        //Vector2 pos = tRT.anchoredPosition;
        //pos.x = Screen.width*3f;
        //tRT.anchoredPosition = pos;

        titleBannerPanel.gameObject.SetActive(true);
    }

    /// <summary>
    /// finalise state after title animation
    /// </summary>
    private void TitleSequenceComplete() {
        //rayBlockPanel.gameObject.SetActive(false);
        cVT.ShowButtons();
        state = SequenceState.Inactive;
    }

    private Sequence titleTweenSeq;

    const bool DEBUG = false;
    const string DEFAULT_TITLE = "This Is The Level Title";

    LevelDataSO savedLevelSO;

    [Button]
    private void StartTitleSequence(LevelDataSO lvlDataSO = null) {
        cVT.ShowInterrupter();
        savedLevelSO = lvlDataSO;

        if (lvlDataSO.LevelTitle == null || lvlDataSO.LevelTitle.Equals("")) {
            Debug.Log("No level title to load, skipping level title sequence");
            //cVT.ShowButtons();
            cVT.SetBlur(false);
            TryStartTutorialSequence(lvlDataSO);
            return;
        }
        //getting title text
        titleTMP.text = lvlDataSO.LevelTitle;

        TitleSequencePrepare();

        float inOutTime = (titleAnimLength * (1 - stallPercent)) * .5f;

        titleTweenSeq?.Kill();
        titleTweenSeq = DOTween.Sequence();

        var tRT = titleTMP.rectTransform;

        titleTweenSeq.AppendCallback(() => Debug.Log("title tween started"));
        //titleTweenSeq.AppendInterval(.5f);
        //titleTweenSeq.Append(tRT.DOAnchorPosX(0f, inOutTime).SetEase(Ease.OutSine).SetTarget(tRT));
        titleTweenSeq.AppendInterval(titleAnimLength * stallPercent);
        if (manualTitleClear) {
            titleTweenSeq.AppendCallback(() => {
                titleTweenSeq.Pause();
                state = SequenceState.TitlePaused;
            });
            titleTweenSeq.AppendInterval(.1f);
        }
        //titleTweenSeq.Append(tRT.DOAnchorPosX(-Screen.width*3f, inOutTime).SetEase(Ease.InSine).SetTarget(tRT));
        //titleTweenSeq.AppendCallback(() => TitleSequenceComplete());
        titleTweenSeq.AppendCallback(() => TryStartTutorialSequence(lvlDataSO));
        titleTweenSeq.AppendCallback(() => Debug.Log("title tween completed"));

        titleTweenSeq.OnKill(() => Debug.Log("Sequence was killed"));
        titleTweenSeq.OnComplete(() => Debug.Log("Sequence ended cleanly"));

        titleTweenSeq.Play();

    }

    //udpate loop to catch input while title is tweening, will skip ahead
    private void Update() {
        if (state != SequenceState.ActiveTitle && state != SequenceState.TitlePaused) return;

        if (Input.anyKeyDown) {
            Debug.Log("title tween interrupted");
            //TitleSequenceComplete();
            TryStartTutorialSequence(savedLevelSO);
        }
    }


    private void TryStartTutorialSequence(LevelDataSO lvlDataSO) {
        if (lvlDataSO == null) {
            Debug.Log("No level data found, skipping tutorial sequence");
            TitleSequenceComplete();
            return;
        }

        TutMessage[] messages = lvlDataSO.TutorialMessages;
        if (messages == null || messages.Length == 0) {
            Debug.Log("No tutorial messages found, skipping tutorial sequence");
            TitleSequenceComplete();
            return;
        }

        //fade out title canvas group
        var titleGroup = titleBannerPanel.GetComponent<CanvasGroup>();
        DOTween.Kill(titleGroup);
        titleGroup.DOFade(0f, 1.2f).OnComplete(() => {
            titleGroup.gameObject.SetActive(false);
            titleGroup.alpha = 1f;
        });

        //show canvas if not yet triggered by title sequence
        //if (state == SequenceState.Inactive)
        //    cVT.ShowInterrupter();


        Debug.Log("Tutorial present, starting tutorial coroutine sequence");
        StartCoroutine(TutorialCoreography(lvlDataSO));
    }

    const float MinInterruptDelay = .2f;

    /// <summary>
    /// coroutine thats active while tutorial messages are being shown, waits for input to go to next message
    /// </summary>
    /// <returns></returns>
    private IEnumerator TutorialCoreography(LevelDataSO lvlDataSO) {
        state = SequenceState.ActiveTutorial;

        TutMessage[] messages = lvlDataSO.TutorialMessages;

        //first message setup
        tutMessageTMP.text = messages[0].message;
        var msgRT = tutCanvasGroup.GetComponent<RectTransform>();
        SetAnchorPivot(msgRT, messages[0].anchorPivot);

        //fade in first message box
        tutCanvasGroup.alpha = 0f;
        tutCanvasGroup.gameObject.SetActive(true);
        tutCanvasGroup.DOKill();
        tutCanvasGroup.DOFade(1f, .5f);

        yield return new WaitForSeconds(.5f);

        //unblur
        cVT.SetBlur(false);

        float timeSinceLastInterrupt = Time.time;

        //wait for input before instant switching to next tutorial message
        for (int i = 0; i < messages.Length; i++) {
            if (i != 0) {
                var message = messages[i];

                //hide message
                tutCanvasGroup.gameObject.SetActive(false);

                //update to next message
                tutMessageTMP.text = message.message;
                msgRT = tutCanvasGroup.GetComponent<RectTransform>();
                SetAnchorPivot(msgRT, message.anchorPivot);

                //display message
                tutCanvasGroup.gameObject.SetActive(true);
            }

            //wait for input
            while (true) {
                if (Input.anyKeyDown && Time.time > timeSinceLastInterrupt + MinInterruptDelay)
                    break;
                yield return null;
            }

            yield return null;
        }

        //end tut - dissapear message
        tutCanvasGroup.alpha = 0f;
        tutCanvasGroup.gameObject.SetActive(false);

        //fast dissapear interrupter canvas
        var overallCanvasGroup = rayBlockPanel.gameObject.GetComponentInParent<CanvasGroup>();
        overallCanvasGroup.alpha = 0;
        overallCanvasGroup.gameObject.SetActive(false);

        state = SequenceState.Inactive;

        //fade in regular UI
        cVT.ShowButtons();
        GetComponentInChildren<GridMaskController>().UpdateMaskBounds();
    }

    const float MARGIN = 120f;

    private void SetAnchorPivot(RectTransform rt, TutMessage.Anchor anchorPivot) {
        Vector2 vec = anchorPivot == TutMessage.Anchor.Bottom ? new Vector2(.5f, 0f) : new Vector2(.5f, 1f);

        rt.anchorMin = vec;
        rt.anchorMax = vec;
        rt.pivot = vec;

        switch (anchorPivot) {
            case TutMessage.Anchor.Top:
                rt.anchoredPosition = new Vector2(0f, -MARGIN);
                break;
            case TutMessage.Anchor.Bottom:
                rt.anchoredPosition = new Vector2(0f, MARGIN);
                break;
        }
    }
}
