using System.Collections;
using UnityEngine;

namespace SoftMasking.Tests {
    public class TestChangeCanvas : MonoBehaviour {
        public RectTransform panel;
        public Canvas[] canvases;

        public IEnumerator Start() {
            while (true) {
                for (int i = 0; i < canvases.Length; ++i) {
                    var canvas = canvases[i];
                    panel.SetParent(canvas ? canvas.transform : null);
                    yield return new WaitForSeconds(1.0f);
                }
            }
        }
    }
}
