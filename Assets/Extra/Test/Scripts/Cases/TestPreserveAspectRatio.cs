using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SoftMasking.Tests {
    public class TestPreserveAspectRatio : MonoBehaviour {
        public Image image;
        public AutomatedTest automatedTest;

        public IEnumerator Start() {
            while (true) {
                foreach (var preserve in new[] { false, true, false, true }) {
                    image.preserveAspect = preserve;
                    yield return automatedTest.Proceed(0.5f);
                }
                yield return automatedTest.Finish();
            }
        }
    }
}
