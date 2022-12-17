using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoftMasking.Samples {
    public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
        public RectTransform tooltip;

        public void LateUpdate() {
            if (tooltip.gameObject.activeInHierarchy) {
                Vector2 position;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        tooltip.parent.GetComponent<RectTransform>(),
                        Input.mousePosition,
                        null,
                        out position))
                    tooltip.anchoredPosition = position + new Vector2(10.0f, -20.0f);
            }
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) {
            tooltip.gameObject.SetActive(true);
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData) {
            tooltip.gameObject.SetActive(false);
        }
    }
}
