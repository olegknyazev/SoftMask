using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SoftMasking.Tests {
    public class TestCreateMaskWithoutCanvas : MonoBehaviour {
        public Shader shader;
        public AutomatedTest automatedTest;

        public IEnumerator Start() {
            while (true) {
                var maskObject = new GameObject("Mask Object");
                maskObject.AddComponent<Image>();
                var mask = maskObject.AddComponent<SoftMask>();
                mask.defaultShader = shader;
                // Change something to make mask dirty. It's a regression test: there was a bug in version 1.2.4
                // caused assertion failures in that case.
                mask.channelWeights = MaskChannel.gray;

                yield return automatedTest.Proceed(1f);

                DestroyImmediate(maskObject);
                yield return automatedTest.Proceed(1f);

                yield return automatedTest.Finish();
            }
        }
    }
}
