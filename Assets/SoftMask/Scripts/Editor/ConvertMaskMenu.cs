using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace SoftMasking.Editor {
    public static class ConvertMaskMenu {
        [MenuItem("Tools/Soft Mask/Convert Mask to Soft Mask")]
        public static void Convert() {
            Assert.IsTrue(CanConvert());
            var selectedTransforms = Selection.GetTransforms(SelectionMode.Editable);
            foreach (var transform in selectedTransforms)
                Convert(transform.gameObject);
        }
        
        [MenuItem("Tools/Soft Mask/Convert Mask to Soft Mask", validate = true)]
        public static bool CanConvert() {
            var selectedTransforms = Selection.GetTransforms(SelectionMode.Editable);
            return selectedTransforms.Any()
                && selectedTransforms.All(t => IsConvertibleMask(t.gameObject));
        }

        static bool IsConvertibleMask(GameObject gameObject) {
            var mask = gameObject.GetComponent<Mask>();
            if (!mask)
                return false;
            var graphic = gameObject.GetComponent<Graphic>();
            if (!graphic) 
                return false;
            return graphic is Image || graphic is RawImage;
        }

        static void Convert(GameObject gameObject) {
            Assert.IsTrue(IsConvertibleMask(gameObject));
            var mask = gameObject.GetComponent<Mask>();
            var graphic = gameObject.GetComponent<Graphic>();
            var softMask = gameObject.AddComponent<SoftMask>();
            if (mask.showMaskGraphic) {
                softMask.source = SoftMask.MaskSource.Graphic;
                UnityEngine.Object.DestroyImmediate(mask);
            } else {
                if (graphic is Image) {
                    var image = (Image)graphic;
                    softMask.source = SoftMask.MaskSource.Sprite;
                    softMask.sprite = SoftMaskCompatibleVersionOf(image.sprite);
                    softMask.spriteBorderMode = BorderModeOf(image); // TODO check unsupported borderMode
                #if UNITY_2019_2_OR_NEWER
                    softMask.spritePixelsPerUnitMultiplier = image.pixelsPerUnitMultiplier;
                #endif
                    UnityEngine.Object.DestroyImmediate(mask);
                    UnityEngine.Object.DestroyImmediate(image);
                } else if (graphic is RawImage) {
                    var rawImage = (RawImage)graphic;
                    softMask.source = SoftMask.MaskSource.Texture;
                    if (rawImage.texture is Texture2D)
                        softMask.texture = (Texture2D)rawImage.texture;
                    else if (rawImage.texture is RenderTexture)
                        softMask.renderTexture = (RenderTexture)rawImage.texture;
                    else
                        ; // TODO report error
                    softMask.textureUVRect = rawImage.uvRect;
                    UnityEngine.Object.DestroyImmediate(mask);
                    UnityEngine.Object.DestroyImmediate(rawImage);
                } else {
                    Debug.LogAssertionFormat("Converted Game Object should have an Image or Raw Image component");
                }
            }
            //Undo.RecordObjects();
        }

        static Sprite SoftMaskCompatibleVersionOf(Sprite original) {
            return original == standardUIMaskSprite
                ? adaptedUIMaskSprite
                : original;
        }

        // TODO copied from SoftMask.cs
        static SoftMask.BorderMode BorderModeOf(Image image) {
            switch (image.type) {
                case Image.Type.Simple: return SoftMask.BorderMode.Simple;
                case Image.Type.Sliced: return SoftMask.BorderMode.Sliced;
                case Image.Type.Tiled: return SoftMask.BorderMode.Tiled;
                default:
                    return SoftMask.BorderMode.Simple;
            }
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
 