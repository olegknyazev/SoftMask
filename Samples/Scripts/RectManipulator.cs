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
        Vector2 _startAnchoredPosition;
        Vector2 _startSizeDelta;
        float _startRotation;
        
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
            RememberStartTransform();
        }

        void RememberStartTransform() {
            if (targetTransform) {
                _startAnchoredPosition = targetTransform.anchoredPosition;
                _startSizeDelta = targetTransform.sizeDelta;
                _startRotation = targetTransform.localRotation.eulerAngles.z;
            }
        }

        public void OnDrag(PointerEventData eventData) {
            if (targetTransform == null || parentTransform == null || !_isManipulatedNow)
                return;
            var startPoint = ToParentSpace(eventData.pressPosition, eventData.pressEventCamera);
            var curPoint = ToParentSpace(eventData.position, eventData.pressEventCamera);
            DoRotate(startPoint, curPoint);
            var parentSpaceMovement = curPoint - startPoint;
            DoMove(parentSpaceMovement);
            DoResize(parentSpaceMovement);
        }

        Vector2 ToParentSpace(Vector2 position, Camera eventCamera) {
            Vector2 localPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentTransform, position, eventCamera, out localPosition);
            return localPosition;
        }
        
        RectTransform parentTransform {
            get { return targetTransform.parent as RectTransform; }
        }

        void DoMove(Vector2 parentSpaceMovement) {
            if (Is(ManipulationType.Move))
                MoveTo(_startAnchoredPosition + parentSpaceMovement);
        }

        bool Is(ManipulationType expected) {
            return (manipulation & expected) == expected;
        }

        void MoveTo(Vector2 desiredAnchoredPosition) {
            targetTransform.anchoredPosition = ClampPosition(desiredAnchoredPosition);
        }

        Vector2 ClampPosition(Vector2 position) {
            var parentSize = parentTransform.rect.size;
            return new Vector2(
                Mathf.Clamp(position.x, 0f, parentSize.x),
                Mathf.Clamp(position.y, -parentSize.y, 0f));
        }

        void DoRotate(Vector2 startParentPoint, Vector2 targetParentPoint) {
            if (Is(ManipulationType.Rotate)) {
                var startLever = startParentPoint - (Vector2)targetTransform.localPosition;
                var targetLever = targetParentPoint - (Vector2)targetTransform.localPosition;
                var additionalRotation = DeltaRotation(startLever, targetLever);
                targetTransform.localRotation = Quaternion.AngleAxis(_startRotation + additionalRotation, Vector3.forward);
            }
        }

        float DeltaRotation(Vector2 startLever, Vector2 endLever) {
            var startAngle = Mathf.Atan2(startLever.y, startLever.x) * Mathf.Rad2Deg;
            var endAngle = Mathf.Atan2(endLever.y, endLever.x) * Mathf.Rad2Deg;
            return Mathf.DeltaAngle(startAngle, endAngle);
        }

        void DoResize(Vector2 parentSpaceMovement) {
            var localSpaceMovement = Quaternion.Inverse(targetTransform.rotation) * parentSpaceMovement;
            var projectedOffset = ProjectResizeOffset(localSpaceMovement);
            if (projectedOffset.sqrMagnitude > 0f)
                SetSizeDirected(projectedOffset, ResizeSign());
        }

        Vector2 ProjectResizeOffset(Vector2 localOffset) {
            var isHorizontal = Is(ManipulationType.ResizeLeft) || Is(ManipulationType.ResizeRight);
            var isVertical = Is(ManipulationType.ResizeUp) || Is(ManipulationType.ResizeDown);
            return new Vector2(
                isHorizontal ? localOffset.x : 0f,
                isVertical ? localOffset.y : 0f);
        }

        Vector2 ResizeSign() {
            return new Vector2(
                Is(ManipulationType.ResizeLeft) ? -1f : 1f,
                Is(ManipulationType.ResizeDown) ? -1f : 1f);
        }
        
        void SetSizeDirected(Vector2 localOffset, Vector2 sizeSign) {
            var newSize = ClampSize(_startSizeDelta + Vector2.Scale(localOffset, sizeSign));
            targetTransform.sizeDelta = newSize;
            var actualSizeOffset = newSize - _startSizeDelta;
            var localMoveOffset = Vector2.Scale(actualSizeOffset / 2, sizeSign);
            MoveTo(_startAnchoredPosition + (Vector2)targetTransform.TransformVector(localMoveOffset));
        }

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
