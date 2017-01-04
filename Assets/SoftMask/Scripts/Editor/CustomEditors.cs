using UnityEditor;
using UnityEngine;

namespace SoftMask.Editor {
    public static class CustomEditors {
        static class Labels {
            public static readonly GUIContent ChannelWeights = new GUIContent("Channel Weights");
            public static readonly GUIContent R = new GUIContent("R");
            public static readonly GUIContent G = new GUIContent("G");
            public static readonly GUIContent B = new GUIContent("B");
            public static readonly GUIContent A = new GUIContent("A");
        }

        public static void ChannelWeights(GUIContent label, SerializedProperty weightsProp, ref bool customWeightsExpanded) {
            var rect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.popup);
            if (customWeightsExpanded) {
                var secondLine = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.textField);
                rect.max = secondLine.max;
            }   
            ChannelWeights(rect, label, weightsProp, ref customWeightsExpanded);
        }

        public static Color ColorField(Rect rect, GUIContent label, Color color) {
            rect = EditorGUI.PrefixLabel(rect, label);
            color.r = ColorComponentField(Part(rect, 0, 4, 2), Labels.R, color.r);
            color.g = ColorComponentField(Part(rect, 1, 4, 2), Labels.G, color.g);
            color.b = ColorComponentField(Part(rect, 2, 4, 2), Labels.B, color.b);
            color.a = ColorComponentField(Part(rect, 3, 4, 2), Labels.A, color.a);
            return color;
        }

        public static Color ColorField(GUIContent label, Color color) {
            var rect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.label);
            return ColorField(rect, label, color);
        }

        static void ChannelWeights(Rect rect, GUIContent label, SerializedProperty weightsProp, ref bool customWeightsExpanded) {
            var wasExpanded = customWeightsExpanded;
            var knownChannel =
                wasExpanded
                    ? KnownMaskChannel.Custom
                    : KnownChannel(weightsProp.colorValue);
            label = EditorGUI.BeginProperty((Rect)rect, label, weightsProp);
            EditorGUI.BeginChangeCheck();
            if (wasExpanded)
                rect.height = EditorStyles.popup.CalcSize(label).y;
            knownChannel = (KnownMaskChannel)EditorGUI.EnumPopup((Rect)rect, label, knownChannel);
            customWeightsExpanded = knownChannel == KnownMaskChannel.Custom;
            var weights = Weights(knownChannel, weightsProp.colorValue);
            if (wasExpanded) {
                rect.y += rect.height + Mathf.Max(EditorStyles.popup.margin.bottom, EditorStyles.textField.margin.top);
                rect.height = EditorStyles.textField.CalcSize(label).y;
                EditorGUI.indentLevel += 1;
                weights = ColorField(rect, Labels.ChannelWeights, weights);
                EditorGUI.indentLevel -= 1;
            }
            if (EditorGUI.EndChangeCheck())
                weightsProp.colorValue = weights;
            EditorGUI.EndProperty();
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

        enum KnownMaskChannel { Alpha, Red, Green, Blue, Gray, Custom }

        static KnownMaskChannel KnownChannel(Color weights) {
            if (weights == MaskChannel.alpha) return KnownMaskChannel.Alpha;
            else if (weights == MaskChannel.red) return KnownMaskChannel.Red;
            else if (weights == MaskChannel.green) return KnownMaskChannel.Green;
            else if (weights == MaskChannel.blue) return KnownMaskChannel.Blue;
            else if (weights == MaskChannel.gray) return KnownMaskChannel.Gray;
            else return KnownMaskChannel.Custom;
        }

        static Color Weights(KnownMaskChannel known, Color custom) {
            switch (known) {
                case KnownMaskChannel.Alpha: return MaskChannel.alpha;
                case KnownMaskChannel.Red: return MaskChannel.red;
                case KnownMaskChannel.Green: return MaskChannel.green;
                case KnownMaskChannel.Blue: return MaskChannel.blue;
                case KnownMaskChannel.Gray: return MaskChannel.gray;
                default: return custom;
            }
        }
    }
}
 