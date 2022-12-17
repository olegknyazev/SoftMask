using System.Collections;
using UnityEngine;

namespace SoftMasking.Tests {
    public class TestTiledMaskInScaledCanvas : MonoBehaviour {
        public Canvas canvas;
        public AutomatedTest automatedTest;

        public IEnumerator Start() {
            var pixelsPerUnitToTest = new[] { 50, 100, 200 };
            foreach (var ppu in pixelsPerUnitToTest) {
                canvas.referencePixelsPerUnit = ppu;
                yield return automatedTest.Proceed(0.25f);
            }
            yield return automatedTest.Finish();
        }
    }
}
