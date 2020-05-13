using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using UnityEngine.UI;

namespace SoftMasking.Editor {
    public class ConvertMaskMenuTest {
        [Test] public void WhenNoObjectSelected_ShouldBeNotAvailable() {
            Selection.objects = new Object [0];
            Assert.IsFalse(ConvertMaskMenu.CanConvert());
        }

        [Test] public void WhenEmptyObjectSelected_ShouldBeNotAvailable() {
            var go = new GameObject("EmptyTestObject");
            Selection.objects = new [] { go };
            Assert.IsFalse(ConvertMaskMenu.CanConvert());
            Object.DestroyImmediate(go);
        }

        [Test] public void WhenConvertibleObjectsSelected_ShouldBeAvailable() {
            var go = new GameObject("TestObject", typeof(Mask), typeof(Image));
            Selection.objects = new [] { go };
            Assert.IsTrue(ConvertMaskMenu.CanConvert());
            Object.DestroyImmediate(go);
        }

        [Test] public void WhenInvokedOnNonRenderableImage_ShouldConvertItToSpriteSoftMask() {
            var standardSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            var go = new GameObject("TestObject");
            var image = go.AddComponent<Image>();
            image.sprite = standardSprite;
            image.type = Image.Type.Sliced;
            var mask = go.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            Selection.objects = new [] { go };
            ConvertMaskMenu.Convert();
            var softMask = go.GetComponent<SoftMask>();
            Assert.AreEqual(standardSprite, softMask.sprite);
            Assert.AreEqual(SoftMask.BorderMode.Sliced, softMask.spriteBorderMode);
            // TODO check standard mask is removed (here or a seprate test?)
            Object.DestroyImmediate(go);
            // TODO use a `using` scope?
        }
    }
}
