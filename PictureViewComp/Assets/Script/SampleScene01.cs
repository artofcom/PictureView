using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SampleScene01 : MonoBehaviour
{
    [SerializeField] PinchablePageScroller PPScroller;

    // Start is called before the first frame update
    void Start()
    {
#if !UNITY_EDITOR
        Application.targetFrameRate = 60;
#endif

        if (PPScroller != null)
        {
            PPScroller.Trigger();
            PPScroller.OnPageChangeEnded.AddListener(OnPageChangeEnded);
        }
    }


    public void OnHomeClicked()
    {
        if (PPScroller != null)
            PPScroller.JumpToHome();
    }
    public void OnScaleUpClicked()
    {
        float fRate = 1.1f;
        if (PPScroller != null)
            PPScroller.ScaleWithPinch(fRate);
    }
    public void OnScaleDownClicked()
    {
        float fRate = 0.95f;
        if (PPScroller != null)
            PPScroller.ScaleWithPinch(fRate);
    }
    void OnPageChangeEnded(int index)
    {
        Debug.Log($"OnPageChanged index:{index}");
    }
}
