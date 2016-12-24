using System.Collections;
using UnityEngine;

public class TestChangeHierarchy : MonoBehaviour {
    public RectTransform panelWithMask;
    public RectTransform panelWithoutMask;
    public RectTransform[] objects;

    public IEnumerator Start() {
        while (true) {
            for (int i = 0; i < objects.Length; ++i) {
                var obj = objects[i];
                obj.transform.SetParent(AnotherPanel(obj.transform.parent.GetComponent<RectTransform>()), false);
                yield return new WaitForSeconds(0.5f);
            }
            yield return new WaitForSeconds(1.0f);
        }
    }

    RectTransform AnotherPanel(RectTransform current) {
        return current == panelWithMask ? panelWithoutMask : panelWithMask;
    }
}
