using System;
using System.Collections;
using UnityEngine;

namespace SoftMasking.Tests {
    public class TestChangeCanvas : MonoBehaviour {
        [Serializable] public struct Step {
            public Canvas canvas;
            public bool spawnCanvasOnItself;
        }

        public RectTransform panel;
        public Step[] steps;

        public IEnumerator Start() {
            while (true) {
                for (int i = 0; i < steps.Length; ++i) {
                    var step = steps[i];
                    var canvas = step.canvas;
                    panel.SetParent(canvas ? canvas.transform : null);
                    Canvas spawnedCanvas = null; 
                    if (step.spawnCanvasOnItself) {
                        spawnedCanvas = panel.gameObject.AddComponent<Canvas>();
                        if (!canvas) {
                            spawnedCanvas.renderMode = RenderMode.WorldSpace;
                            spawnedCanvas.worldCamera = Camera.main;
                        }
                    }
                    yield return new WaitForSeconds(1.5f);
                    if (spawnedCanvas) {
                        DestroyImmediate(spawnedCanvas);
                    }
                }
            }
        }
    }
}
