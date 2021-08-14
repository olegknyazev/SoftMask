using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace SoftMasking.Tests {
    public class TestMoveMask : MonoBehaviour {
        public RectTransform mask;
        public RectTransform[] immovables;
        public AutomatedTest automatedTest;

        public IEnumerator Start() {
            // TODO looks like it takes two frames to change resolution. See a comment in AutomatedTest.CaptureStep()
            yield return null;
            yield return null;
            var originalImmovablePositions = immovables.Select(x => x.position).ToArray();
            Action<float> updatePositions = (angle) => {
                mask.anchoredPosition = new Vector2(Mathf.Cos(angle) * 100.0f, Mathf.Sin(angle) * 100.0f);
                for (int i = 0; i < immovables.Length; ++i)
                    immovables[i].position = originalImmovablePositions[i];
            };
            while (true) {
                var angle = 0.0f;
                var stopCount = 8;
                var sectorWidth = Mathf.PI * 2f / stopCount;
                for (int stop = 0; stop <= stopCount; ++stop) {
                    var furtherBound = stop * sectorWidth;
                    if (automatedTest.speedUp) {
                        angle = furtherBound;
                        updatePositions(angle);
                    } else
                        do {
                            angle = Mathf.Clamp(angle + Time.deltaTime, 0f, furtherBound);
                            updatePositions(angle);
                            yield return null;
                        } while (angle < furtherBound);
                    yield return automatedTest.Proceed();
                }
                yield return automatedTest.Finish();
            }
        }
    }
}
