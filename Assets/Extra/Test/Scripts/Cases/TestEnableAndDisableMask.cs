using System.Collections;
using UnityEngine;

namespace SoftMasking.Tests {
    public class TestEnableAndDisableMask : MonoBehaviour {
        public SoftMask mask;
        public AutomatedTest automatedTest;

        public IEnumerator Start() {
            var step = 0;
            while (true) {
                ++step;
                foreach (var enabled in new [] { false, true }) {
                    mask.enabled = enabled;
                    yield return automatedTest.Proceed(1);
                }
                yield return (step == 2) ? automatedTest.Finish() : null;
            }
        }
    }
}
