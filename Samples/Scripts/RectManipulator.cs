using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoftMasking.Samples {
    [RequireComponent(typeof(RectTransform))]
    public class RectManipulator : UIBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler {
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
        public Graphic[] displayGraphics;
        
        public void OnPointerEnter(PointerEventData eventData) {
            DisplayHighlight(true);
        }

        public void OnPointerExit(PointerEventData eventData) {
            if (_activeManipulation != manipulation)
                DisplayHighlight(false);
        }

        void DisplayHighlight(bool highlight, bool instant = false) {
            var targetAlpha = highlight ? 1f : 0f;
            var duration = instant ? 0f : 0.2f;
            foreach (var graphic in displayGraphics)
                graphic.CrossFadeAlpha(targetAlpha, duration, true);
        }

        protected override void Start() {
            base.Start();
            DisplayHighlight(false, instant: true);
        }

        public void OnBeginDrag(PointerEventData eventData) {
            _activeManipulation = manipulation;
        }
        
        public void OnDrag(PointerEventData eventData) {
            if (targetTransform == null)
                return;
            Func<ManipulationType, bool> Is = ActiveManipulationIs;
            var prevLocalPoint = ToLocal(eventData.position - eventData.delta, eventData.pressEventCamera);
            var curLocalPoint = ToLocal(eventData.position, eventData.pressEventCamera);
            if (Is(ManipulationType.Move))
                targetTransform.anchoredPosition += eventData.delta;
            if (Is(ManipulationType.Rotate))
                targetTransform.localRotation *= Quaternion.AngleAxis(DeltaRotation(prevLocalPoint, curLocalPoint), Vector3.forward);
            var localDelta = curLocalPoint - prevLocalPoint;
            if (Is(ManipulationType.ResizeLeft))
                ResizeDirected(HorizontalProjection(localDelta), -1f);
            if (Is(ManipulationType.ResizeUp))
                ResizeDirected(VerticalProjection(localDelta), +1f);
            if (Is(ManipulationType.ResizeRight))
                ResizeDirected(HorizontalProjection(localDelta), +1f);
            if (Is(ManipulationType.ResizeDown))
                ResizeDirected(VerticalProjection(localDelta), -1f);
        }

        bool ActiveManipulationIs(ManipulationType manipulation) {
            return (_activeManipulation & manipulation) == manipulation;
        }

        Vector2 ToLocal(Vector2 position, Camera eventCamera) {
            Vector2 localPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                targetTransform, position, eventCamera, out localPosition);
            return localPosition;
        }

        float DeltaRotation(Vector2 prevLocalPoint, Vector2 curLocalPoint) {
            var prevAngle = Mathf.Atan2(prevLocalPoint.y, prevLocalPoint.x) * Mathf.Rad2Deg;
            var curAngle = Mathf.Atan2(curLocalPoint.y, curLocalPoint.x) * Mathf.Rad2Deg;
            return Mathf.DeltaAngle(prevAngle, curAngle);
        }

        void ResizeDirected(Vector2 localResizeDelta, float sizeSign) {
            targetTransform.sizeDelta += localResizeDelta * sizeSign;
            targetTransform.position += targetTransform.TransformVector(localResizeDelta) / 2;
        }

        Vector2 HorizontalProjection(Vector2 vec) { return new Vector2(vec.x, 0f); }
        Vector2 VerticalProjection(Vector2 vec) { return new Vector2(0f, vec.y); }

        public void OnEndDrag(PointerEventData eventData) {
            _activeManipulation = ManipulationType.None;
            if (!eventData.hovered.Contains(gameObject))
                DisplayHighlight(false);
        }
    }
}
