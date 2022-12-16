using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace SoftMasking.Editor {
    public static class ConvertMaskMenu {
        [MenuItem("Tools/Soft Mask/Convert Mask to Soft Mask")]
        public static void ConvertMenu() {
            var undoGroup = Undo.GetCurrentGroup();
            try {
                Convert();
            } catch (UnsupportedRawImageTextureType ex) {
                EditorUtility.DisplayDialog(
                    "Soft Mask",
                    string.Format(
                        "Unable to convert object '{0}' to Soft Mask. It's Raw Image component has Texture of an unsupported type: {1}.",
                        ex.objectBeingConverted.name,
                        ex.unsupportedTexture.GetType().Name),
                    "OK");
                Undo.RevertAllDownToGroup(undoGroup);
            } catch (UnsupportedImageType ex) {
                EditorUtility.DisplayDialog(
                    "Soft Mask",
                    string.Format(
                        "Unable to convert object '{0}' to SoftMask. It's Image component has unsupported type: {1}.",
                        ex.objectBeingConverted.name,
                        ex.unsupportedType),
                    "OK");
                Undo.RevertAllDownToGroup(undoGroup);
            }
        }
        
        [MenuItem("Tools/Soft Mask/Convert Mask to Soft Mask", validate = true)]
        public static bool CanConvertMenu() {
            return CanConvert();
        }
        
        public static void Convert() {
            Assert.IsTrue(CanConvert());
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Convert Mask to Soft Mask");
            var selectedTransforms = Selection.GetTransforms(SelectionMode.Editable);
            foreach (var transform in selectedTransforms)
                Convert(transform.gameObject);
        }
        
        public static bool CanConvert() {
            var selectedTransforms = Selection.GetTransforms(SelectionMode.Editable);
            return selectedTransforms.Any()
                && selectedTransforms.All(t => IsConvertibleMask(t.gameObject));
        }

        static bool IsConvertibleMask(GameObject gameObject) {
            var mask = gameObject.GetComponent<Mask>();
            var graphic = gameObject.GetComponent<Graphic>();
            return mask
                && graphic
                && (graphic is Image || graphic is RawImage);
        }

        static void Convert(GameObject gameObject) {
            Assert.IsTrue(IsConvertibleMask(gameObject));
            DeepCheckConvertibility(gameObject);
            var mask = gameObject.GetComponent<Mask>();
            var softMask = Undo.AddComponent<SoftMask>(gameObject);
            var mayUseGraphic = MayUseGraphicSource(mask);
            if (mayUseGraphic) {
                softMask.source = SoftMask.MaskSource.Graphic;
                Undo.DestroyObjectImmediate(mask);
            } else {
                var graphic = gameObject.GetComponent<Graphic>();
                if (graphic is Image)
                    SetUpFromImage(softMask, (Image)graphic);
                else if (graphic is RawImage)
                    SetUpFromRawImage(softMask, (RawImage)graphic);
                else
                    Debug.LogAssertionFormat("Converted Game Object should have an Image or Raw Image component");
                Undo.DestroyObjectImmediate(mask);
                if (!mask.showMaskGraphic)
                    Undo.DestroyObjectImmediate(graphic);
            }
        }

        static void DeepCheckConvertibility(GameObject gameObject) {
            var rawImage = gameObject.GetComponent<RawImage>();
            if (rawImage) {
                var texture = rawImage.texture;
                if (texture && !(texture is Texture2D) && !(texture is RenderTexture))
                    throw new UnsupportedRawImageTextureType(gameObject, texture);
            }
            var image = gameObject.GetComponent<Image>();
            if (image && !SoftMask.IsImageTypeSupported(image.type))
                throw new UnsupportedImageType(image.gameObject, image.type);
        }

        public class UnsupportedImageType : Exception {
            public UnsupportedImageType(GameObject objectBeingConverted, Image.Type unsupportedType) {
                this.objectBeingConverted = objectBeingConverted;
                this.unsupportedType = unsupportedType;
            }
            public GameObject objectBeingConverted { get; private set; }
            public Image.Type unsupportedType { get; private set; }
        }

        public class UnsupportedRawImageTextureType : Exception {
            public UnsupportedRawImageTextureType(GameObject objectBeingConverted, Texture unsupportedTexture) {
                this.objectBeingConverted = objectBeingConverted;
                this.unsupportedTexture = unsupportedTexture;
            }
            public GameObject objectBeingConverted { get; private set; }
            public Texture unsupportedTexture { get; private set; }
        }

        static bool MayUseGraphicSource(Mask mask) {
            var image = mask.GetComponent<Image>();
            var usesStandardUIMaskSprite = image && IsStandardUIMaskSprite(image.sprite);
            return mask.showMaskGraphic
                && !usesStandardUIMaskSprite;
        }

        static bool IsStandardUIMaskSprite(Sprite sprite) {
            return sprite == standardUIMaskSprite;
        }

        static Sprite SoftMaskCompatibleVersionOf(Sprite original) {
            return IsStandardUIMaskSprite(original)
                ? adaptedUIMaskSprite
                : original;
        }

        static void SetUpFromImage(SoftMask softMask, Image image) {
            Assert.IsTrue(SoftMask.IsImageTypeSupported(image.type));
            softMask.source = SoftMask.MaskSource.Sprite;
            softMask.sprite = SoftMaskCompatibleVersionOf(image.sprite);
            softMask.spriteBorderMode = SoftMask.ImageTypeToBorderMode(image.type);
        #if UNITY_2019_2_OR_NEWER
            softMask.spritePixelsPerUnitMultiplier = image.pixelsPerUnitMultiplier;
        #endif
        }

        static void SetUpFromRawImage(SoftMask softMask, RawImage rawImage) {
            softMask.source = SoftMask.MaskSource.Texture;
            var texture = rawImage.texture;
            if (texture)
                if (texture is Texture2D)
                    softMask.texture = (Texture2D)texture;
                else if (texture is RenderTexture)
                    softMask.renderTexture = (RenderTexture)texture;
                else
                    Debug.LogAssertionFormat("Unsupported RawImage texture type: {0}", texture);
            softMask.textureUVRect = rawImage.uvRect;
        }

        static Sprite _standardUIMaskSprite;
        public static Sprite standardUIMaskSprite {
            get {
                if (!_standardUIMaskSprite)
                    _standardUIMaskSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd");
                return _standardUIMaskSprite;
            }
        }

        static Sprite _adaptedUIMaskSprite;
        public static Sprite adaptedUIMaskSprite {
            get {
                if (!_adaptedUIMaskSprite)
                    _adaptedUIMaskSprite =
                        AssetDatabase.LoadAssetAtPath<Sprite>(
                            Path.Combine(PackageResources.packagePath, "Sprites/UIMask-FullAlpha.png"));
                return _adaptedUIMaskSprite;
            }
        }
    }
}