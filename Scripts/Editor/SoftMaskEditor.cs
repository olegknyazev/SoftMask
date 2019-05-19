using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace SoftMasking.Editor {
    [CustomEditor(typeof(SoftMask))]
    [CanEditMultipleObjects]
    public class SoftMaskEditor : UnityEditor.Editor {
        SerializedProperty _source;
        SerializedProperty _separateMask;
        SerializedProperty _sprite;
        SerializedProperty _spriteBorderMode;
        SerializedProperty _texture;
        SerializedProperty _textureUVRect;
        SerializedProperty _channelWeights;
        SerializedProperty _raycastThreshold;
        SerializedProperty _invertMask;
        SerializedProperty _invertOutsides;

        bool _customWeightsExpanded;
        
        static class Labels {
            public static readonly GUIContent Source = new GUIContent("Source",
                "Specifies from where the mask gets its image.");
            public static readonly GUIContent Sprite = new GUIContent("Sprite",
                "Sprite that should be used as the mask image.");
            public static readonly GUIContent SpriteBorderMode = new GUIContent("Sprite Border Mode",
                "Specifies how sprite borders should be processed. Corresponds to Unity UI Image " +
                "types. The Sliced and Tiled modes are available only for sprites having borders.");
            public static readonly GUIContent Texture = new GUIContent("Texture",
                "Texture that should be used as the mask image.");
            public static readonly GUIContent TextureUVRect = new GUIContent("Texture UV Rect",
                "Specifies a normalized rectangle in UV-space defining an image part that " + 
                "should be used as the mask image.");
            public static readonly GUIContent SeparateMask = new GUIContent("Separate Mask",
                "A Rect Transform that defines the bounds of the mask in the scene. If not set " +
                "bounds of Rect Transform attached to this object is used. Use of a separate Rect " +
                "Transform allows to move or resize mask without affecting children.");
            public static readonly GUIContent RaycastThreshold = new GUIContent("Raycast Threshold",
                "Specifies the minimum value that the mask should have at any given point to " +
                "pass a pointer input event to children. 0 makes the entire mask rectangle pass " +
                "events. 1 passes events only in points where masked objects are fully opaque.");
            public static readonly GUIContent MaskChannel = new GUIContent("Mask Channel",
                "Specifies which image channel(s) should be used as the mask source.");
            public static readonly GUIContent ChannelWeights = new GUIContent("Channel Weights",
                "Specifies how much value each of image channels adds to the resulted mask value. " + 
                "The resulted mask value is calculated as the sum of products of image channel " + 
                "values and the corresponding fields");
            public static readonly GUIContent R = new GUIContent("R");
            public static readonly GUIContent G = new GUIContent("G");
            public static readonly GUIContent B = new GUIContent("B");
            public static readonly GUIContent A = new GUIContent("A");
            public static readonly GUIContent InvertMask = new GUIContent("Invert Mask",
                "If set, the mask inside the mask rectangle will be inverted.");
            public static readonly GUIContent InvertOutsides = new GUIContent("Invert Outsides",
                "If set, the mask outside the mask rectangle will be inverted. By default everything " +
                "outside the mask is transparent, so when this flag is set, the outsides of the mask " +
                "are opaque.");
            public static readonly string UnsupportedShaders = 
                "Some of children's shaders aren't supported. Mask won't work on these elements. " +
                "See the documentation for more details about how to add Soft Mask support to " +
                "custom shaders.";
            public static readonly string NestedMasks =
                "The mask may work not as expected because a child or parent SoftMask exists. " +
                "SoftMask doesn't support nesting. You can work around this limitation by nesting " +
                "a SoftMask into a Unity standard Mask or RectMask2D or vice versa.";
            public static readonly string TightPackedSprite =
                "SoftMask doesn't support tight packed sprites. Disable packing for the mask sprite " +
                "or use Rectangle pack mode.";
            public static readonly string AlphaSplitSprite =
                "SoftMask doesn't support sprites with an alpha split texture. Disable compression of " +
                "the sprite texture or use another compression type.";
            public static readonly string UnsupportedImageType =
                "SoftMask doesn't support this image type. The supported image types are Simple, Sliced " +
                "and Tiled.";
            public static readonly string UnreadableTexture =
                "SoftMask with Raycast Threshold greater than zero can't be used with a CPU-unreadable " +
                "texture. You can make the texture readable in the Texture Import Settings.";
        }

        public void OnEnable() {
            _source = serializedObject.FindProperty("_source");
            _separateMask = serializedObject.FindProperty("_separateMask");
            _sprite = serializedObject.FindProperty("_sprite");
            _spriteBorderMode = serializedObject.FindProperty("_spriteBorderMode");
            _texture = serializedObject.FindProperty("_texture");
            _textureUVRect = serializedObject.FindProperty("_textureUVRect");
            _channelWeights = serializedObject.FindProperty("_channelWeights");
            _raycastThreshold = serializedObject.FindProperty("_raycastThreshold");
            _invertMask = serializedObject.FindProperty("_invertMask");
            _invertOutsides = serializedObject.FindProperty("_invertOutsides");
            Assert.IsNotNull(_source);
            Assert.IsNotNull(_separateMask);
            Assert.IsNotNull(_sprite);
            Assert.IsNotNull(_spriteBorderMode);
            Assert.IsNotNull(_texture);
            Assert.IsNotNull(_textureUVRect);
            Assert.IsNotNull(_channelWeights);
            Assert.IsNotNull(_raycastThreshold);
            Assert.IsNotNull(_invertMask);
            Assert.IsNotNull(_invertOutsides);
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();
            EditorGUILayout.PropertyField(_source, Labels.Source);
            CustomEditors.WithIndent(() => {
                switch ((SoftMask.MaskSource)_source.enumValueIndex) {
                    case SoftMask.MaskSource.Graphic:
                        break;
                    case SoftMask.MaskSource.Sprite:
                        EditorGUILayout.PropertyField(_sprite, Labels.Sprite);
                        EditorGUILayout.PropertyField(_spriteBorderMode, Labels.SpriteBorderMode);
                        break;
                    case SoftMask.MaskSource.Texture:
                        EditorGUILayout.PropertyField(_texture, Labels.Texture);
                        EditorGUILayout.PropertyField(_textureUVRect, Labels.TextureUVRect);
                        break;
                }
            });
            EditorGUILayout.PropertyField(_separateMask, Labels.SeparateMask);
            EditorGUILayout.Slider(_raycastThreshold, 0, 1, Labels.RaycastThreshold);
            EditorGUILayout.PropertyField(_invertMask, Labels.InvertMask);
            EditorGUILayout.PropertyField(_invertOutsides, Labels.InvertOutsides);
            CustomEditors.ChannelWeights(Labels.MaskChannel, _channelWeights, ref _customWeightsExpanded);
            ShowErrorsIfAny();
            serializedObject.ApplyModifiedProperties();
        }

        void ShowErrorsIfAny() {
            var errors = CollectErrors();
            ShowErrorIfPresent(errors, SoftMask.Errors.UnsupportedShaders,   Labels.UnsupportedShaders,   MessageType.Warning);
            ShowErrorIfPresent(errors, SoftMask.Errors.NestedMasks,          Labels.NestedMasks,          MessageType.Warning);
            ShowErrorIfPresent(errors, SoftMask.Errors.TightPackedSprite,    Labels.TightPackedSprite,    MessageType.Error);
            ShowErrorIfPresent(errors, SoftMask.Errors.AlphaSplitSprite,     Labels.AlphaSplitSprite,     MessageType.Error);
            ShowErrorIfPresent(errors, SoftMask.Errors.UnsupportedImageType, Labels.UnsupportedImageType, MessageType.Error);
            ShowErrorIfPresent(errors, SoftMask.Errors.UnreadableTexture,    Labels.UnreadableTexture,    MessageType.Error);
        }

        SoftMask.Errors CollectErrors() {
            var result = SoftMask.Errors.NoError;
            foreach (var t in targets)
                result |= ((SoftMask)t).PollErrors();
            return result;
        }

        static void ShowErrorIfPresent(
                SoftMask.Errors actualErrors,
                SoftMask.Errors expectedError,
                string errorMessage,
                MessageType errorMessageType) {
            if ((actualErrors & expectedError) != 0)
                EditorGUILayout.HelpBox(errorMessage, errorMessageType);
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
                try {
                    f();
                } finally {
                    --EditorGUI.indentLevel;
                }
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
                if (Event.current.type != EventType.Layout)
                    customWeightsExpanded = knownChannel == KnownMaskChannel.Custom;
                EditorGUI.EndProperty();
            }

            static Color ColorField(Rect rect, GUIContent label, Color color) {
                rect = EditorGUI.PrefixLabel(rect, label);
                color.r = ColorComponentField(HorizontalSlice(rect, 0, 4, 2), Labels.R, color.r);
                color.g = ColorComponentField(HorizontalSlice(rect, 1, 4, 2), Labels.G, color.g);
                color.b = ColorComponentField(HorizontalSlice(rect, 2, 4, 2), Labels.B, color.b);
                color.a = ColorComponentField(HorizontalSlice(rect, 3, 4, 2), Labels.A, color.a);
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

            static Rect HorizontalSlice(Rect whole, int part, int partCount, int spacing) {
                var result = new Rect(whole);
                result.width -= (partCount - 1) * spacing;
                result.width /= partCount;
                result.x += part * (result.width + spacing);
                return result;
            }

            static T WithZeroIndent<T>(Func<T> f) {
                var prev = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                try {
                    return f();
                } finally {
                    EditorGUI.indentLevel = prev;
                }
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
 