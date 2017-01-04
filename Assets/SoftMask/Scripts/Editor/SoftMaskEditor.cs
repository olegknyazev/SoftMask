using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace SoftMask.Editor {
    [CustomEditor(typeof(SoftMask))]
    public class SoftMaskEditor : UnityEditor.Editor {
        SerializedProperty maskSource;
        SerializedProperty maskSprite;
        SerializedProperty maskBorderMode;
        SerializedProperty maskTexture;
        SerializedProperty maskChannelWeights;
        bool _customChannelWeights;
        
        static class Content {
            public static readonly GUIContent MaskChannel = new GUIContent("Mask Channel");
            public static readonly GUIContent ChannelWeights = new GUIContent("Channel Weights");
            public static readonly GUIContent R = new GUIContent("R");
            public static readonly GUIContent G = new GUIContent("G");
            public static readonly GUIContent B = new GUIContent("B");
            public static readonly GUIContent A = new GUIContent("A");
        }

        void OnEnable() {
            maskSource = serializedObject.FindProperty("_maskSource");
            maskSprite = serializedObject.FindProperty("_maskSprite");
            maskBorderMode = serializedObject.FindProperty("_maskBorderMode");
            maskTexture = serializedObject.FindProperty("_maskTexture");
            maskChannelWeights = serializedObject.FindProperty("_maskChannelWeights");
            Assert.IsNotNull(maskSource);
            Assert.IsNotNull(maskSprite);
            Assert.IsNotNull(maskBorderMode);
            Assert.IsNotNull(maskTexture);
            Assert.IsNotNull(maskChannelWeights);
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();
            EditorGUILayout.PropertyField(maskSource);
            EditorGUI.indentLevel += 1;
            switch ((SoftMask.MaskSource)maskSource.enumValueIndex) {
                case SoftMask.MaskSource.Graphic:
                    break;
                case SoftMask.MaskSource.Sprite:
                    EditorGUILayout.PropertyField(maskSprite);
                    EditorGUILayout.PropertyField(maskBorderMode);
                    break;
                case SoftMask.MaskSource.Texture:
                    EditorGUILayout.PropertyField(maskTexture);
                    break;
            }
            EditorGUI.indentLevel -= 1;
            maskChannelWeights.colorValue = ChannelWeightsGUI(maskChannelWeights.colorValue);
            serializedObject.ApplyModifiedProperties();
        }

        enum KnownMaskChannel { Alpha, Red, Green, Blue, Gray, Custom }

        KnownMaskChannel KnownChannel(Color weights) {
            if (weights == MaskChannel.alpha) return KnownMaskChannel.Alpha;
            else if (weights == MaskChannel.red) return KnownMaskChannel.Red;
            else if (weights == MaskChannel.green) return KnownMaskChannel.Green;
            else if (weights == MaskChannel.blue) return KnownMaskChannel.Blue;
            else if (weights == MaskChannel.gray) return KnownMaskChannel.Gray;
            else return KnownMaskChannel.Custom;
        }

        Color Weights(KnownMaskChannel channel, Color weights) {
            switch (channel) {
                case KnownMaskChannel.Alpha: return MaskChannel.alpha;
                case KnownMaskChannel.Red: return MaskChannel.red;
                case KnownMaskChannel.Green: return MaskChannel.green;
                case KnownMaskChannel.Blue: return MaskChannel.blue;
                case KnownMaskChannel.Gray: return MaskChannel.gray;
                default: return weights;
            }
        }

        Color ChannelWeightsGUI(Color weights) {
            var knownChannel = KnownChannel(weights);
            knownChannel = (KnownMaskChannel)EditorGUILayout.EnumPopup(Content.MaskChannel, knownChannel);
            weights = Weights(knownChannel, weights);
            if (knownChannel == KnownMaskChannel.Custom)
                _customChannelWeights = true;
            if (_customChannelWeights) {
                EditorGUI.indentLevel += 1;
                weights = ChannelWeightsControl(weights);
                EditorGUI.indentLevel -= 1;
            }
            return weights;
        }

        static float WeightComponentField(Rect rect, GUIContent label, float value) {
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

        static Color ChannelWeightsControl(Rect rect, Color weights) {
            var id = GUIUtility.GetControlID(FocusType.Passive);
            rect = EditorGUI.PrefixLabel(rect, Content.ChannelWeights);
            weights.r = WeightComponentField(Part(rect, 0, 4, 2), Content.R, weights.r);
            weights.g = WeightComponentField(Part(rect, 1, 4, 2), Content.G, weights.g);
            weights.b = WeightComponentField(Part(rect, 2, 4, 2), Content.B, weights.b);
            weights.a = WeightComponentField(Part(rect, 3, 4, 2), Content.A, weights.a);
            return weights;
        }

        static Color ChannelWeightsControl(Color weights) {
            var rect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.label);
            return ChannelWeightsControl(rect, weights);
        }
    }
}
 