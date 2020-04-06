using System;
using System.Collections;
using UnityEngine;

namespace SoftMasking.Tests {
    public class TestResizeMask : MonoBehaviour {        
        [Serializable]
        public struct Anchoring {
            public Vector2 min;
            public Vector2 max;
        }

        public RectTransform mask;
        public Anchoring[] anchorings;
        public AutomatedTest automatedTest;

        public IEnumerator Start() {
            while (true) {
                foreach (var anch in anchorings) {
                    mask.anchorMin = anch.min;
                    mask.anchorMax = anch.max;
                    for (int i = 0; i < 8; ++i) {
                        var angle = i * Mathf.PI / 4f;    
                        mask.sizeDelta =
                            Vector2.one * 300f + new Vector2(Mathf.Cos(angle),  Mathf.Sin(angle)) * 100f;
                        yield return automatedTest.Proceed(0.2f);
                    }
                }
                yield return automatedTest.Finish();
            }
        }
    }
}
