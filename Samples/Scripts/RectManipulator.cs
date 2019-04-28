using System;
using System.Collections.Generic;
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
        public float resizeGripSize;

        public void OnBeginDrag(PointerEventData eventData) {
            _activeManipulation = ManipulationByPressPosition(ToLocal(eventData.pressPosition, eventData.pressEventCamera));
        }
        
        ManipulationType ManipulationByPressPosition(Vector2 pos) {
            var rect = targetTransform.rect;
            var xSegment = DetermineSegment(pos.x, rect.xMin, rect.xMax, resizeGripSize);
            var ySegment = DetermineSegment(pos.y, rect.yMin, rect.yMax, resizeGripSize);
            ManipulationType result;
            return manipulationBySegment.TryGetValue(new SegmentPair(xSegment, ySegment), out result)
                ? result
                : ManipulationType.None;
        }

        Segment DetermineSegment(float v, float min, float max, float border) {
            if (v >= min && v < min + border)
                return Segment.Lower;
            else if (v >= min + border && v < max - border)
                return Segment.Center;
            else if (v >= max - border && v < max)
                return Segment.Upper;
            else
                return Segment.None;
        }
        
        enum Segment { None, Lower, Center, Upper }

        struct SegmentPair {
            public Segment x;
            public Segment y;
            public SegmentPair(Segment x_, Segment y_) {
                x = x_;
                y = y_;
            }
        }

        static readonly Dictionary<SegmentPair, ManipulationType> manipulationBySegment =
            new Dictionary<SegmentPair, ManipulationType>() {
                { new SegmentPair(Segment.Lower, Segment.Upper), ManipulationType.ResizeUpLeft },
                { new SegmentPair(Segment.Center, Segment.Upper), ManipulationType.ResizeUp},
                { new SegmentPair(Segment.Upper, Segment.Upper), ManipulationType.ResizeUpRight},

                { new SegmentPair(Segment.Lower, Segment.Center), ManipulationType.ResizeLeft },
                { new SegmentPair(Segment.Center, Segment.Center), ManipulationType.Move},
                { new SegmentPair(Segment.Upper, Segment.Center), ManipulationType.ResizeRight},

                { new SegmentPair(Segment.Lower, Segment.Lower), ManipulationType.ResizeDownLeft },
                { new SegmentPair(Segment.Center, Segment.Lower), ManipulationType.ResizeDown},
                { new SegmentPair(Segment.Upper, Segment.Lower), ManipulationType.ResizeDownRight },
            };

        public void OnDrag(PointerEventData eventData) {
            if (targetTransform == null)
                return;
            var prevLocalPoint = ToLocal(eventData.position - eventData.delta, eventData.pressEventCamera);
            var curLocalPoint = ToLocal(eventData.position, eventData.pressEventCamera);
            DoMove(eventData);
            DoRotate(prevLocalPoint, curLocalPoint);
            DoResize(curLocalPoint - prevLocalPoint);
        }

        Vector2 ToLocal(Vector2 position, Camera eventCamera) {
            Vector2 localPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                targetTransform, position, eventCamera, out localPosition);
            return localPosition;
        }

        void DoMove(PointerEventData eventData) {
            if (ActiveManipulationIs(ManipulationType.Move))
                targetTransform.anchoredPosition += eventData.delta;
        }

        void DoRotate(Vector2 prevLocalPoint, Vector2 curLocalPoint) {
            if (ActiveManipulationIs(ManipulationType.Rotate))
                targetTransform.localRotation *= Quaternion.AngleAxis(DeltaRotation(prevLocalPoint, curLocalPoint), Vector3.forward);
        }

        void DoResize(Vector2 resizeDelta) {
            var hDelta = HorizontalProjection(resizeDelta);
            if (ActiveManipulationIs(ManipulationType.ResizeLeft))
                ResizeDirected(hDelta, -1f);
            else if (ActiveManipulationIs(ManipulationType.ResizeRight))
                ResizeDirected(hDelta, +1f);
            var vDelta = VerticalProjection(resizeDelta);
            if (ActiveManipulationIs(ManipulationType.ResizeUp))
                ResizeDirected(vDelta, +1f);
            else if (ActiveManipulationIs(ManipulationType.ResizeDown))
                ResizeDirected(vDelta, -1f);
        }

        bool ActiveManipulationIs(ManipulationType manipulation) {
            return (_activeManipulation & manipulation) == manipulation;
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
        }
    }
}
