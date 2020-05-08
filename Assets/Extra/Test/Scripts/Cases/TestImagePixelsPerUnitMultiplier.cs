#if UNITY_2019_2_OR_NEWER
using System.Collections;
#endif
using UnityEngine;
using UnityEngine.UI;

namespace SoftMasking.Tests {
    public class TestImagePixelsPerUnitMultiplier : MonoBehaviour {
        public AutomatedTest automatedTest;
        public Image image;
        public float[] pixelsPerUnitMultiplierSteps;

    #if UNITY_2019_2_OR_NEWER
        public IEnumerator Start() {
            foreach (var multiplier in pixelsPerUnitMultiplierSteps) {
                image.pixelsPerUnitMultiplier = multiplier;
                // There is a bug in 2019.3: changing pixelsPerUnitMultiplier from script
                // doesn't update anything.
                image.SetVerticesDirty();
                yield return automatedTest.Proceed(2f);
            }
            yield return automatedTest.Finish();
        }
    #endif
    }
}
