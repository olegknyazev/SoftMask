using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SoftMasking.Samples {
    [RequireComponent(typeof(RectTransform))]
    public class RectManipulator : UIBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
        [Flags] public enum ManipulationType {
            None = 0,
            Move = 1 << 0,
            ResizeLeft = 1 << 1,
            ResizeUp = 1 << 2,
            ResizeRight = 1 << 3,
            ResizeDown = 1 << 4,
            ResizeUpLeft = ResizeUp | ResizeLeft,
            ResizeUpRight = ResizeUp | ResizeRight,
            ResizeDownLeft = ResizeDown | ResizeLeft,
            ResizeDownRight = ResizeDown | ResizeRight,
            Rotate = 1 << 5
        }

        ManipulationType _activeManipulation;

        public RectTransform targetTransform;
        public ManipulationType manipulation;
        
        public void OnBeginDrag(PointerEventData eventData) {
            _activeManipulation = manipulation;
        }
        
        public void OnDrag(PointerEventData eventData) {
            if (targetTransform == null)
                return;
            var prevLocalPoint = ToLocal(eventData.position - eventData.delta, eventData.pressEventCamera);
            var curLocalPoint = ToLocal(eventData.position, eventData.pressEventCamera);
            var localDelta = curLocalPoint - prevLocalPoint;
            if (ActiveManipulationIs(ManipulationType.Move))
                targetTransform.anchoredPosition += eventData.delta;
            if (ActiveManipulationIs(ManipulationType.Rotate)) {
                // TODO reduce copypaste
                var prevAngle = Mathf.Atan2(prevLocalPoint.y, prevLocalPoint.x) * Mathf.Rad2Deg;
                var curAngle = Mathf.Atan2(curLocalPoint.y, curLocalPoint.x) * Mathf.Rad2Deg;
                targetTransform.localRotation *= Quaternion.AngleAxis(Mathf.DeltaAngle(prevAngle, curAngle), Vector3.forward);
            }
            if (ActiveManipulationIs(ManipulationType.ResizeLeft))
                DirectedResize(HorizontalProjection(localDelta), -1f);
            if (ActiveManipulationIs(ManipulationType.ResizeUp))
                DirectedResize(VerticalProjection(localDelta), +1f);
            if (ActiveManipulationIs(ManipulationType.ResizeRight))
                DirectedResize(HorizontalProjection(localDelta), +1f);
            if (ActiveManipulationIs(ManipulationType.ResizeDown))
                DirectedResize(VerticalProjection(localDelta), -1f);
        }

        bool ActiveManipulationIs(ManipulationType manipulation) {
            return (_activeManipulation & manipulation) == manipulation;
        }

        void DirectedResize(Vector2 localResizeDelta, float direction) { // TODO it's unclear what `direction` is
            targetTransform.sizeDelta += localResizeDelta * direction;
            targetTransform.position += targetTransform.TransformVector(localResizeDelta) / 2;
        }

        Vector2 HorizontalProjection(Vector2 vec) { return new Vector2(vec.x, 0f); }
        Vector2 VerticalProjection(Vector2 vec) { return new Vector2(0f, vec.y); }

        public void OnEndDrag(PointerEventData eventData) {
            _activeManipulation = ManipulationType.None;
        }

        Vector2 ToLocal(Vector2 position, Camera eventCamera) {
            Vector2 localPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                targetTransform, position, eventCamera, out localPosition);
            return localPosition;
        }
    }
}
