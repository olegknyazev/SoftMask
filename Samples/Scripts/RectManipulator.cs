using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoftMasking.Samples {
    [RequireComponent(typeof(RectTransform))]
    public class RectManipulator : UIBehaviour,
            IPointerEnterHandler,
            IPointerExitHandler,
            IBeginDragHandler,
            IDragHandler,
            IEndDragHandler {

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

        public RectTransform targetTransform;
        public ManipulationType manipulation;
        
        [Header("Display")]
        public Graphic icon;
        public float normalAlpha = 0.2f;
        public float selectedAlpha = 1f;
        public float transitionDuration = 0.2f;

        bool _isManipulatedNow;
        
        public void OnPointerEnter(PointerEventData eventData) {
            HighlightIcon(true);
        }

        public void OnPointerExit(PointerEventData eventData) {
            if (!_isManipulatedNow)
                HighlightIcon(false);
        }

        void HighlightIcon(bool highlight, bool instant = false) {
            if (icon) {
                var targetAlpha = highlight ? selectedAlpha : normalAlpha;
                var duration = instant ? 0f : transitionDuration;
                icon.CrossFadeAlpha(targetAlpha, duration, true);
            }
        }

        protected override void Start() {
            base.Start();
            HighlightIcon(false, instant: true);
        }

        public void OnBeginDrag(PointerEventData eventData) {
            _isManipulatedNow = true;
        }
        
        public void OnDrag(PointerEventData eventData) {
            if (targetTransform == null || !_isManipulatedNow)
                return;
            var prevLocalPoint = ToLocal(eventData.position - eventData.delta, eventData.pressEventCamera);
            var curLocalPoint = ToLocal(eventData.position, eventData.pressEventCamera);
            DoMove(eventData);
            DoRotate(prevLocalPoint, curLocalPoint);
            var localDelta = curLocalPoint - prevLocalPoint;
            DoResize(localDelta);
        }

        Vector2 ToLocal(Vector2 position, Camera eventCamera) {
            Vector2 localPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                targetTransform, position, eventCamera, out localPosition);
            return localPosition;
        }

        void DoMove(PointerEventData eventData) {
            if (Is(ManipulationType.Move))
                targetTransform.anchoredPosition += eventData.delta;
        }

        bool Is(ManipulationType expected) {
            return (manipulation & expected) == expected;
        }

        void DoRotate(Vector2 prevLocalPoint, Vector2 curLocalPoint) {
            if (Is(ManipulationType.Rotate))
                targetTransform.localRotation *= Quaternion.AngleAxis(DeltaRotation(prevLocalPoint, curLocalPoint), Vector3.forward);
        }

        float DeltaRotation(Vector2 prevLocalPoint, Vector2 curLocalPoint) {
            var prevAngle = Mathf.Atan2(prevLocalPoint.y, prevLocalPoint.x) * Mathf.Rad2Deg;
            var curAngle = Mathf.Atan2(curLocalPoint.y, curLocalPoint.x) * Mathf.Rad2Deg;
            return Mathf.DeltaAngle(prevAngle, curAngle);
        }

        void DoResize(Vector2 localDelta) {
            var horizontalDelta = HorizontalProjection(localDelta);
            if (Is(ManipulationType.ResizeLeft))
                ResizeDirected(horizontalDelta, -1f);
            else if (Is(ManipulationType.ResizeRight))
                ResizeDirected(horizontalDelta, +1f);

            var verticalDelta = VerticalProjection(localDelta);
            if (Is(ManipulationType.ResizeUp))
                ResizeDirected(verticalDelta, +1f);
            else if (Is(ManipulationType.ResizeDown))
                ResizeDirected(verticalDelta, -1f);
        }

        void ResizeDirected(Vector2 localResizeDelta, float sizeSign) {
            targetTransform.sizeDelta += localResizeDelta * sizeSign;
            targetTransform.position += targetTransform.TransformVector(localResizeDelta) / 2;
        }

        Vector2 HorizontalProjection(Vector2 vec) { return new Vector2(vec.x, 0f); }
        Vector2 VerticalProjection(Vector2 vec) { return new Vector2(0f, vec.y); }

        public void OnEndDrag(PointerEventData eventData) {
            _isManipulatedNow = false;
            if (!eventData.hovered.Contains(gameObject))
                HighlightIcon(false);
        }
    }
}
