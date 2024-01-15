using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System;
using UnityEngine.Events;

public class PinchablePageScroller : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    #region [CLASS MEMBERS]
    // Serialized Field --------------------------------------------------//
    //
    [SerializeField] Canvas Canvas;
    [SerializeField] Camera UICamera;
    [SerializeField] RectTransform ContentTransform;
    [SerializeField] List<RectTransform> TargetViews = new List<RectTransform>();
    [SerializeField] float TweenTime = 0.12f;
    [SerializeField] float QuickDragDuration = 0.3f;
    [SerializeField] GameObject ButtonScaleUp, ButtonScaleDown;
    [SerializeField] float DesignedCanvasWidth = 800.0f;
    [SerializeField] public int PageIndex = 0;
    [SerializeField] bool FitToScreen = false;


    public UnityEvent<int> OnPageChangeEnded;

    // inner class define --------------------------------------------------//
    //
    class PointInfo
    {
        public int ID;
        public Vector2 vPos;
    }


    // member values --------------------------------------------------//
    //
    float mContentWidth = 800.0f;
    float mContentHeight = 1055.0f;
    List<PointInfo> mMSPointInfos = new List<PointInfo>();
    float mZoomStartPointsDist;
    Vector3 mZoomStartScale;
    Vector2 mOriginalScale;
    float mPageMargin;
    float mMSDownTime;
    Vector2 mMSDownPos;
    bool mIsZoomMode = false;
    Vector2 mVOrigin = Vector2.zero;
    #endregion



    #region [MONO_EVENTS]

    // Start is called before the first frame update
    void Start()
    {
        UnityEngine.Assertions.Assert.IsTrue(Canvas != null, "Canvas Can't be null!");
        UnityEngine.Assertions.Assert.IsTrue(ContentTransform != null, "ContentTransform Can't be null!");


        Camera uiCamera = Canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : UICamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(Canvas.GetComponent<RectTransform>(), Vector2.zero, uiCamera, out mVOrigin);

#if !UNITY_EDITOR
        if (ButtonScaleUp != null) ButtonScaleUp.SetActive(false);
        if (ButtonScaleDown != null) ButtonScaleDown.SetActive(false);
#endif
    }
    #endregion


    #region [PUBLIC FUNCTIONS]
    public void AddTargetView(RectTransform target)
    {
        TargetViews.Add(target);
        target.SetParent(ContentTransform, false);
    }
    public void Trigger(List<RectTransform> targetViews, int idxStart = 0)
    {
        for (int k = 0; k < targetViews.Count; ++k)
            AddTargetView(targetViews[k]);

        Trigger(idxStart);
    }
    public void Trigger(int idxStart = 0)
    {
        PageIndex = Math.Clamp(idxStart, 0, TargetViews.Count - 1);
        mContentWidth = ContentTransform.rect.width;
        mContentHeight = ContentTransform.rect.height;
        Vector2 vPos = Vector2.zero;

        ContentTransform.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
        ContentTransform.localPosition = Vector2.zero;

        mPageMargin = DesignedCanvasWidth - mContentWidth;
        for (int k = 0; k < TargetViews.Count; ++k)
        {
            TargetViews[k].localPosition = vPos;
            vPos = new Vector2(vPos.x + mContentWidth + mPageMargin, vPos.y);
        }
        mOriginalScale = ContentTransform.localScale;
        mIsZoomMode = false;



        JumpToPage(PageIndex, true);

        if (FitToScreen)
        {
            for (int k = 0; k < TargetViews.Count; ++k)
            {
                float fRate = ((float)mContentWidth) / ((float)TargetViews[k].rect.width);
                TargetViews[k].localScale *= fRate;
            }
        }
    }
    public void JumpToPage(int idxLandPage, bool instantJump = false)
    {
        idxLandPage = Math.Clamp(idxLandPage, 0, TargetViews.Count - 1);
        int diff = PageIndex - idxLandPage;
        float fLandPos = diff * mContentWidth;

        void OnFinishedJumpToPage()
        {
            PageIndex = idxLandPage;
            // Make sure scaling need to be pivoted onto the object at the page.
            float wDiff = mPageMargin / mContentWidth;
            ContentTransform.GetComponent<RectTransform>().pivot = new Vector2(idxLandPage + 0.5f + PageIndex * wDiff, 0.5f);
            ContentTransform.localPosition = Vector2.zero;

            OnPageChangeEnded?.Invoke(PageIndex);
        }

        if (instantJump)
            OnFinishedJumpToPage();
        else
            StartCoroutine(coMoveTo(ContentTransform, new Vector2(fLandPos, .0f), TweenTime, OnFinishedJumpToPage));
    }
    public void Clear()
    {
        TargetViews.Clear();
    }
    #endregion


    #region [MOUSE_IXXXHandler_EVENTS]

    // Pointer Event Handlers --------------------------------------------------//
    //
    public void OnDrag(PointerEventData eventData)
    {
        // Drag Mode.
        if (mMSPointInfos.Count == 1)
            UpdateDrag(eventData);

        // Pinch Mode.
        else if (mMSPointInfos.Count == 2)
            UpdateScale(eventData);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        UpdatePointInfo(eventData.pointerId, eventData.position);

        mMSDownTime = Time.time;
        mMSDownPos = eventData.position;

        if (mMSPointInfos.Count == 2)
            EnterZoomMode();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        RemovePointInfo(eventData.pointerId);

        if (mMSPointInfos.Count == 0)
        {
            Vector2 vCurScale = ContentTransform.localScale;
            float fDiff = vCurScale.magnitude - mOriginalScale.magnitude;
            if (Mathf.Abs(fDiff) < 0.01f)
                LandToNearestPage(eventData.position.x);

            else if (fDiff < .0f)
                ExitZoomMode();
        }
    }
    #endregion


    #region [HELPERS]

    // search land pos.
    int FindLandPage(float curPosX)
    {
        if (curPosX < -mContentWidth * 0.5f)
            return PageIndex + 1;
        else if (curPosX >= -mContentWidth * 0.5f && curPosX < mContentWidth * 0.5f)
            return PageIndex;
        else
            return PageIndex - 1;
    }
    void UpdateDrag(PointerEventData eventData)
    {
        Vector2 vRet;
        Camera uiCamera = Canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : UICamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(Canvas.GetComponent<RectTransform>(), eventData.delta, uiCamera, out vRet);

        if (mIsZoomMode)
        {
            float xOffset = vRet.x - mVOrigin.x;
            float yOffset = vRet.y - mVOrigin.y;
            float xLimit = mContentWidth * mOriginalScale.x - TargetViews[PageIndex].rect.width * TargetViews[PageIndex].localScale.x * ContentTransform.localScale.x;
            float yLimit = mContentHeight * mOriginalScale.y - TargetViews[PageIndex].rect.height * TargetViews[PageIndex].localScale.y * ContentTransform.localScale.y;

            float xPos = Mathf.Clamp(ContentTransform.localPosition.x + xOffset, -Math.Abs(xLimit) * 0.5f, Math.Abs(xLimit) * 0.5f);
            float yPos = Mathf.Clamp(ContentTransform.localPosition.y + yOffset, -Math.Abs(yLimit) * 0.5f, Math.Abs(yLimit) * 0.5f);

            ContentTransform.localPosition = new Vector3(xPos, yPos, ContentTransform.localPosition.z);
        }
        else
        {
            ContentTransform.localPosition += new Vector3(vRet.x - mVOrigin.x, .0f, .0f);
        }
        mMSPointInfos[0].vPos = eventData.position;
    }
    void UpdateScale(PointerEventData eventData)
    {
        if (eventData.pointerId == mMSPointInfos[0].ID)
        {
            mMSPointInfos[0].vPos = eventData.position;
        }
        else
        {
            mMSPointInfos[1].vPos = eventData.position;

            float curLength = (mMSPointInfos[0].vPos - mMSPointInfos[1].vPos).magnitude;
            float fRate = curLength / mZoomStartPointsDist;
            ContentTransform.localScale = mZoomStartScale * fRate;
        }
    }
    void LandToNearestPage(float MSXPos)
    {
        float fDir = mMSDownPos.x - MSXPos < .0f ? 1.0f : -1.0f;
        float fQuickDragPoint = .0f;
        if (Time.time - mMSDownTime < QuickDragDuration)
            fQuickDragPoint = fDir * mContentWidth * 0.5f;

        int idxLandPage = FindLandPage(ContentTransform.localPosition.x + fQuickDragPoint);
        JumpToPage(idxLandPage);
    }
    void EnterZoomMode(bool isEditorMode = false)
    {
        mIsZoomMode = true;

        if (!isEditorMode)
        {
            mZoomStartPointsDist = (mMSPointInfos[0].vPos - mMSPointInfos[1].vPos).magnitude;
            mZoomStartScale = ContentTransform.localScale;
        }
        for (int k = 0; k < TargetViews.Count; ++k)
            TargetViews[k].gameObject.SetActive(k == PageIndex);
    }
    void ExitZoomMode()
    {
        mIsZoomMode = false;

        StartCoroutine(coScaleTo(ContentTransform, mOriginalScale, TweenTime));
        StartCoroutine(coMoveTo(ContentTransform, Vector2.zero, TweenTime, () =>
        {
            for (int k = 0; k < TargetViews.Count; ++k)
                TargetViews[k].gameObject.SetActive(true);
        }));
    }
    #endregion


    #region [TWEENERS]

    IEnumerator coMoveTo(Transform trMoveTarget, Vector2 vTo, float duration, Action callbackFinish)
    {
        float fStartTime = Time.time;
        Vector2 vStart = trMoveTarget.localPosition;
        while (Time.time - fStartTime < duration)
        {
            float fRate = (Time.time - fStartTime) / duration;
            trMoveTarget.localPosition = Vector2.Lerp(vStart, vTo, fRate);
            yield return null;
        }
        trMoveTarget.localPosition = vTo;

        if (callbackFinish != null)
            callbackFinish.Invoke();
    }
    IEnumerator coScaleTo(Transform trScaleTarget, Vector2 vTo, float duration)
    {
        float fStartTime = Time.time;
        Vector2 vStart = trScaleTarget.localScale;
        while (Time.time - fStartTime < duration)
        {
            float fRate = (Time.time - fStartTime) / duration;
            trScaleTarget.localScale = Vector2.Lerp(vStart, vTo, fRate);
            yield return null;
        }
        trScaleTarget.localScale = vTo;
    }

    #endregion


    #region [EDITOR ONLY]

    public void OnHomeClicked()
    {
        ContentTransform.localScale = mOriginalScale;
        ExitZoomMode();

        JumpToPage(0);
    }
    public void OnScaleUpClicked()
    {
        float fRate = 1.1f;
        ContentTransform.localScale *= fRate;
        EnterZoomMode(isEditorMode: true);
    }
    public void OnScaleDownClicked()
    {
        float fRate = 0.95f;
        ContentTransform.localScale *= fRate;

        Vector2 vCurScale = ContentTransform.localScale;
        float fDiff = vCurScale.magnitude - mOriginalScale.magnitude;

        if (fDiff < .0f)
            ExitZoomMode();
    }
    #endregion


    #region [Buffer Control]

    // Point Buffer control ---------------------------------------------------//
    //
    int FindPointInfoIndex(int id)
    {
        for (int k = 0; k < mMSPointInfos.Count; ++k)
        {
            if (mMSPointInfos[k].ID == id)
                return k;
        }
        return -1;
    }

    void RemovePointInfo(int id)
    {
        int index = FindPointInfoIndex(id);
        if (index >= 0)
            mMSPointInfos.RemoveAt(index);
    }

    void UpdatePointInfo(int id, Vector2 vPos)
    {
        int index = FindPointInfoIndex(id);
        if (index >= 0)
            mMSPointInfos.RemoveAt(index);

        PointInfo info = new PointInfo();
        info.ID = id;
        info.vPos = vPos;

        mMSPointInfos.Add(info);
    }

    #endregion
}
