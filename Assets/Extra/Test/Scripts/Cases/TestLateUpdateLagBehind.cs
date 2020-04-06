using System;
using System.Collections;
using UnityEngine;

namespace SoftMasking.Tests {
    public class TestLateUpdateLagBehind : MonoBehaviour {
        public RectTransform mask;
        public AutomatedTest automatedTest;

        Action lateUpdateAction;

        public IEnumerator Start() {
            Time.captureFramerate = 60;
            while (true) {
                foreach (var offset in new [] { new Vector2(-20, 0), new Vector2(0, 0), new Vector2(20, 0) }) {
                    DoInLateUpdate(() => { mask.anchoredPosition = offset; });
                    yield return automatedTest.Proceed(0.5f);
                }
                yield return automatedTest.Finish();
            }
        }

        public void LateUpdate() {
            if (lateUpdateAction != null) {
                lateUpdateAction();
                lateUpdateAction = null;
            }
        }

        public void OnDestroy() {
            Time.captureFramerate = 0;
        }
        
        void DoInLateUpdate(Action action) {
            lateUpdateAction = action;
        }
    }
}
