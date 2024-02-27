using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System;
using UnityEngine.Events;

namespace UI.Scroller
{
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
        [SerializeField] public int PageIndex = 0;
        [SerializeField] RectTransform FitTargetTransform = null;


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
        float mPageWidth = 0.0f;
        List<PointInfo> mMSPointInfos = new List<PointInfo>();
        float mZoomStartPointsDist;
        Vector3 mZoomStartScale;
        List<Vector2> mOriginalScales = new List<Vector2>();
        List<Vector2> mOriginalLocs = new List<Vector2>();
        float mMSDownTime;
        Vector2 mMSDownPos;
        bool mIsZoomMode = false;
        Vector2 mVOrigin = Vector2.zero;
        GameObject mScaleRoot;
        bool mIsTransitioning = false;
        float PAGE_ORIGIN_X(int IndexPage) => -IndexPage * mPageWidth;

        #endregion


        #region [MONO_EVENTS]

        // Start is called before the first frame update
        void Start()
        {
            UnityEngine.Assertions.Assert.IsTrue(Canvas != null, "Canvas Can't be null!");
            UnityEngine.Assertions.Assert.IsTrue(ContentTransform != null, "ContentTransform Can't be null!");

            Camera uiCamera = Canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : UICamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Canvas.GetComponent<RectTransform>(), Vector2.zero, uiCamera, out mVOrigin);
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

            Vector2 vScreenSize = new Vector2(Screen.width, Screen.height);
            Camera uiCamera = Canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : UICamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Canvas.GetComponent<RectTransform>(), vScreenSize, uiCamera, out vScreenSize);
            mPageWidth = 2.0f * vScreenSize.x;

            Vector2 vPos = Vector2.zero;

            ContentTransform.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
            ContentTransform.localPosition = Vector2.zero;

            mOriginalLocs.Clear();
            mOriginalScales.Clear();
            for (int k = 0; k < TargetViews.Count; ++k)
            {
                TargetViews[k].localPosition = vPos;
                vPos = new Vector2(vPos.x + mPageWidth, vPos.y);
                mOriginalScales.Add(TargetViews[k].localScale);
                mOriginalLocs.Add(TargetViews[k].localPosition);
            }

            mIsZoomMode = false;


            JumpToPage(PageIndex, true);

            if (FitTargetTransform != null)
            {
                float screenRatio = ((float)Screen.width) / ((float)Screen.height);
                for (int k = 0; k < TargetViews.Count; ++k)
                {
                    float viewRatio = ((float)TargetViews[k].rect.width) / ((float)TargetViews[k].rect.height);

                    float fRate = screenRatio < viewRatio ?
                        ((float)FitTargetTransform.rect.width) / ((float)TargetViews[k].rect.width) :
                        ((float)FitTargetTransform.rect.height) / ((float)TargetViews[k].rect.height);
                    TargetViews[k].localScale *= fRate;
                    mOriginalScales[k] = TargetViews[k].localScale;
                }
            }
        }
        public void Reset()
        {
            TargetViews.Clear();
        }
        public void JumpToPage(int idxLandPage, bool instantJump = false)
        {
            idxLandPage = Math.Clamp(idxLandPage, 0, TargetViews.Count - 1);

            void OnFinishedJumpToPage()
            {
                int oldPage = PageIndex;
                PageIndex = idxLandPage;
                ContentTransform.localPosition = new Vector2(PAGE_ORIGIN_X(PageIndex), .0f);

                if (oldPage != PageIndex)
                    OnPageChangeEnded?.Invoke(PageIndex);
            }

            if (instantJump)
                OnFinishedJumpToPage();
            else
                StartCoroutine(coMoveTo(ContentTransform, new Vector2(PAGE_ORIGIN_X(idxLandPage), .0f), TweenTime, OnFinishedJumpToPage));
        }
        public void JumpToHome()
        {
            ExitZoomMode();
            JumpToPage(0);
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
            {
                StartZoom(isEditorMode: false);
                if (!mIsZoomMode)
                    EnterZoomMode();
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            RemovePointInfo(eventData.pointerId);

            if (mIsZoomMode)
            {
                if (mScaleRoot != null)
                {
                    Vector2 vCurScale = new Vector2(mScaleRoot.transform.localScale.x * TargetViews[PageIndex].localScale.x,
                                                    mScaleRoot.transform.localScale.y * TargetViews[PageIndex].localScale.y);

                    float fDiff = vCurScale.magnitude - mOriginalScales[PageIndex].magnitude;
                    if (fDiff < .0f)
                        ExitZoomMode();


                    EndZoom();
                }
            }
            else
            {
                if (!mIsTransitioning)
                    LandToNearestPage(eventData.position.x);
            }
        }
        #endregion


        #region [HELPERS]

        // search land pos.
        int FindLandPage(float curPosX)
        {
            float pageBasedPosX = curPosX - PAGE_ORIGIN_X(PageIndex);
            if (pageBasedPosX < -mPageWidth * 0.5f)
                return PageIndex + 1;
            else if (pageBasedPosX >= -mPageWidth * 0.5f && pageBasedPosX < mPageWidth * 0.5f)
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
                float xLimit = ContentTransform.rect.width - TargetViews[PageIndex].rect.width * TargetViews[PageIndex].localScale.x;
                float yLimit = ContentTransform.rect.height - TargetViews[PageIndex].rect.height * TargetViews[PageIndex].localScale.y;

                float pageX = mPageWidth * PageIndex;
                float xOffset = vRet.x - mVOrigin.x;
                float yOffset = vRet.y - mVOrigin.y;
                float xPos = Mathf.Clamp(TargetViews[PageIndex].localPosition.x + xOffset, -Math.Abs(xLimit) * 0.5f + pageX, Math.Abs(xLimit) * 0.5f + pageX);
                float yPos = Mathf.Clamp(TargetViews[PageIndex].localPosition.y + yOffset, -Math.Abs(yLimit) * 0.5f, Math.Abs(yLimit) * 0.5f);

                TargetViews[PageIndex].localPosition = new Vector3(xPos, yPos, TargetViews[PageIndex].localPosition.z);
            }
            else
            {
                if(!mIsTransitioning)
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
                mScaleRoot.transform.localScale = mZoomStartScale * fRate;

                Debug.Log($"Cur ScaleRoot scale : {mScaleRoot.transform.localScale.x}, {mScaleRoot.transform.localScale.y} ");
            }
        }
        void LandToNearestPage(float MSXPos)
        {
            float fDir = mMSDownPos.x - MSXPos < .0f ? 1.0f : -1.0f;
            float fQuickDragPoint = .0f;
            if (Time.time - mMSDownTime < QuickDragDuration)
                fQuickDragPoint = fDir * mPageWidth * 0.5f;

            int idxLandPage = FindLandPage(ContentTransform.localPosition.x + fQuickDragPoint);
            JumpToPage(idxLandPage);
        }

        void StartZoom(bool isEditorMode)
        {
            Vector2 vRet;
            Camera uiCamera = Canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : UICamera;

            vRet = isEditorMode ? new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) : Vector2.Lerp(mMSPointInfos[0].vPos, mMSPointInfos[1].vPos, 0.5f);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(TargetViews[PageIndex].parent.GetComponent<RectTransform>(), vRet, uiCamera, out vRet);

            if (mScaleRoot != null)
                Destroy(mScaleRoot);

            mScaleRoot = new GameObject();
            mScaleRoot.transform.SetParent(ContentTransform, false);
            mScaleRoot.transform.localPosition = new Vector3(vRet.x, vRet.y, .0f);
            TargetViews[PageIndex].SetParent(mScaleRoot.transform, true);

            if (!isEditorMode)
            {
                mZoomStartPointsDist = (mMSPointInfos[0].vPos - mMSPointInfos[1].vPos).magnitude;
                mZoomStartScale = mScaleRoot.transform.localScale;
            }
        }
        void EndZoom()
        {
            TargetViews[PageIndex].SetParent(ContentTransform, true);
            GameObject.Destroy(mScaleRoot);
            mScaleRoot = null;
        }

        void EnterZoomMode()
        {
            if (mIsZoomMode) return;

            for (int k = 0; k < TargetViews.Count; ++k)
                TargetViews[k].gameObject.SetActive(k == PageIndex);

            mIsZoomMode = true;

        }
        void ExitZoomMode()
        {
            mIsZoomMode = false;

            for (int k = 0; k < TargetViews.Count; ++k)
                TargetViews[k].gameObject.SetActive(true);

            mIsTransitioning = true;

            StartCoroutine(coActionWithDelay(0.05f, () =>
            {
                StartCoroutine(coScaleTo(TargetViews[PageIndex], mOriginalScales[PageIndex], TweenTime));
                StartCoroutine(coMoveTo(TargetViews[PageIndex], mOriginalLocs[PageIndex], TweenTime, () => mIsTransitioning = false));

            }));
        }
        #endregion


        #region [TWEENERS]

        IEnumerator coMoveTo(Transform trMoveTarget, Vector2 vTo, float duration, Action callbackFinish = null)
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
        IEnumerator coActionWithDelay(float delay, Action action)
        {
            if (delay > .0f)
                yield return new WaitForSeconds(delay);

            action.Invoke();
        }

        #endregion


        #region [EDITOR TESTING ONLY]
        public void ScaleWithPinch(float fRate)
        {
#if UNITY_EDITOR

            StartZoom(isEditorMode: true);

            mScaleRoot.transform.localScale *= fRate;

            if (fRate >= 1.0f)
            {
                if (!mIsZoomMode)
                    EnterZoomMode();
            }
            else
            {
                Vector2 vCurScale = fRate * TargetViews[PageIndex].localScale;
                float fDiff = vCurScale.magnitude - mOriginalScales[PageIndex].magnitude;

                if (fDiff < .0f)
                    ExitZoomMode();
            }

            EndZoom();
#endif
        }
        #endregion


        #region [Buffer Control]

        // Point Buffer control ---------------------------------------------------//
        //
        int FindPointInfoIndex(int id)
        {
            int idxRet = mMSPointInfos.FindIndex(x => x.ID == id);
            if (idxRet >= 0 && idxRet < mMSPointInfos.Count)
                return idxRet;
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
}