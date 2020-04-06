using System;
using System.Collections;
using UnityEngine;

namespace SoftMasking.Tests {
    public class TestChangeCanvas : MonoBehaviour {
        [Serializable] public struct Step {
            public Canvas nestIntoCanvas;
            public bool spawnOwnCanvas;
        }

        public RectTransform panel;
        public Step[] steps;

        public AutomatedTest automatedTest;

        public IEnumerator Start() {
            while (true) {
                foreach (var step in steps) {
                    var canvas = step.nestIntoCanvas;
                    panel.SetParent(canvas ? canvas.transform : null);
                    Canvas spawnedCanvas = null; 
                    if (step.spawnOwnCanvas) {
                        spawnedCanvas = panel.gameObject.AddComponent<Canvas>();
                        if (!canvas) {
                            spawnedCanvas.renderMode = RenderMode.WorldSpace;
                            spawnedCanvas.worldCamera = Camera.main;
                        }
                    }
                    yield return automatedTest.Proceed(1.5f);
                    if (spawnedCanvas) {
                        DestroyImmediate(spawnedCanvas);
                    }
                }
                yield return automatedTest.Finish();
            }
        }
    }
}
