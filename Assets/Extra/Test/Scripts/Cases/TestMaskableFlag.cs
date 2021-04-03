using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SoftMasking.Tests {
    public class TestMaskableFlag : MonoBehaviour {
        public AutomatedTest automatedTest;
        public Image image;

        public IEnumerator Start() {
            while (true) {
                foreach (var maskable in new[] { false, true, false, true }) {
                    image.maskable = maskable;
                    yield return automatedTest.Proceed(1f);
                }
                yield return automatedTest.Finish();
            }
        }
    }
}
