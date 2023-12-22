using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class Scroller : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] Transform ContentTransform;

    [SerializeField] List<RectTransform> TargetViews = new List<RectTransform>();
    [SerializeField] float PageWidth = 800.0f;
    [SerializeField] float TweenTime = 0.6f;
    [SerializeField] float QuickDragDuration = 0.2f;

    List<PointInfo> PointInfos = new List<PointInfo>();
    float StartLength;
    Vector3 StartScale;
    Vector2 mOriginalScale;

    float MSDownTime;
    Vector2 MSDownPos;
    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = 60;

        Vector2 vPos = Vector2.zero;
        for(int k = 0; k < TargetViews.Count; ++k)
        {
            TargetViews[k].localPosition = vPos;
            vPos = new Vector2(vPos.x + PageWidth, vPos.y);
        }
        mOriginalScale = ContentTransform.localScale;
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log("Update");
    }

    



    // Pointer Event Handlers --------------------------------------------------//
    //
    public void OnDrag(PointerEventData eventData)
    {
        
        // Drag Mode.
        if (PointInfos.Count == 1)
        {
            Vector2 vOrizin = transform.InverseTransformPoint(Vector2.zero);
            Vector2 vRet = transform.InverseTransformPoint(eventData.delta);
            
            ContentTransform.localPosition += new Vector3(vRet.x-vOrizin.x, .0f, .0f);
            PointInfos[0].vPos = eventData.position;

            //Debug.Log("Drag");
        }
        

        // Pinch Mode.
        else if (PointInfos.Count == 2)
        {
            if (eventData.pointerId == PointInfos[0].ID)
            {
                PointInfos[0].vPos = eventData.position;
            }
            else
            {
                PointInfos[1].vPos = eventData.position;

                float curLength = (PointInfos[0].vPos - PointInfos[1].vPos).magnitude;
                float fRate = curLength / StartLength;
                ContentTransform.localScale = StartScale * fRate;
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        UpdatePointInfo(eventData.pointerId, eventData.position);

        MSDownTime = Time.time;
        MSDownPos = eventData.position;

        
        if(PointInfos.Count == 2)
        {
            StartLength = (PointInfos[0].vPos - PointInfos[1].vPos).magnitude;
            StartScale = ContentTransform.localScale;

            // Debug.Log($"2 Pointers are Down ! [{StartLength}]");
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        float fDir = MSDownPos.x - eventData.position.x < .0f ? 1.0f : -1.0f;
        RemovePointInfo(eventData.pointerId);

        if(PointInfos.Count == 0)
        {
            Vector2 vCurScale = ContentTransform.localScale;
            float fDiff = vCurScale.magnitude - mOriginalScale.magnitude;
            if(Mathf.Abs(fDiff) < 0.01f)
            {
                float fQuickDragPoint = .0f;
                if (Time.time - MSDownTime < QuickDragDuration)
                    fQuickDragPoint = fDir * PageWidth * 0.5f;

                Debug.Log($"Quick Drag Point ! [{fQuickDragPoint}]");
                int idxLandPage = FindLandPage(ContentTransform.localPosition.x + fQuickDragPoint);
                float fLandPos = -PageWidth * (float)idxLandPage;
                StartCoroutine(coMoveTo(ContentTransform, new Vector2(fLandPos, .0f), TweenTime, () =>
                {
                    ContentTransform.GetComponent<RectTransform>().pivot = new Vector2(idxLandPage + 0.5f, 0.5f);
                    ContentTransform.localPosition = Vector2.zero;

                }));
            }
            else if (fDiff < .0f)
                StartCoroutine(coScaleTo(ContentTransform, mOriginalScale, TweenTime));
        }
    }


    // search land pos.
    int FindLandPage(float curPosX)
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



    #region [Buffer Control]

    // Point Buffer ----------------------------------------------------------//
    //
    int FindPointInfoIndex(int id)
    {
        for (int k = 0; k < PointInfos.Count; ++k)
        {
            if (PointInfos[k].ID == id)
                return k;
        }
        return -1;
    }

    void RemovePointInfo(int id)
    {
        int index = FindPointInfoIndex(id);
        if (index >= 0)
            PointInfos.RemoveAt(index);
    }

    void UpdatePointInfo(int id, Vector2 vPos)
    {
        int index = FindPointInfoIndex(id);
        if (index >= 0)
            PointInfos.RemoveAt(index);

        PointInfo info = new PointInfo();
        info.ID = id;
        info.vPos = vPos;

        PointInfos.Add(info);
    }

    #endregion 
}
