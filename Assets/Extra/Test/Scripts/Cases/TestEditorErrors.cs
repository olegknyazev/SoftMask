using System;
using System.Collections;
using UnityEngine;

namespace SoftMasking.Tests {
    public class TestEditorErrors : MonoBehaviour {
        [Serializable] public class Case {
            public GameObject objectToActivate;

            public IEnumerable Execute(AutomatedTest test) {
                yield return test.Proceed(0.1f);
                objectToActivate.SetActive(true);
                yield return test.Proceed(0.1f);
            }
        }

        public AutomatedTest automatedTest;
        public Case[] cases;

        public IEnumerator Start() {
            foreach (var c in cases)
                foreach (var step in c.Execute(automatedTest))
                    yield return step;
            yield return automatedTest.Finish();
        }
    }
}
