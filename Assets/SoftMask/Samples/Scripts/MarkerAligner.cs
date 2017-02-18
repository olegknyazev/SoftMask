using UnityEngine;

namespace SoftMasking.Samples {
    public class MarkerAligner : MonoBehaviour {
        public RectTransform map;
        public RectTransform marker;

        public void LateUpdate() {
            map.anchoredPosition = -marker.anchoredPosition;
        }
    }
}
