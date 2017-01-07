using System.Collections;
using UnityEngine;

namespace SoftMasking.Tests {
    public class TestCreateAndDestroyMask : MonoBehaviour {
        public GameObject panel;
        public Shader shader;

        public IEnumerator Start() {
            SoftMask mask = null;
            while (true) {
                if (mask) {
                    DestroyImmediate(mask);
                    mask = null;
                } else {
                    mask = panel.AddComponent<SoftMask>();
                    mask.defaultShader = shader;
                }
                yield return new WaitForSeconds(1);
            }
        }
    }
}
