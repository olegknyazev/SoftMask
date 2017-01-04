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
                weights = PerComponentColorEditor.ColorField(Content.ChannelWeights, weights);
                EditorGUI.indentLevel -= 1;
            }
            return weights;
        }
    }
}
 