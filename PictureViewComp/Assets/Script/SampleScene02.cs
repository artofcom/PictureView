using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SampleScene02 : MonoBehaviour
{
    [SerializeField] PinchablePageScroller PPScroller;
    [SerializeField] List<RectTransform> ListItems;

    // Start is called before the first frame update
    void Start()
    {
#if !UNITY_EDITOR
        Application.targetFrameRate = 60;
#endif

        if (ListItems != null && PPScroller != null)
        {
            for (int k = 0; k < ListItems.Count; ++k)
            {
                GameObject obj = Instantiate<GameObject>(ListItems[k].gameObject);
                obj.SetActive(true);

                PPScroller.AddTargetView((RectTransform)obj.transform);
            }
            PPScroller.Trigger();
        }
    }
}
