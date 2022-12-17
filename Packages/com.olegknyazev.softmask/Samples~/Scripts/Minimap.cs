using UnityEngine;

namespace SoftMasking.Samples {
    public class Minimap : MonoBehaviour {
        public RectTransform map;
        public RectTransform marker;

        [Space]
        public float minZoom = 0.8f;
        public float maxZoom = 1.4f;
        public float zoomStep = 0.2f;

        float _zoom = 1.0f;

        public void LateUpdate() {
            map.anchoredPosition = -marker.anchoredPosition * _zoom;
        }

        public void ZoomIn() {
            _zoom = Clamp(_zoom + zoomStep);
            map.localScale = Vector3.one * _zoom;
        }

        public void ZoomOut() {
            _zoom = Clamp(_zoom - zoomStep);
            map.localScale = Vector3.one * _zoom;
        }

        float Clamp(float zoom) {
            return Mathf.Clamp(zoom, minZoom, maxZoom);
        }
    }
}
