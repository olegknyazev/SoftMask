using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SoftMasking.Tests {
    public class TestChangeMaskImage : MonoBehaviour {
        [Serializable]
        public struct State {
            public Sprite sprite;
            public Image.Type imageType;
        }

        public Image image;
        public float delay = 2.0f;
        public State[] states;

        public AutomatedTest automatedTest;
        
        public IEnumerator Start() {
            var idx = 0;
            while (true) {
                var state = states[idx];
                image.sprite = state.sprite;
                image.type = state.imageType;
                idx = (idx + 1) % states.Length;
                yield return automatedTest.Proceed(delay);
                if (idx >= states.Length - 1)
                    yield return automatedTest.Finish();
            }
        }
    }
}
