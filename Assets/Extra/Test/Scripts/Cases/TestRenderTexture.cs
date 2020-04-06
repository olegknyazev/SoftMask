using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SoftMasking.Tests {
    public class TestRenderTexture : MonoBehaviour {
        [Serializable] public class Step {
            public Vector2 offset;
            public float rotation;
        }

        public RawImage rawImage;
        public SoftMask mask;
        public RectTransform image;
        public Camera maskRenderingCamera;
        public AutomatedTest automatedTest;
        public Step[] steps;

        RenderTexture _renderTexture;

        public IEnumerator Start() {
            _renderTexture = new RenderTexture(100, 100, 24);
            maskRenderingCamera.targetTexture = _renderTexture;
            mask.renderTexture = _renderTexture;
            rawImage.texture = _renderTexture;
            rawImage.enabled = false;
            foreach (var step in steps) {
                image.anchoredPosition = step.offset;
                image.localRotation = Quaternion.AngleAxis(step.rotation, Vector3.forward);

                mask.source = SoftMask.MaskSource.Texture;
                yield return automatedTest.Proceed(0.5f);

                mask.source = SoftMask.MaskSource.Graphic;
                yield return automatedTest.Proceed(0.5f);
            }
            yield return automatedTest.Finish();
        }

        public void OnDestroy() {
            if (maskRenderingCamera)
                maskRenderingCamera.targetTexture = null;
            DestroyImmediate(_renderTexture);
        }
    }
}
