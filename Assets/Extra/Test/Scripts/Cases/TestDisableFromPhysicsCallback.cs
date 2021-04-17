using System.Collections;
using UnityEngine;

namespace SoftMasking.Tests {
    // This test reproduces a long-existing Soft Mask issue: if SoftMaskable go out
    // of SoftMask in physics callback, it causes an error because DestroyImmediate
    // should not be called from physics callbacks.
    // See SoftMaskable.FindMaskOrDie.
    public class TestDisableFromPhysicsCallback : MonoBehaviour {
        public Rigidbody rigidbodyToEnable;
        public Transform childToUnparent;

        public AutomatedTest automatedTest;

        public IEnumerator Start() {
            yield return automatedTest.Proceed();
            rigidbodyToEnable.useGravity = true;
        }

        void OnCollisionEnter(Collision other) {
            childToUnparent.SetParent(childToUnparent.parent.parent);
            StartCoroutine(ProceedAndFinish());
        }

        IEnumerator ProceedAndFinish() {
            yield return automatedTest.Proceed();
            yield return automatedTest.Finish();
        }
    }
}
