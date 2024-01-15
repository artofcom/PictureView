using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SampleScene03 : MonoBehaviour
{
    [SerializeField] PinchablePageScroller PPScroller;

    // Start is called before the first frame update
    void Start()
    {
#if !UNITY_EDITOR
        Application.targetFrameRate = 60;
#endif

        if (PPScroller != null)
            PPScroller.Trigger();
    }
}
