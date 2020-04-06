using UnityEngine;
using UnityEngine.EventSystems;

namespace SoftMasking.Samples {
    [RequireComponent(typeof(RectTransform))]
    public class ShowOnHover : UIBehaviour, IPointerEnterHandler, IPointerExitHandler {
        public CanvasGroup targetGroup;

        public bool forcedVisible {
            get { return _forcedVisible; }
            set {
                if (_forcedVisible != value) {
                    _forcedVisible = value;
                    UpdateVisibility();
                }
            }
        }

        bool _forcedVisible;
        bool _isPointerOver;

        protected override void Start() {
            base.Start();
            UpdateVisibility();
        }

        void UpdateVisibility() {
            SetVisible(ShouldBeVisible());
        }

        bool ShouldBeVisible() {
            return _forcedVisible || _isPointerOver;
        }

        void SetVisible(bool visible) {
            if (targetGroup)
                targetGroup.alpha = visible ? 1f : 0f;
        }

        public void OnPointerEnter(PointerEventData eventData) {
            _isPointerOver = true;
            UpdateVisibility();
        }

        public void OnPointerExit(PointerEventData eventData) {
            _isPointerOver = false;
            UpdateVisibility();
        }
    }
}
