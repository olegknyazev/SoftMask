using System.Collections;
using UnityEngine;
using SoftMasking;

public class TestCreateAndDestroyMask : MonoBehaviour {
    public GameObject panel;
    public Shader shader;

    public IEnumerator Start() {
        SoftMask mask = null;
        while (true) {
            if (mask) {
                DestroyImmediate(mask);
                mask = null;
            } else {
                mask = panel.AddComponent<SoftMask>();
                mask.defaultShader = shader;
            }
            yield return new WaitForSeconds(1);
        }
    }
}
