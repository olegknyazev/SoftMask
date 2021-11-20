using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace SoftMasking.Editor {
    public class DestroyImmediateFromEventTest {
        [UnityTest]
        public IEnumerator Simultaneous_deletion_of_SoftMaskable_and_OnCanvasHierarchyChanged_should_not_spam_errors() {
            var root = new GameObject("Canvas", typeof(Canvas));

            var mask = new GameObject("Mask", typeof(SoftMask));
            mask.transform.SetParent(root.transform);

            var canvasObject = new GameObject("Canvas", typeof(Canvas));
            canvasObject.GetComponent<Canvas>().overrideSorting = true;
            canvasObject.transform.SetParent(mask.transform);
            canvasObject.SetActive(false);

            yield return null;

            Object.DestroyImmediate(mask.GetComponent<SoftMask>());

            yield return null;

            Object.DestroyImmediate(canvasObject);

            yield return null;
        }
    }
}
