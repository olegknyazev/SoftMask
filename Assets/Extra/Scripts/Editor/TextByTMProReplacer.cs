using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SoftMasking.TextMeshPro.Editor {
    public static class TextByTMProReplacer {
        [MenuItem("Tools/Soft Mask/Replace Selected Texts by TextMesh Pro")]
        public static void Replace() {
            foreach (var text in selectedTexts)
                Replace(text);
        }

        [MenuItem("Tools/Soft Mask/Replace Selected Texts by TextMesh Pro", isValidateFunction: true)]
        public static bool ReplaceAvailable() { return selectedTexts.Any(); }

        static IEnumerable<Text> selectedTexts => Selection.GetFiltered(typeof(Text), SelectionMode.Editable).Cast<Text>();

        static void Replace(Text text) {
            var obj = text.gameObject;
            var props = new {
                textValue = text.text,
                font = MatchFont(text.font),
                text.fontSize,
                text.color,
                alignment = ConvertAlignment(text.alignment)
            };

            Undo.DestroyObjectImmediate(text);

            var tmpro = Undo.AddComponent<TextMeshProUGUI>(obj);
            Undo.RecordObject(tmpro, "Set up TextMesh Pro properties");
            tmpro.text = props.textValue;
            tmpro.font = props.font;
            tmpro.fontSize = props.fontSize;
            tmpro.color = props.color;
            tmpro.alignment = props.alignment;
        }

        static TMP_FontAsset MatchFont(Font original) {
            return AssetDatabase
                .FindAssets(original.name + " SDF")
                .Select(x => AssetDatabase.GUIDToAssetPath(x))
                .Select(x => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(x))
                .FirstOrDefault();
        }

        static TextAlignmentOptions ConvertAlignment(TextAnchor alignment) {
            switch (alignment) {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
                case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
                default:
                    Debug.LogErrorFormat("Unknown TextAnchor: {0}", alignment);
                    return TextAlignmentOptions.TopLeft;
            }
        }
    }
}
