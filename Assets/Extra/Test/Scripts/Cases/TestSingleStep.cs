using System.Collections;
using UnityEngine;

namespace SoftMasking.Tests {
    public class TestSingleStep : MonoBehaviour {
        public AutomatedTest automatedTest;

        public IEnumerator Start() {
            yield return null;
            yield return null;
            yield return automatedTest.Proceed();
            yield return automatedTest.Finish();
        }
    }
}
