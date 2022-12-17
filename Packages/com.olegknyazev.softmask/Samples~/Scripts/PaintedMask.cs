using UnityEngine;
using UnityEngine.EventSystems;

namespace SoftMasking.Samples {
    [RequireComponent(typeof(RectTransform))]
    public class PaintedMask : UIBehaviour {
        public Camera renderCamera;
        public SoftMask targetMask;

        RenderTexture _renderTexture;

        protected override void Start() {
            base.Start();
            _renderTexture = new RenderTexture((int)maskSize.x, (int)maskSize.y, 0, RenderTextureFormat.ARGB32);
            _renderTexture.Create();
            renderCamera.targetTexture = _renderTexture;
            targetMask.renderTexture = _renderTexture;
        }

        Vector2 maskSize {
            get {
                var rectTransform = (RectTransform)targetMask.transform;
                return rectTransform.rect.size;
            }
        }
    }
}
