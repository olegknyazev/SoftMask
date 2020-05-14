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

        [Test] public void AfterInvokedOnNonRenderableImage_SelectedObjectShouldHaveSoftMask() {
            var go = CreateAndConvertImageMask(renderable: false);
            var softMask = go.GetComponent<SoftMask>();
            Assert.IsNotNull(softMask);
            Assert.AreEqual(standardUISprite, softMask.sprite);
            Assert.AreEqual(SoftMask.BorderMode.Sliced, softMask.spriteBorderMode);
        }

        GameObject CreateAndConvertImageMask(bool renderable) {
            var go = CreateObjectWithImageMask(renderable);
            SelectObjects(go);
            ConvertMaskMenu.Convert();
            return go;
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

        [Test] public void AfterInvokedOnNonRenderableImage_SelectedObjectShouldHaveNoStandardMask() {
            var go = CreateAndConvertImageMask(renderable: false);
            Assert.IsNull(go.GetComponent<Mask>());
        }

        [Test] public void AfterInvokedOnNonRenderableImage_SelectedObjectShouldHaveNoImage() {
            var go = CreateAndConvertImageMask(renderable: false);
            Assert.IsNull(go.GetComponent<Image>());
        }

        [Test] public void AfterInvokedOnRenderableImage_SelectedObjectShouldHaveSoftMask() {
            var go = CreateAndConvertImageMask(renderable: true);
            var softMask = go.GetComponent<SoftMask>();
            Assert.IsNotNull(softMask);
            Assert.AreEqual(SoftMask.MaskSource.Graphic, softMask.source);
        }

        [Test] public void AfterInvokedOnRenderableImage_SelectedObjectShouldStillHaveImage() {
            var go = CreateAndConvertImageMask(renderable: true);
            Assert.IsNotNull(go.GetComponent<Image>());
        }

        [Test] public void AfterInvokedOnRenderableImage_SelectedObjectShouldHaveNoStandardMask() {
            var go = CreateAndConvertImageMask(renderable: true);
            Assert.IsNull(go.GetComponent<Mask>());
        }

        [Test] public void AfterInvokedOnNonRenderableRawImage_SelectedObjectShouldHaveSoftMask() {
            var go = CreateAndConvertRawImageMask(renderable: false);
            var softMask = go.GetComponent<SoftMask>();
            Assert.IsNotNull(softMask);
            Assert.AreEqual(standardUISprite.texture, softMask.texture);
            Assert.AreEqual(standardRect, softMask.textureUVRect);
        }
        
        GameObject CreateAndConvertRawImageMask(bool renderable) {
            var go = CreateObjectWithRawImageMask(renderable);
            SelectObjects(go);
            ConvertMaskMenu.Convert();
            return go;
        }

        static readonly Rect standardRect = new Rect(0.2f, 0.1f, 0.7f, 0.6f);

        GameObject CreateObjectWithRawImageMask(bool renderable) {
            var go = CreateGameObject();
            var image = go.AddComponent<RawImage>();
            image.texture = standardUISprite.texture;
            image.uvRect = standardRect;
            var mask = go.AddComponent<Mask>();
            mask.showMaskGraphic = renderable;
            return go;
        }

        [Test] public void AfterInvokedOnNonRenderableRawImage_SelectedObjectShouldHaveNoStandardMask() {
            var go = CreateAndConvertRawImageMask(renderable: false);
            Assert.IsNull(go.GetComponent<Mask>());
        }

        [Test] public void AfterInvokedOnNonRenderableRawImage_SelectedObjectShouldHaveNoRawImage() {
            var go = CreateAndConvertRawImageMask(renderable: false);
            Assert.IsNull(go.GetComponent<RawImage>());
        }
    }
}
