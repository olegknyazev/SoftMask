using UnityEngine;

namespace SoftMasking.Samples {
    public class AlignMiniMap : MonoBehaviour {
        public RectTransform map;
        public RectTransform marker;

        public void LateUpdate() {
            map.anchoredPosition = -marker.anchoredPosition;
        }
    }
}
