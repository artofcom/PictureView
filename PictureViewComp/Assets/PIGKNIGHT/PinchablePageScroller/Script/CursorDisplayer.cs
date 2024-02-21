using UnityEngine;

namespace UI.Helper
{
    public class CursorDisplayer : MonoBehaviour
    {
        [SerializeField] RectTransform TransCursor00;
        [SerializeField] RectTransform TransCursor01;

        [SerializeField] Canvas Canvas;
        [SerializeField] Camera UICamera;

        // Start is called before the first frame update
        void Start()
        {
            UnityEngine.Assertions.Assert.IsTrue(Canvas != null, "Canvas Can't be null!");
            UnityEngine.Assertions.Assert.IsTrue(TransCursor00 != null, "TransCursor00 Can't be null!");
            UnityEngine.Assertions.Assert.IsTrue(TransCursor01 != null, "TransCursor01 Can't be null!");

            if (TransCursor00 != null)
                TransCursor00.gameObject.SetActive(false);
            if (TransCursor01 != null)
                TransCursor01.gameObject.SetActive(false);
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetMouseButton(0))
            {
                TransCursor00.gameObject.SetActive(true);
                Touch tc = Input.GetTouch(0);
                Vector2 worldPos;
                Camera UICam = Canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : UICamera;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(Canvas.GetComponent<RectTransform>(), tc.position, UICam, out worldPos);
                TransCursor00.anchoredPosition = worldPos;

                if (Input.touchCount == 2)
                {
                    TransCursor01.gameObject.SetActive(true);
                    tc = Input.GetTouch(1);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(Canvas.GetComponent<RectTransform>(), tc.position, UICam, out worldPos);
                    TransCursor01.anchoredPosition = worldPos;
                }
                else
                    TransCursor01.gameObject.SetActive(false);
            }
            else
            {
                TransCursor00.gameObject.SetActive(false);
                TransCursor01.gameObject.SetActive(false);
            }
        }
    }
}
