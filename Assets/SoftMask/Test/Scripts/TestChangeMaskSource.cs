using System;
using System.Collections;
using UnityEngine;

namespace SoftMasking.Tests {
    public class TestChangeMaskSource : MonoBehaviour {
        [Serializable]
        public struct State {
            public SoftMask.MaskSource source;
            public Sprite customSprite;
            public SoftMask.BorderMode spriteBorderMode;
            public Texture2D customTexture;
            public Rect textureRect;
        }
        
        public SoftMask mask;
        public float delay = 2.0f;
        public State[] states;
        
        public IEnumerator Start() {
            var idx = 0;
            while (true) {
                var state = states[idx];
                mask.source = state.source;
                if (state.source == SoftMask.MaskSource.Sprite) {
                    mask.sprite = state.customSprite;
                    mask.spriteBorderMode = state.spriteBorderMode;
                } else if (state.source == SoftMask.MaskSource.Texture) {
                    mask.texture = state.customTexture;
                    mask.textureUVRect = state.textureRect;
                }
                idx = (idx + 1) % states.Length;
                yield return new WaitForSeconds(delay);
            }
        }
    }
}
