using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoftMasking.Tests {
    public class TestEditorErrors : MonoBehaviour {
        public AutomatedTest automatedTest;
        public GameObject objectToActivate;

        public IEnumerator Start() {
            yield return null;
            yield return automatedTest.Proceed();
            objectToActivate.SetActive(true);
            yield return null;
            yield return automatedTest.Proceed();
            yield return automatedTest.Finish();
        }
    }
}
