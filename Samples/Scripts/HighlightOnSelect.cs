using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoftMasking.Samples {
    [RequireComponent(typeof(RectTransform))]
    public class HighlightOnSelect : UIBehaviour, IPointerEnterHandler, IPointerExitHandler {
        public Graphic[] targetGraphics;

        public void OnPointerEnter(PointerEventData eventData) {
            foreach (var graphic in targetGraphics)
                graphic.CrossFadeAlpha(1f, 0.2f, true);
        }

        public void OnPointerExit(PointerEventData eventData) {
            foreach (var graphic in targetGraphics)
                graphic.CrossFadeAlpha(0f, 0.2f, true);
        }
    }
}
