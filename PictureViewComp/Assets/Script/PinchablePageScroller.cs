using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class PinchablePageScroller : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    #region [CLASS MEMBERS]
    // Serialized Field --------------------------------------------------//
    //
    [SerializeField] Transform ContentTransform;
    [SerializeField] List<RectTransform> TargetViews = new List<RectTransform>();
    [SerializeField] float TweenTime = 0.12f;
    [SerializeField] float QuickDragDuration = 0.3f;
    [SerializeField] GameObject ButtonScaleUp, ButtonScaleDown;
    [SerializeField] public float PageWidth = 800.0f;
    [SerializeField] public int PageIndex = 0;


    // inner class define --------------------------------------------------//
    //
    class PointInfo
    {
        public int ID;
        public Vector2 vPos;
    }


    // member values --------------------------------------------------//
    //
    List<PointInfo> mMSPointInfos = new List<PointInfo>();
    float mZoomStartPointsDist;
    Vector3 mZoomStartScale;
    Vector2 mOriginalScale;

    float mMSDownTime;
    Vector2 mMSDownPos;
    bool mIsZoomMode = false;

    #endregion



    #region [MONO_EVENTS]

    // Start is called before the first frame update
    void Start()
    {
        // Application.targetFrameRate = 60;
#if !UNITY_EDITOR
        if (ButtonScaleUp != null) ButtonScaleUp.SetActive(false);
        if (ButtonScaleDown != null) ButtonScaleDown.SetActive(false);
#endif

    }

    void OnEnable()
    {
        Vector2 vPos = Vector2.zero;
        for (int k = 0; k < TargetViews.Count; ++k)
        {
            TargetViews[k].localPosition = vPos;
            vPos = new Vector2(vPos.x + PageWidth, vPos.y);
        }
        mOriginalScale = ContentTransform.localScale;
        mIsZoomMode = false;
        ContentTransform.GetComponent<RectTransform>().pivot = new Vector2(PageIndex + 0.5f, 0.5f);
    }
    #endregion


    #region [PUBLIC FUNCTIONS]
    public void AddTargetView(RectTransform target)
    {
        TargetViews.Add(target);
    }
    public void JumpToPage(int idxLandPage)
    {
        idxLandPage = Math.Clamp(idxLandPage, 0, TargetViews.Count - 1);
        int diff = PageIndex - idxLandPage;
        float fLandPos = diff * PageWidth;

        StartCoroutine(coMoveTo(ContentTransform, new Vector2(fLandPos, .0f), TweenTime, () =>
        {
            PageIndex = idxLandPage;
            // Make sure scaling need to be pivoted onto the object at the page.
            ContentTransform.GetComponent<RectTransform>().pivot = new Vector2(idxLandPage + 0.5f, 0.5f);
            ContentTransform.localPosition = Vector2.zero;

        }));
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

        if(mMSPointInfos.Count == 2)
            EnterZoomMode();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        RemovePointInfo(eventData.pointerId);

        if(mMSPointInfos.Count == 0)
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
        if (curPosX < -PageWidth * 0.5f)
            return PageIndex + 1;
        else if (curPosX >= -PageWidth * 0.5f && curPosX < PageWidth * 0.5f)
            return PageIndex;
        else
            return PageIndex - 1;
    }
    void UpdateDrag(PointerEventData eventData)
    {
        Vector2 vOrizin = transform.InverseTransformPoint(Vector2.zero);
        Vector2 vRet = transform.InverseTransformPoint(eventData.delta);

        if (mIsZoomMode)
        {
            ContentTransform.localPosition += new Vector3(vRet.x - vOrizin.x, vRet.y - vOrizin.y, .0f);
        }
        else
        {
            ContentTransform.localPosition += new Vector3(vRet.x - vOrizin.x, .0f, .0f);
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
            fQuickDragPoint = fDir * PageWidth * 0.5f;

        // Debug.Log($"Quick Drag Point ! [{fQuickDragPoint}]");
        int idxLandPage = FindLandPage(ContentTransform.localPosition.x + fQuickDragPoint);
        JumpToPage(idxLandPage);
    }
    void EnterZoomMode(bool isEditorMode=false)
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
        while(Time.time-fStartTime < duration)
        {
            float fRate = (Time.time - fStartTime) / duration;
            trMoveTarget.localPosition = Vector2.Lerp(vStart, vTo, fRate);
            yield return null;
        }
        trMoveTarget.localPosition = vTo;

        if(callbackFinish != null)
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
        JumpToPage(0);
    }
    public void OnScaleUpClicked()
    {
        float fRate = 1.1f;
        ContentTransform.localScale *= fRate;

        EnterZoomMode(isEditorMode:true);
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


    #region [BUFFER]
    int FindLandPageOld(float curPosX)
    {
        for (int k = 0; k < TargetViews.Count; ++k)
        {
            bool hit = false;
            float spot = -PageWidth * k;
            if (k == 0)
            {
                if (curPosX >= spot)
                    hit = true;
            }
            else if (k == TargetViews.Count - 1)
            {
                if (curPosX < spot)
                    hit = true;
            }

            if (curPosX >= spot - PageWidth * 0.5f && curPosX < spot + PageWidth * 0.5f)
                hit = true;

            if (hit)
                return k;
        }
        return 0;
    }
    #endregion
}
