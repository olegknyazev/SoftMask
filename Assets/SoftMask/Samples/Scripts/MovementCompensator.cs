using UnityEngine;

namespace SoftMasking.Samples {
    public class MovementCompensator : MonoBehaviour {
        public RectTransform content;
        public RectTransform viewport;

        public void LateUpdate() {
            content.anchoredPosition = -viewport.anchoredPosition;
        }
    }
}
