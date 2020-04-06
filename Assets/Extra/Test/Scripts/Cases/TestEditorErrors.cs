using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoftMasking.Tests {
    public class TestEditorErrors : MonoBehaviour {
        public AutomatedTest automatedTest;
        public GameObject objectToActivate;
        public List<ExpectedLogRecord> expectedLog = new List<ExpectedLogRecord>();

        public IEnumerator Start() {
            foreach (var record in expectedLog)
                automatedTest.ExpectLog(record);
            yield return null;
            objectToActivate.SetActive(true);
            yield return null;
            yield return automatedTest.Proceed();
            yield return automatedTest.Finish();
        }
    }
}
