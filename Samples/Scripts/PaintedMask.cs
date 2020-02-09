using UnityEngine;
using UnityEngine.EventSystems;

namespace SoftMasking.Samples {
    [RequireComponent(typeof(RectTransform))]
    public class PaintedMask : UIBehaviour {
        public Canvas renderCanvas;
        public Camera renderCamera;
        public SoftMask targetMask;

        RenderTexture _renderTexture;

        protected override void Start() {
            base.Start();
            _renderTexture = new RenderTexture(600, 400, 0, RenderTextureFormat.ARGB32);
            _renderTexture.Create();
            renderCamera.targetTexture = _renderTexture;
            targetMask.renderTexture = _renderTexture;
        }
    }
}
