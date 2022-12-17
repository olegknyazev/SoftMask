using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace SoftMasking.Tests {
    public class TestImagePixelsPerUnitMultiplier : MonoBehaviour {
        public AutomatedTest automatedTest;
        public Image image;
        public SoftMask mask;
        public float[] pixelsPerUnitMultiplierSteps;

        public IEnumerator Start() {
            Assert.IsTrue(image || mask);
            foreach (var multiplier in pixelsPerUnitMultiplierSteps) {
                if (image) {
                    image.pixelsPerUnitMultiplier = multiplier;
                    // There is a bug in 2019.3: changing pixelsPerUnitMultiplier from script
                    // doesn't update anything.
                    image.SetVerticesDirty();
                }
                if (mask)
                    mask.spritePixelsPerUnitMultiplier = multiplier;
                yield return automatedTest.Proceed(2f);
            }
            yield return automatedTest.Finish();
        }
    }
}
