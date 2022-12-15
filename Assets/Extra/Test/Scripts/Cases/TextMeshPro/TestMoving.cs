using System.Collections;
using UnityEngine;
using SoftMasking.Tests;

namespace SoftMasking.TextMeshPro.Tests {
    public class TestMoving : MonoBehaviour {
        public RectTransform[] elements;
        public AutomatedTest automatedTest;

        RectTransform _active;

        public IEnumerator Start() {
            while (true) {
                foreach (var element in elements) {
                    yield return Move(element);
                    yield return automatedTest.Proceed(1f);
                }
                yield return automatedTest.Finish();
            }
        }

        IEnumerator Move(RectTransform element) {
            _active = element;
            var offset = 100f;
            var steps = 8;
            for (var step = 0; step <= steps; ++step) {
                var angle = step * (Mathf.PI * 2f / steps);
                element.anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * offset;
                yield return automatedTest.Proceed(0.4f);
            }
            element.anchoredPosition = Vector2.zero;
            _active = null;
        }
    }
}
