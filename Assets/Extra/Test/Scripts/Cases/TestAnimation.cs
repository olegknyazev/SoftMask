using System.Collections;
using UnityEngine;

namespace SoftMasking.Tests {
    public class TestAnimation : MonoBehaviour {
        public Animator animator;
        public float[] validationSteps;

        public AutomatedTest automatedTest;

        public IEnumerator Start() {
            foreach (var step in validationSteps)
                yield return automatedTest.ProceedAnimation(animator, step);
            yield return automatedTest.Finish();
        }
    }
}
