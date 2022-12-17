using UnityEngine;

namespace SoftMasking.Samples {
    [RequireComponent(typeof(Camera))]
    public class HorizontalFovSetter : MonoBehaviour {
        public float horizontalFov;

        Camera _camera;

        public void Awake() {
            _camera = GetComponent<Camera>();
        }

        public void Update() {
            _camera.fieldOfView = horizontalFov / _camera.aspect;
        }
    }
}
