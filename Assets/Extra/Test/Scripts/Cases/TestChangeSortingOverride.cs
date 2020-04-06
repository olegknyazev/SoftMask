using System.Collections;
using UnityEngine;

namespace SoftMasking.Tests {
    public class TestChangeSortingOverride : MonoBehaviour {
        public Canvas canvas;
        public AutomatedTest automatedTest;

        public IEnumerator Start() {
            var iteration = 0;
            while (true) {
                ++iteration;
                foreach (var enabled in new [] { true, false }) {
                    canvas.overrideSorting = enabled;
                    yield return automatedTest.Proceed(2f);
                }
                if (iteration >= 2)
                    yield return automatedTest.Finish();
            }
        }
    }
}
