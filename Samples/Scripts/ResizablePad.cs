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
        public float rotateGripSize = 40f;

        protected override void Awake() {
            base.Awake();
            _rectTransform = GetComponent<RectTransform>();
        }
        
        public void OnBeginDrag(PointerEventData eventData) {
            var localPressPosition = ToLocal(eventData.pressPosition, eventData.pressEventCamera);
            _activeManipulation = ManipulationByPressPosition(localPressPosition);
        }
        
        public void OnDrag(PointerEventData eventData) {
            var prevLocalPoint = ToLocal(eventData.position - eventData.delta, eventData.pressEventCamera);
            var curLocalPoint = ToLocal(eventData.position, eventData.pressEventCamera);
            var localDelta = curLocalPoint - prevLocalPoint;
            switch (_activeManipulation) {
                case ManipulationType.Move:
                    _rectTransform.anchoredPosition += eventData.delta;
                    break;
                case ManipulationType.Rotate:
                    // TODO reduce copypaste
                    var prevAngle = Mathf.Atan2(prevLocalPoint.y, prevLocalPoint.x) * Mathf.Rad2Deg;
                    var curAngle = Mathf.Atan2(curLocalPoint.y, curLocalPoint.x) * Mathf.Rad2Deg;
                    _rectTransform.localRotation *= Quaternion.AngleAxis(Mathf.DeltaAngle(prevAngle, curAngle), Vector3.forward);
                    break;
                case ManipulationType.ResizeLeft:
                    DirectedResize(HorizontalProjection(localDelta), -1f);
                    break;
                case ManipulationType.ResizeUp:
                    DirectedResize(VerticalProjection(localDelta), -1f);
                    break;
                case ManipulationType.ResizeRight:
                    DirectedResize(HorizontalProjection(localDelta), +1f);
                    break;
                case ManipulationType.ResizeDown:
                    DirectedResize(VerticalProjection(localDelta), +1f);
                    break;
            }
        }

        void DirectedResize(Vector2 localResizeDelta, float direction) { // TODO it's unclear what `direction` is
            _rectTransform.sizeDelta += localResizeDelta * direction;
            _rectTransform.position += _rectTransform.TransformVector(localResizeDelta) / 2;
        }

        Vector2 HorizontalProjection(Vector2 vec) { return new Vector2(vec.x, 0f); }
        Vector2 VerticalProjection(Vector2 vec) { return new Vector2(0f, vec.y); }

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
            else // TODO resize by corner too?
                 // TODO offset rotation grip a bit outside?
                return MovementManipulationBySector(pressPosition);
        }

        bool InsideMovementZone(Vector2 pos) {
            var grip = resizeGripSize;
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
            var grip = rotateGripSize;
            return new Rect(anchor.x - grip, anchor.y - grip, rotateGripSize, rotateGripSize);
        }

        ManipulationType MovementManipulationBySector(Vector2 pos) {
            var normalizedPos = new Vector2(pos.x / _rectTransform.rect.width, pos.y / _rectTransform.rect.height);
            if (Mathf.Abs(normalizedPos.x) > Mathf.Abs(normalizedPos.y))
                return normalizedPos.x >= 0f ? ManipulationType.ResizeRight : ManipulationType.ResizeLeft;
            else
                return normalizedPos.y >= 0f ? ManipulationType.ResizeDown : ManipulationType.ResizeUp;
        }
    }
}
