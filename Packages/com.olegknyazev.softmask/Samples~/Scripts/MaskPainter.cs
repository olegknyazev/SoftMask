using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoftMasking.Samples {
    [RequireComponent(typeof(RectTransform))]
    public class MaskPainter : UIBehaviour, IPointerDownHandler, IDragHandler {
        public Graphic spot;
        public RectTransform stroke;
        RectTransform _rectTransform;

        protected override void Awake() {
            base.Awake();
            _rectTransform = GetComponent<RectTransform>();
        }

        protected override void Start() {
            base.Start();
            spot.enabled = false;
        }

        public void OnPointerDown(PointerEventData eventData) {
            UpdateStrokeByEvent(eventData);
        }

        public void OnDrag(PointerEventData eventData) {
            UpdateStrokeByEvent(eventData, drawTrail: true);
        }

        void UpdateStrokeByEvent(PointerEventData eventData, bool drawTrail = false) {
            UpdateStrokePosition(eventData.position, drawTrail);
            UpdateStrokeColor(eventData.button);
        }

        void UpdateStrokePosition(Vector2 screenPosition, bool drawTrail = false) {
            Vector2 localPosition;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, screenPosition, null, out localPosition)) {
                var prevPosition = stroke.anchoredPosition;
                stroke.anchoredPosition = localPosition;
                if (drawTrail)
                    SetUpTrail(prevPosition);
                spot.enabled = true;
            }
        }

        void SetUpTrail(Vector2 prevPosition) {
            var movement = stroke.anchoredPosition - prevPosition;
            stroke.localRotation = Quaternion.AngleAxis(Mathf.Atan2(movement.y, movement.x) * Mathf.Rad2Deg, Vector3.forward);
            spot.rectTransform.offsetMin = new Vector2(-movement.magnitude, 0);
        }

        void UpdateStrokeColor(PointerEventData.InputButton pressedButton) {
            spot.materialForRendering.SetInt("_BlendOp", 
                pressedButton == PointerEventData.InputButton.Left
                    ? (int)UnityEngine.Rendering.BlendOp.Add
                    : (int)UnityEngine.Rendering.BlendOp.ReverseSubtract);
        }
    }
}
