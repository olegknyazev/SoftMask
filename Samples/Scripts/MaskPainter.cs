using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoftMasking.Samples {
    [RequireComponent(typeof(RectTransform))]
    public class MaskPainter : UIBehaviour, IPointerDownHandler, IDragHandler {
        public Graphic stroke;
        RectTransform _rectTransform;
        RectTransform _strokeTranform;

        protected override void Awake() {
            base.Awake();
            _rectTransform = GetComponent<RectTransform>();
            _strokeTranform = stroke.GetComponent<RectTransform>();
        }

        protected override void Start() {
            base.Start();
            stroke.enabled = false;
        }

        public void OnPointerDown(PointerEventData eventData) {
            UpdateStrokeByEvent(eventData);
        }

        public void OnDrag(PointerEventData eventData) {
            UpdateStrokeByEvent(eventData);
        }

        void UpdateStrokeByEvent(PointerEventData eventData) {
            UpdateStrokePosition(eventData.position);
            UpdateStrokeColor(eventData.button);
        }

        void UpdateStrokePosition(Vector2 screenPosition) {
            Vector2 localPosition;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, screenPosition, null, out localPosition)) {
                _strokeTranform.anchoredPosition = localPosition;
                stroke.enabled = true;
            }
        }

        void UpdateStrokeColor(PointerEventData.InputButton pressedButton) {
            stroke.materialForRendering.SetInt("_BlendOp", 
                pressedButton == PointerEventData.InputButton.Left
                    ? (int)UnityEngine.Rendering.BlendOp.Add
                    : (int)UnityEngine.Rendering.BlendOp.ReverseSubtract);
        }
    }
}
