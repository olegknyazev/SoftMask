using System.Collections;
using UnityEngine;

namespace SoftMasking.Tests {
    public class TestChangeSeparateMask : MonoBehaviour {
        public SoftMask mask;
        public RectTransform[] viewports;

        public IEnumerator Start() {
            var idx = 0;
            while (true) {
                mask.separateMask = viewports[idx];
                idx = (idx + 1) % viewports.Length;
                yield return new WaitForSeconds(1.5f);
            }
        }
    }
}
