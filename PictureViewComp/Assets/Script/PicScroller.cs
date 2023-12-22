using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PointInfo
{
    public int ID;
    public Vector2 vPos;
}

public class PicScroller : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] Transform TargetTransform;

    List<PointInfo> PointInfos = new List<PointInfo>();
    float StartLength;
    Vector3 StartScale;

    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = 60;
    }

    // Update is called once per frame
    void Update()
    {

    }

    



    // Pointer Event Handlers --------------------------------------------------//
    //
    public void OnDrag(PointerEventData eventData)
    {
        // Drag Mode.
        if (PointInfos.Count == 1)
        {
            TargetTransform.position += new Vector3(eventData.delta.x, eventData.delta.y, .0f);
            PointInfos[0].vPos = eventData.position;
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
                TargetTransform.localScale = StartScale * fRate;
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        UpdatePointInfo(eventData.pointerId, eventData.position);
        
        if(PointInfos.Count == 2)
        {
            StartLength = (PointInfos[0].vPos - PointInfos[1].vPos).magnitude;
            StartScale = TargetTransform.localScale;

            // Debug.Log($"2 Pointers are Down ! [{StartLength}]");
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        RemovePointInfo(eventData.pointerId);
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
