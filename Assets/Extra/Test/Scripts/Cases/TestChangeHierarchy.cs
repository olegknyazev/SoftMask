using System.Collections;
using UnityEngine;

namespace SoftMasking.Tests {
    public class TestChangeHierarchy : MonoBehaviour {
        public RectTransform panelWithMask;
        public RectTransform panelWithoutMask;
        public RectTransform[] objects;

        public AutomatedTest automatedTest;

        public IEnumerator Start() {
            for (int counter = 0;; ++counter) {
                for (int i = 0; i < objects.Length; ++i) {
                    var obj = objects[i];
                    obj.transform.SetParent(AnotherPanel(obj.transform.parent.GetComponent<RectTransform>()), false);
                    yield return automatedTest.Proceed(0.5f);
                }
                yield return automatedTest.Proceed(1.0f);
                if (counter == 1)
                    automatedTest.Finish();
            }
        }

        RectTransform AnotherPanel(RectTransform current) {
            return current == panelWithMask ? panelWithoutMask : panelWithMask;
        }
    }
}
