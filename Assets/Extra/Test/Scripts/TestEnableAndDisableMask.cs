using System.Collections;
using UnityEngine;

namespace SoftMasking.Tests {
    public class TestEnableAndDisableMask : MonoBehaviour {
        public SoftMask mask;

        public IEnumerator Start() {
            while (true) {
                mask.enabled = !mask.enabled;
                yield return new WaitForSeconds(1);
            }
        }
    }
}
