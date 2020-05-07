#if UNITY_2019_2_OR_NEWER
using System.Collections;
#endif
using UnityEngine;
using UnityEngine.UI;

namespace SoftMasking.Tests {
    public class TestImagePixelsPerUnitMultiplier : MonoBehaviour {
        public AutomatedTest automatedTest;
        public Image image;

    #if UNITY_2019_2_OR_NEWER
        public IEnumerator Start() {
            yield break;
        }
    #endif
    }
}
