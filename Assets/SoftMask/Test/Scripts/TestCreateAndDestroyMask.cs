using System.Collections;
using UnityEngine;

public class TestCreateAndDestroyMask : MonoBehaviour {
    public GameObject panel;

    public IEnumerator Start() {
        SoftMask.SoftMask mask = null;
        while (true) {
            if (mask) {
                DestroyImmediate(mask);
                mask = null;
            } else {
                mask = panel.AddComponent<SoftMask.SoftMask>();
            }
            yield return new WaitForSeconds(1);
        }
    }
}
