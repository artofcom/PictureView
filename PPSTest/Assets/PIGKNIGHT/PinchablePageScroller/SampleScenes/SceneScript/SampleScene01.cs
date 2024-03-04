using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UI.Scroller;


namespace DemoScene
{
    public class SampleScene01 : MonoBehaviour
    {
        [SerializeField] PinchablePageScroller PPScroller;
        [SerializeField] GameObject BtnScaleUp;
        [SerializeField] GameObject BtnScaleDown;

        // Start is called before the first frame update
        void Start()
        {
            bool isEditor = true;
#if !UNITY_EDITOR
            Application.targetFrameRate = 60;
            isEditor = false;
#endif
            BtnScaleUp.SetActive(isEditor);
            BtnScaleDown.SetActive(isEditor);

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
}