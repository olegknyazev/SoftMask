using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace SoftMask.Editor {
    [CustomEditor(typeof(SoftMask))]
    [CanEditMultipleObjects]
    public class SoftMaskEditor : UnityEditor.Editor {
        SerializedProperty maskSource;
        SerializedProperty maskSprite;
        SerializedProperty maskBorderMode;
        SerializedProperty maskTexture;
        SerializedProperty maskChannelWeights;

        bool _customWeightsExpanded;
        
        static class Labels {
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
            CustomEditors.WithIndent(() => {
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
            });
            CustomEditors.ChannelWeights(Labels.MaskChannel, maskChannelWeights, ref _customWeightsExpanded);
            serializedObject.ApplyModifiedProperties();
        }

        public static class CustomEditors {
            public static void ChannelWeights(GUIContent label, SerializedProperty weightsProp, ref bool customWeightsExpanded) {
                var rect = GUILayoutUtility.GetRect(GUIContent.none, KnownChannelStyle);
                if (customWeightsExpanded)
                    rect.max = GUILayoutUtility.GetRect(GUIContent.none, CustomWeightsStyle).max;
                ChannelWeights(rect, label, weightsProp, ref customWeightsExpanded);
            }

            public static void WithIndent(Action f) {
                ++EditorGUI.indentLevel;
                f();
                --EditorGUI.indentLevel;
            }

            static readonly GUIStyle KnownChannelStyle = EditorStyles.popup;
            static readonly GUIStyle CustomWeightsStyle = EditorStyles.textField;

            static void ChannelWeights(Rect rect, GUIContent label, SerializedProperty weightsProp, ref bool customWeightsExpanded) {
                var knownChannel =
                    customWeightsExpanded
                        ? KnownMaskChannel.Custom
                        : KnownChannel(weightsProp.colorValue);
                label = EditorGUI.BeginProperty(rect, label, weightsProp);
                EditorGUI.BeginChangeCheck();
                if (customWeightsExpanded)
                    rect.height = HeightOf(KnownChannelStyle);
                knownChannel = (KnownMaskChannel)EditorGUI.EnumPopup(rect, label, knownChannel);
                var weights = Weights(knownChannel, weightsProp.colorValue);
                if (customWeightsExpanded) {
                    rect.y += rect.height + Mathf.Max(KnownChannelStyle.margin.bottom, CustomWeightsStyle.margin.top);
                    rect.height = HeightOf(CustomWeightsStyle);
                    WithIndent(() => {
                        weights = ColorField(rect, Labels.ChannelWeights, weights);
                    });
                }
                if (EditorGUI.EndChangeCheck())
                    weightsProp.colorValue = weights;
                if (Event.current.type != EventType.layout)
                    customWeightsExpanded = knownChannel == KnownMaskChannel.Custom;
                EditorGUI.EndProperty();
            }

            static Color ColorField(Rect rect, GUIContent label, Color color) {
                rect = EditorGUI.PrefixLabel(rect, label);
                color.r = ColorComponentField(Part(rect, 0, 4, 2), Labels.R, color.r);
                color.g = ColorComponentField(Part(rect, 1, 4, 2), Labels.G, color.g);
                color.b = ColorComponentField(Part(rect, 2, 4, 2), Labels.B, color.b);
                color.a = ColorComponentField(Part(rect, 3, 4, 2), Labels.A, color.a);
                return color;
            }

            static float ColorComponentField(Rect rect, GUIContent label, float value) {
                return WithZeroIndent(() => {
                    var labelWidth = EditorStyles.label.CalcSize(label).x + 1;
                    EditorGUI.LabelField(new Rect(rect) { width = labelWidth }, label);
                    rect.width -= labelWidth;
                    rect.x += labelWidth;
                    return EditorGUI.FloatField(rect, value);
                });
            }

            static Rect Part(Rect whole, int part, int partCount, int spacing) {
                var result = new Rect(whole);
                result.width -= (partCount - 1) * spacing;
                result.width /= partCount;
                result.x += part * (result.width + spacing);
                return result;
            }

            static T WithZeroIndent<T>(Func<T> f) {
                var prev = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                var result = f();
                EditorGUI.indentLevel = prev;
                return result;
            }

            static float HeightOf(GUIStyle style) { return style.CalcSize(GUIContent.none).y; }

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

}
 