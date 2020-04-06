using System.Collections;
using UnityEngine;

namespace SoftMasking.Tests {
    public class TestSwitchProjectionMode : MonoBehaviour {
        public Camera testCamera;
        public Animator animator;
        public AutomatedTest automatedTest;

        public IEnumerator Start() {
            while (true) {
                testCamera.orthographic = false;
                yield return automatedTest.ProceedAnimation(animator, 0f);
                testCamera.orthographic = true;
                yield return automatedTest.ProceedAnimation(animator, 0.33f);
                testCamera.orthographic = false;
                yield return automatedTest.ProceedAnimation(animator, 0.66f);
                testCamera.orthographic = true;
                yield return automatedTest.ProceedAnimation(animator, 1f);
                yield return automatedTest.Finish();
            }
        }
    }
}
