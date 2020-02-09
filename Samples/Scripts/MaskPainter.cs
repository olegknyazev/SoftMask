using UnityEngine;
using UnityEngine.EventSystems;

namespace SoftMasking.Samples {
    [RequireComponent(typeof(RectTransform))]
    public class MaskPainter : UIBehaviour, IDragHandler {
        public RectTransform stroke;

        RectTransform _rectTransform;

        protected override void Awake() {
            base.Awake();
            _rectTransform = GetComponent<RectTransform>();
        }

        protected override void Start() {
            base.Start();
            
        }

        public void OnDrag(PointerEventData eventData) {
            Vector2 localPosition;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, eventData.position, null, out localPosition)) {
                stroke.anchoredPosition = localPosition;
            }
        }
    }
}
