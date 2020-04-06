using System.Collections;
using UnityEngine;
using SoftMasking.Tests;

namespace SoftMasking.TextMeshPro.Tests {
    public class TestHierarchyChanging : MonoBehaviour {
        public Transform[] elements;
        public Transform[] roots;
        public AutomatedTest automatedTest;

        public IEnumerator Start() {
            var iteration = 0;
            while (true) {
                foreach (var root in roots)
                    foreach (var element in elements) {
                        element.SetParent(root, false);
                        yield return automatedTest.Proceed(0.5f);
                    }
                if (++iteration == 2)
                    yield return automatedTest.Finish();
            }
        }
    }
}
