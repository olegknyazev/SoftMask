using UnityEditor;
using UnityEngine;

namespace SoftMask.Editor {
    public static class PerComponentColorEditor {
        static class Content {
            public static readonly GUIContent R = new GUIContent("R");
            public static readonly GUIContent G = new GUIContent("G");
            public static readonly GUIContent B = new GUIContent("B");
            public static readonly GUIContent A = new GUIContent("A");
        }

        public static Color ColorField(Rect rect, GUIContent label, Color color) {
            var id = GUIUtility.GetControlID(FocusType.Passive);
            rect = EditorGUI.PrefixLabel(rect, label);
            color.r = ColorComponentField(Part(rect, 0, 4, 2), Content.R, color.r);
            color.g = ColorComponentField(Part(rect, 1, 4, 2), Content.G, color.g);
            color.b = ColorComponentField(Part(rect, 2, 4, 2), Content.B, color.b);
            color.a = ColorComponentField(Part(rect, 3, 4, 2), Content.A, color.a);
            return color;
        }

        public static Color ColorField(GUIContent label, Color color) {
            var rect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.label);
            return ColorField(rect, label, color);
        }

        static float ColorComponentField(Rect rect, GUIContent label, float value) {
            var prev = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            var content = new GUIContent(label);
            var labelWidth = EditorStyles.label.CalcSize(content).x + 1;
            EditorGUI.LabelField(new Rect(rect) { width = labelWidth }, content);
            rect.width -= labelWidth;
            rect.x += labelWidth;
            value = EditorGUI.FloatField(rect, value);
            EditorGUI.indentLevel = prev;
            return value;
        }

        static Rect Part(Rect whole, int part, int partCount, int spacing) {
            var result = new Rect(whole);
            result.width -= (partCount - 1) * spacing;
            result.width /= partCount;
            result.x += part * (result.width + spacing);
            return result;
        }
    }
}
 