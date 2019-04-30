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
        public ShowOnHover showOnHover;
        
        [Header("Limits")]
        public Vector2 minSize;

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
            if (showOnHover)
                showOnHover.forcedVisible = highlight;
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
                Move(eventData.delta);
        }

        void Move(Vector2 delta) {
            targetTransform.anchoredPosition = ClampPosition(targetTransform.anchoredPosition + delta);
        }

        bool Is(ManipulationType expected) {
            return (manipulation & expected) == expected;
        }

        Vector2 ClampPosition(Vector2 position) {
            if (targetTransform && targetTransform.parent is RectTransform) {
                var parentRect = ((RectTransform)targetTransform.parent).rect;
                return new Vector2(
                    Mathf.Clamp(position.x, 0f, parentRect.width),
                    Mathf.Clamp(position.y, -parentRect.height, 0f));
            }
            return position;
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
            var curSize = targetTransform.sizeDelta;
            var newSize = ClampSize(curSize + localResizeDelta * sizeSign);
            targetTransform.sizeDelta = newSize;
            var actualSizeDelta = newSize - curSize;
            var moveDelta = actualSizeDelta * sizeSign / 2;
            Move(targetTransform.TransformVector(moveDelta));
        }

        Vector2 HorizontalProjection(Vector2 vec) { return new Vector2(vec.x, 0f); }
        Vector2 VerticalProjection(Vector2 vec) { return new Vector2(0f, vec.y); }

        Vector2 ClampSize(Vector2 size) {
            return new Vector2(
                Mathf.Max(size.x, minSize.x),
                Mathf.Max(size.y, minSize.y));
        }

        public void OnEndDrag(PointerEventData eventData) {
            _isManipulatedNow = false;
            if (!eventData.hovered.Contains(gameObject))
                HighlightIcon(false);
        }
    }
}
