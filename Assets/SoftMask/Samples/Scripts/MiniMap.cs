using UnityEngine;

namespace SoftMasking.Samples {
    public class MiniMap : MonoBehaviour {
        public RectTransform map;
        public RectTransform marker;

        public void LateUpdate() {
            map.anchoredPosition = -marker.anchoredPosition;
        }
    }
}
