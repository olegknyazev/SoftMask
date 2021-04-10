using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SoftMasking.Tests {
    public class TestRectMaskSoftness : MonoBehaviour {
        public RectMask2D rectMask;
        public AutomatedTest automatedTest;

        public IEnumerator Start() {
        #if UNITY_2020_1_OR_NEWER
            while (true) {
                foreach (var softness in new[] { 0, 5, 25 }) {
                    rectMask.softness = Vector2Int.one * softness;
                    yield return automatedTest.Proceed(1f);
                }
                yield return automatedTest.Finish();
            }
        #else
            Debug.LogAssertion("This test should not be run in Unity prior to 2020");
            yield break;
        #endif
        }
    }
}
