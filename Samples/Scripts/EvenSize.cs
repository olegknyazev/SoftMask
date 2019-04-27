using UnityEngine;
using UnityEngine.EventSystems;

namespace SoftMasking.Samples {
    [RequireComponent(typeof(RectTransform))]
    public class EvenSize : UIBehaviour {
        public Vector2 sizeStep;

        RectTransform _rectTransform;
        Vector2 _originalSizeDelta;
        bool _change;

        protected override void Awake() {
            base.Awake();
            _rectTransform = GetComponent<RectTransform>();
            _originalSizeDelta = _rectTransform.sizeDelta;
        }

        protected override void OnRectTransformDimensionsChange() {
            base.OnRectTransformDimensionsChange();
            if (_rectTransform != null && !_change) {
                _change = true;
                var potentialSize = _rectTransform.rect.size + (_originalSizeDelta - _rectTransform.sizeDelta);
                var desiredSize = RoundSize(potentialSize, sizeStep);
                var delta = desiredSize - _rectTransform.rect.size;
                _rectTransform.sizeDelta += delta;
                _change = false;
            }
        }

        Vector2 RoundSize(Vector2 size, Vector2 sizeStep) {
            return new Vector2(RoundSize(size.x, sizeStep.x), RoundSize(size.y, sizeStep.y));
        }

        float RoundSize(float size, float sizeStep) {
            return sizeStep > 0f ? Mathf.Floor(size / sizeStep) * sizeStep : size;
        }
    }
}
