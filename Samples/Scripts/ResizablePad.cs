using UnityEngine;
using UnityEngine.EventSystems;

namespace SoftMasking.Samples {
    [RequireComponent(typeof(RectTransform))]
    public class ResizablePad : UIBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
        enum ManipulationType {
            None,
            Move, 
            ResizeLeft, 
            ResizeUp, 
            ResizeRight, 
            ResizeDown, 
            Rotate 
        }

        RectTransform _rectTransform;
        ManipulationType _activeManipulation;

        public float resizeGripSize = 40f;
        public float rotateGripSize = 60f;

        protected override void Awake() {
            base.Awake();
            _rectTransform = GetComponent<RectTransform>();
        }
        
        public void OnBeginDrag(PointerEventData eventData) {
            var localPressPosition = ToLocal(eventData.pressPosition, eventData.pressEventCamera);
            _activeManipulation = ManipulationByPressPosition(localPressPosition);
        }
        
        public void OnDrag(PointerEventData eventData) {
            switch (_activeManipulation) {
                case ManipulationType.Move:
                    _rectTransform.anchoredPosition += eventData.delta;
                    break;
                case ManipulationType.Rotate:
                    // TODO reduce copypaste
                    var prevLever = ToLocal(eventData.position - eventData.delta, eventData.pressEventCamera);
                    var curLever = ToLocal(eventData.position, eventData.pressEventCamera);
                    var prevAngle = Mathf.Atan2(prevLever.y, prevLever.x) * Mathf.Rad2Deg;
                    var curAngle = Mathf.Atan2(curLever.y, curLever.x) * Mathf.Rad2Deg;
                    _rectTransform.localRotation *= Quaternion.AngleAxis(Mathf.DeltaAngle(prevAngle, curAngle), Vector3.forward);
                    break;
            }
        }

        public void OnEndDrag(PointerEventData eventData) {
            _activeManipulation = ManipulationType.None;
        }

        Vector2 ToLocal(Vector2 position, Camera eventCamera) {
            Vector2 localPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rectTransform, position, eventCamera, out localPosition);
            return localPosition;
        }

        ManipulationType ManipulationByPressPosition(Vector2 pressPosition) {
            if (InsideMovementZone(pressPosition))
                return ManipulationType.Move;
            else if (InsideRotationZone(pressPosition))
                return ManipulationType.Rotate;
            else
                return ManipulationType.None;
        }

        bool InsideMovementZone(Vector2 pos) {
            var grip = resizeGripSize / 2;
            var selfRect = _rectTransform.rect;
            return pos.x > selfRect.xMin + grip
                && pos.x < selfRect.xMax - grip
                && pos.y > selfRect.yMin + grip
                && pos.y < selfRect.yMax - grip;
        }

        bool InsideRotationZone(Vector2 pos) {
            var selfRect = _rectTransform.rect;
            return RotationZone(new Vector2(selfRect.xMin, selfRect.yMin)).Contains(pos)
                || RotationZone(new Vector2(selfRect.xMin, selfRect.yMax)).Contains(pos)
                || RotationZone(new Vector2(selfRect.xMax, selfRect.yMin)).Contains(pos)
                || RotationZone(new Vector2(selfRect.xMax, selfRect.yMax)).Contains(pos);
        }

        Rect RotationZone(Vector2 anchor) {
            var grip = rotateGripSize / 2;
            return new Rect(anchor.x - grip, anchor.y - grip, rotateGripSize, rotateGripSize);
        }
    }
}
