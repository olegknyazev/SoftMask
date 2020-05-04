using System;
using System.Collections;
using UnityEngine;

namespace SoftMasking.Tests {
    public class TestEditorErrors : MonoBehaviour {
        [Serializable] public struct Case {
            public GameObject objectToActivate;
        }

        public AutomatedTest automatedTest;
        public Case[] cases;

        public IEnumerator Start() {
            foreach (var c in cases) {
                yield return automatedTest.Proceed(0.1f);
                c.objectToActivate.SetActive(true);
                yield return automatedTest.Proceed(0.1f);
            }
            yield return automatedTest.Finish();
        }
    }
}
