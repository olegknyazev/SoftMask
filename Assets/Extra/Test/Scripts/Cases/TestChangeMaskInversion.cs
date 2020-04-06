using System.Collections;
using UnityEngine;

namespace SoftMasking.Tests {
    public class TestChangeMaskInversion : MonoBehaviour {
        public SoftMask mask;
        public float delay = 1.0f;

        public AutomatedTest automatedTest;
        
        public IEnumerator Start() {
            var idx = 0;
            while (true) {
                foreach (var invertInsides in new [] { false, true })
                    foreach (var invertOutsides in new [] { false, true }) {
                        mask.invertMask = invertInsides;
                        mask.invertOutsides = invertOutsides;
                        yield return automatedTest.Proceed(delay);
                    }
                if (++idx == 2)
                    yield return automatedTest.Finish();
            }
        }
    }
}
