using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SoftMasking.Tests {
    public class TestPremultipliedAlpha : MonoBehaviour {
        public Camera renderTextureCamera;
        public RawImage display;
        
        public AutomatedTest automatedTest;

        RenderTexture renderTexture;
        
        public IEnumerator Start() {
            renderTexture = new RenderTexture(300, 300, 32, RenderTextureFormat.Default);
            renderTextureCamera.targetTexture = renderTexture;
            display.texture = renderTexture;
            yield return null;
            yield return null;
            yield return automatedTest.Proceed(1f);
            yield return automatedTest.Finish();
        }

        public void OnDestroy() {
            if (renderTexture) {
                renderTextureCamera.targetTexture = null;
                display.texture = null;
                DestroyImmediate(renderTexture);
            }
        }
    }
}
