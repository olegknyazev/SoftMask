using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SoftMasking.Tests {
    public class TestNestMasks : MonoBehaviour {
        [Serializable] public struct Stack {
            public RectTransform[] elements;
        }

        public RectTransform root;
        public Stack[] steps;
        public float delay = 5.0f;

        public IEnumerator Start() {
            var i = 0;
            while (true) {
                var step = steps[i];
                var parent = root;
                for (int j = 0; j < step.elements.Length; ++j) {
                    var element = step.elements[j];
                    element.SetParent(parent, false);
                    if (j > 0) element.anchoredPosition = RandomPosition();
                    parent = element;
                }
                i = (i + 1) % steps.Length;
                yield return new WaitForSeconds(delay);
            }
        }

        Vector2 RandomPosition() {
            var rotation = Quaternion.AngleAxis(Random.Range(0, 360.0f), Vector3.forward);
            return rotation * Vector3.right * Random.Range(30.0f, 60.0f);
        }
    }
}
