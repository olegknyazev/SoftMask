using System.Collections;
using UnityEngine;

namespace SoftMasking.Tests {
    public class TestCreateAndDestroyMask : MonoBehaviour {
        public GameObject panel;
        public AutomatedTest automatedTest;

        public IEnumerator Start() {
            SoftMask mask = null;
            int steps = 0;
            while (true) {
                if (mask) {
                    DestroyImmediate(mask);
                    mask = null;
                } else
                    mask = panel.AddComponent<SoftMask>();
                yield return automatedTest.Proceed(1f);
                if (++steps == 2)
                    yield return automatedTest.Finish();
            }
        }
    }
}
