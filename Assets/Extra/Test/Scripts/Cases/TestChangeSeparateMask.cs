using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SoftMasking.Tests {
    public class TestChangeSeparateMask : MonoBehaviour {
        public SoftMask mask;
        public RectTransform[] viewports;
        public Text currentViewportLabel;
        public float stepDuration = 2;

        public AutomatedTest automatedTest;

        public IEnumerator Start() {
            while (true) {
                foreach (var viewport in viewports) {
                    currentViewportLabel.text = "Viewport: " + (viewport ? viewport.name : "null");
                    mask.separateMask = viewport;
                    yield return automatedTest.Proceed(stepDuration);
                }
                yield return automatedTest.Finish();
            }
        }
    }
}
