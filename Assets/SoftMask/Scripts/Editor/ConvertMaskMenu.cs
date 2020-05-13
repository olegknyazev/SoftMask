using System;
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
            if (mask.showMaskGraphic)
                softMask.source = SoftMask.MaskSource.Graphic;
            else {
                if (graphic is Image) {
                    var image = (Image)graphic;
                    softMask.source = SoftMask.MaskSource.Sprite;
                    softMask.sprite = image.sprite;
                    softMask.spriteBorderMode = BorderModeOf(image); // TODO check unsupported borderMode
                #if UNITY_2019_2_OR_NEWER
                    softMask.spritePixelsPerUnitMultiplier = image.pixelsPerUnitMultiplier;
                #endif
                    UnityEngine.Object.DestroyImmediate(mask);
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
                    // TODO remove standard mask
                } else {
                    Debug.LogAssertionFormat("Converted Game Object should have an Image or Raw Image component");
                }
            }
            //Undo.RecordObjects();
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
    }
}
 