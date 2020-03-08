using UnityEngine;
using UnityEngine.EventSystems;

namespace SoftMasking.Samples {
    [RequireComponent(typeof(RectTransform))]
    public class MaskPainter : UIBehaviour, IPointerDownHandler, IDragHandler {
        public RectTransform stroke;

        RectTransform _rectTransform;

        protected override void Awake() {
            base.Awake();
            _rectTransform = GetComponent<RectTransform>();
        }

        protected override void Start() {
            base.Start();
            stroke.gameObject.SetActive(false);
        }

        public void OnPointerDown(PointerEventData eventData) {
            UpdateStrokePosition(eventData.position);
        }

        public void OnDrag(PointerEventData eventData) {
            UpdateStrokePosition(eventData.position);
        }

        void UpdateStrokePosition(Vector2 screenPosition) {
            Vector2 localPosition;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, screenPosition, null, out localPosition)) {
                stroke.anchoredPosition = localPosition;
                stroke.gameObject.SetActive(true);
            }
        }
    }
}
