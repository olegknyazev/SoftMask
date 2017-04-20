using System.Collections;
using UnityEngine;

namespace SoftMasking.Tests {
    public class TestSwitchProjectionMode : MonoBehaviour {
        public Camera testCamera;

        public IEnumerator Start() {
            while (true) {
                yield return new WaitForSeconds(2);
                testCamera.orthographic = !testCamera.orthographic;
            }
        }
    }
}
