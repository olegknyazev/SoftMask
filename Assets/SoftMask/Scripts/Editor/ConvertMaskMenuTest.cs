using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Object = UnityEngine.Object;

namespace SoftMasking.Editor {
    public class ConvertMaskMenuTest {
        List<GameObject> objectsToDestroy = new List<GameObject>();

        [TearDown] public void TearDown() {
            foreach (var obj in objectsToDestroy)
                GameObject.DestroyImmediate(obj);
        }

        [Test] public void WhenNoObjectSelected_ShouldBeNotAvailable() {
            SelectObjects();
            Assert.IsFalse(ConvertMaskMenu.CanConvert());
        }

        [Test] public void WhenEmptyObjectSelected_ShouldBeNotAvailable() {
            var go = CreateGameObject();
            SelectObjects(go);
            Assert.IsFalse(ConvertMaskMenu.CanConvert());
        }

        GameObject CreateGameObject(params Type[] componentTypes) {
            var go = new GameObject("TestObject", componentTypes);
            objectsToDestroy.Add(go);
            return go;
        }

        void SelectObjects(params GameObject[] objects) {
            Selection.objects = objects.Cast<Object>().ToArray();
        }

        [Test] public void WhenConvertibleObjectsSelected_ShouldBeAvailable() {
            var go = CreateGameObject(typeof(Mask), typeof(Image));
            SelectObjects(go);
            Assert.IsTrue(ConvertMaskMenu.CanConvert());
        }
        
        [Test] public void WhenInvokedOnSeveralObjects_TheyAllShouldBeConverted() {
            var gos = new [] {
                CreateObjectWithImageMask(renderable: true),
                CreateObjectWithImageMask(renderable: false),
                CreateObjectWithRawImageMask(renderable: true),
                CreateObjectWithRawImageMask(renderable: false),
            };
            SelectObjects(gos);
            ConvertMaskMenu.Convert();
            AssertConvertedProperty(gos[0], renderable: true, raw: false);
            AssertConvertedProperty(gos[1], renderable: false, raw: false);
            AssertConvertedProperty(gos[2], renderable: true, raw: true);
            AssertConvertedProperty(gos[3], renderable: false, raw: true);
        }

        void AssertConvertedProperty(GameObject go, bool renderable, bool raw) {
            var softMask = go.GetComponent<SoftMask>();
            Assert.IsNotNull(softMask);
            Assert.IsNull(go.GetComponent<Mask>());
            if (renderable)
                Assert.AreEqual(SoftMask.MaskSource.Graphic, softMask.source);
            else {
                if (raw) {
                    Assert.IsNull(go.GetComponent<RawImage>());
                    Assert.AreEqual(standardUISprite.texture, softMask.texture);
                    Assert.AreEqual(standardRect, softMask.textureUVRect);
                } else {
                    Assert.IsNull(go.GetComponent<Image>());
                    Assert.AreEqual(standardUISprite, softMask.sprite);
                    Assert.AreEqual(SoftMask.BorderMode.Sliced, softMask.spriteBorderMode);
                    // TODO check pixelsPerUnitMultiplier in 2019.2
                }
            }
        }

        GameObject CreateObjectWithImageMask(bool renderable) {
            var go = CreateGameObject();
            var image = go.AddComponent<Image>();
            image.sprite = standardUISprite;
            image.type = Image.Type.Sliced;
            var mask = go.AddComponent<Mask>();
            mask.showMaskGraphic = renderable;
            return go;
        }

        static Sprite _standardUISprite;
        static Sprite standardUISprite {
            get {
                return _standardUISprite
                    ? _standardUISprite
                    : (_standardUISprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"));
            }
        }

        GameObject CreateObjectWithRawImageMask(bool renderable) {
            var go = CreateGameObject();
            var image = go.AddComponent<RawImage>();
            image.texture = standardUISprite.texture;
            image.uvRect = standardRect;
            var mask = go.AddComponent<Mask>();
            mask.showMaskGraphic = renderable;
            return go;
        }

        static readonly Rect standardRect = new Rect(0.2f, 0.1f, 0.7f, 0.6f);
    }
}
