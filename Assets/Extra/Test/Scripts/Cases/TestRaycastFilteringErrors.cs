using System;
using System.Collections;
using UnityEngine;

namespace SoftMasking.Tests {
    public class TestRaycastFilteringErrors : MonoBehaviour {
        [Serializable] public struct Case {
            public SoftMask mask;
        }
        public AutomatedTest automatedTest;
        public Case[] cases;

        public IEnumerator Start() {
            foreach (var c in cases) {
                for (int i = 0; i < 2; ++i) { // one message for many interactions
                    var locationValid = c.mask.IsRaycastLocationValid(new Vector2(10, 10), null);
                    if (!locationValid)
                        yield return automatedTest.Fail("Expected that raycast location will be valid in case of error");
                    yield return automatedTest.Proceed();
                }
            }
            yield return automatedTest.Finish();
        }
    }
}
