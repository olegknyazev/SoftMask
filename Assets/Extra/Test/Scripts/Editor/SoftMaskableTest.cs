using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SoftMasking.Editor {
    public class SoftMaskableTest {
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

        [UnityTest] public IEnumerator Simultaneous_deletion_of_SoftMaskable_and_SoftMask_should_not_spam_errors() {
            var root = new GameObject("Canvas", typeof(Canvas));

            var mask = new GameObject("Mask", typeof(SoftMask));
            mask.transform.SetParent(root.transform);

            var nestedMask = new GameObject("NestedMask", typeof(SoftMask));
            nestedMask.transform.SetParent(mask.transform);
            nestedMask.SetActive(false);

            yield return null;
            
            Object.DestroyImmediate(mask.GetComponent<SoftMask>());

            yield return null;
            
            Object.DestroyImmediate(nestedMask);

            yield return null;
        }

        [UnityTest] public IEnumerator Parent_mask_removal_should_destroy_disabled_SoftMaskable() {
            var canvas = new GameObject("Canvas", typeof(Canvas));
            
            var mask = new GameObject("Mask", typeof(SoftMask));
            mask.transform.SetParent(canvas.transform);
            
            var maskable = new GameObject("Image");
            maskable.transform.SetParent(mask.transform);
            maskable.SetActive(false);
            
            yield return null;
            
            UnityEngine.Assertions.Assert.IsNotNull(maskable.GetComponent<SoftMaskable>());

            Object.DestroyImmediate(mask.GetComponent<SoftMask>());

            yield return null;

            Assert.That(maskable.GetComponent<SoftMaskable>(), Is.Null);
        }
    }
}
