using System.Collections;
using UnityEngine;
using SoftMasking.Tests;

namespace SoftMasking.TextMeshPro.Tests {
    public class TestEnablingAndDisabling : MonoBehaviour {
        public SoftMask softMask;
        public GameObject element;
        public AutomatedTest automatedTest;

        public IEnumerator Start() {
            var iteration = 0;
            while (true) {
                foreach (var elementActive in new [] { true, false })
                    foreach (var maskEnabled in new [] { true, false }) {
                        element.SetActive(elementActive);
                        softMask.enabled = maskEnabled;
                        yield return automatedTest.Proceed(1f);
                    }
                if (++iteration == 2)
                    yield return automatedTest.Finish();
            }
        }

        public void OnGUI() {
            GUILayout.Label("MASK: " + softMask.enabled);
            GUILayout.Label("TEXT: " + element.activeSelf);
        }
    }
}
