using UnityEngine;

namespace SoftMasking.Samples {
    public class HorizontalFovSetter : MonoBehaviour {
        public new Camera camera;
        public float horizontalFov;

        public void Update() {
            camera.fieldOfView = horizontalFov / camera.aspect;
        }
    }
}
