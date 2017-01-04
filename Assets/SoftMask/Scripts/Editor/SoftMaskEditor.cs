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
        
        static class Labels {
            public static readonly GUIContent MaskChannel = new GUIContent("Mask Channel");
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
            CustomEditors.ChannelWeights(Labels.MaskChannel, maskChannelWeights, ref _customChannelWeights);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
 