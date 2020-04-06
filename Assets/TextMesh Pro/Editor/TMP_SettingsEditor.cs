// Copyright (C) 2014 - 2016 Stephan Bouchard - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms


using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections;

#pragma warning disable 0414 // Disabled a few warnings for not yet implemented features.

namespace TMPro.EditorUtilities
{

    [CustomEditor(typeof(TMP_Settings))]
    public class TMP_SettingsEditor : Editor
    {
        //private struct UI_PanelState
        //{

        //}

        //private string[] uiStateLabel = new string[] { "<i>(Click to expand)</i>", "<i>(Click to collapse)</i>" };
        //private GUIStyle _Label;


        private SerializedProperty prop_FontAsset;
        private SerializedProperty prop_DefaultFontAssetPath;
        private SerializedProperty prop_DefaultFontSize;
        private SerializedProperty prop_DefaultAutoSizeMinRatio;
        private SerializedProperty prop_DefaultAutoSizeMaxRatio;
        private SerializedProperty prop_DefaultTextMeshProTextContainerSize;
        private SerializedProperty prop_DefaultTextMeshProUITextContainerSize;
        private SerializedProperty prop_AutoSizeTextContainer;

        private SerializedProperty prop_SpriteAsset;
        private SerializedProperty prop_SpriteAssetPath;
        private SerializedProperty prop_EnableEmojiSupport;
        private SerializedProperty prop_StyleSheet;
        private ReorderableList m_list;

        private SerializedProperty prop_matchMaterialPreset;

        private SerializedProperty prop_WordWrapping;
        private SerializedProperty prop_Kerning;
        private SerializedProperty prop_ExtraPadding;
        private SerializedProperty prop_TintAllSprites;
        private SerializedProperty prop_ParseEscapeCharacters;
        private SerializedProperty prop_MissingGlyphCharacter;

        private SerializedProperty prop_WarningsDisabled;

        private SerializedProperty prop_LeadingCharacters;
        private SerializedProperty prop_FollowingCharacters;



        public void OnEnable()
        {
            prop_FontAsset = serializedObject.FindProperty("m_defaultFontAsset");
            prop_DefaultFontAssetPath = serializedObject.FindProperty("m_defaultFontAssetPath");
            prop_DefaultFontSize = serializedObject.FindProperty("m_defaultFontSize");
            prop_DefaultAutoSizeMinRatio = serializedObject.FindProperty("m_defaultAutoSizeMinRatio");
            prop_DefaultAutoSizeMaxRatio = serializedObject.FindProperty("m_defaultAutoSizeMaxRatio");
            prop_DefaultTextMeshProTextContainerSize = serializedObject.FindProperty("m_defaultTextMeshProTextContainerSize");
            prop_DefaultTextMeshProUITextContainerSize = serializedObject.FindProperty("m_defaultTextMeshProUITextContainerSize");
            prop_AutoSizeTextContainer = serializedObject.FindProperty("m_autoSizeTextContainer");

            prop_SpriteAsset = serializedObject.FindProperty("m_defaultSpriteAsset");
            prop_SpriteAssetPath = serializedObject.FindProperty("m_defaultSpriteAssetPath");
            prop_EnableEmojiSupport = serializedObject.FindProperty("m_enableEmojiSupport");
            prop_StyleSheet = serializedObject.FindProperty("m_defaultStyleSheet");

            m_list = new ReorderableList(serializedObject, serializedObject.FindProperty("m_fallbackFontAssets"), true, true, true, true);

            m_list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = m_list.serializedProperty.GetArrayElementAtIndex(index);
                rect.y += 2;
                EditorGUI.PropertyField( new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), element, GUIContent.none);
            };

            m_list.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "<b>Fallback Font Asset List</b>", TMP_UIStyleManager.Label);
            };

            prop_matchMaterialPreset = serializedObject.FindProperty("m_matchMaterialPreset");

            prop_WordWrapping = serializedObject.FindProperty("m_enableWordWrapping");
            prop_Kerning = serializedObject.FindProperty("m_enableKerning");
            prop_ExtraPadding = serializedObject.FindProperty("m_enableExtraPadding");
            prop_TintAllSprites = serializedObject.FindProperty("m_enableTintAllSprites");
            prop_ParseEscapeCharacters = serializedObject.FindProperty("m_enableParseEscapeCharacters");
            prop_MissingGlyphCharacter = serializedObject.FindProperty("m_missingGlyphCharacter");

            prop_WarningsDisabled = serializedObject.FindProperty("m_warningsDisabled");

            prop_LeadingCharacters = serializedObject.FindProperty("m_leadingCharacters");
            prop_FollowingCharacters = serializedObject.FindProperty("m_followingCharacters");

            // Get the UI Skin and Styles for the various Editors
            TMP_UIStyleManager.GetUIStyles();
        }

        public override void OnInspectorGUI()
        {
            //Event evt = Event.current;

            serializedObject.Update();

            GUILayout.Label("<b>TEXTMESH PRO - SETTINGS</b>", TMP_UIStyleManager.Section_Label);

            // TextMeshPro Font Info Panel
            EditorGUI.indentLevel = 0;

            //GUI.enabled = false; // Lock UI

            EditorGUIUtility.labelWidth = 135;
            //EditorGUIUtility.fieldWidth = 80;

            // FONT ASSET
            EditorGUILayout.BeginVertical(TMP_UIStyleManager.SquareAreaBox85G);
            GUILayout.Label("<b>Default Font Asset</b>", TMP_UIStyleManager.Label);
            GUILayout.Label("Select the Font Asset that will be assigned by default to newly created text objects when no Font Asset is specified.", TMP_UIStyleManager.Label);
            GUILayout.Space(5f);
            EditorGUILayout.PropertyField(prop_FontAsset);
            GUILayout.Space(10f);
            GUILayout.Label("The relative path to a Resources folder where the Font Assets and Material Presets are located.\nExample \"Fonts & Materials/\"", TMP_UIStyleManager.Label);
            EditorGUILayout.PropertyField(prop_DefaultFontAssetPath, new GUIContent("Path:        Resources/"));
            EditorGUILayout.EndVertical();


            // FALLBACK FONT ASSETs
            EditorGUILayout.BeginVertical(TMP_UIStyleManager.SquareAreaBox85G);
            GUILayout.Label("<b>Fallback Font Assets</b>", TMP_UIStyleManager.Label);
            GUILayout.Label("Select the Font Assets that will be searched to locate and replace missing characters from a given Font Asset.", TMP_UIStyleManager.Label);
            GUILayout.Space(5f);
            m_list.DoLayoutList();
            GUILayout.Label("<b>Fallback Material Settings</b>", TMP_UIStyleManager.Label);
            EditorGUILayout.PropertyField(prop_matchMaterialPreset, new GUIContent("Match Material Presets"));
            EditorGUILayout.EndVertical();


            // TEXT OBJECT DEFAULT PROPERTIES
            EditorGUILayout.BeginVertical(TMP_UIStyleManager.SquareAreaBox85G);
            GUILayout.Label("<b>New Text Object Default Settings</b>", TMP_UIStyleManager.Label);
            GUILayout.Label("Default settings used by all new text objects.", TMP_UIStyleManager.Label);
            GUILayout.Space(10f);
            EditorGUI.BeginChangeCheck();

            GUILayout.Label("<b>Text Container Default Settings</b>", TMP_UIStyleManager.Label);

            EditorGUIUtility.labelWidth = 150;
            EditorGUILayout.PropertyField(prop_DefaultTextMeshProTextContainerSize, new GUIContent("TextMeshPro")); //, GUILayout.MinWidth(180), GUILayout.MaxWidth(200));
            EditorGUILayout.PropertyField(prop_DefaultTextMeshProUITextContainerSize, new GUIContent("TextMeshPro UI")); //, GUILayout.MinWidth(80), GUILayout.MaxWidth(100));
            EditorGUILayout.PropertyField(prop_AutoSizeTextContainer, new GUIContent("Auto Size Text Container", "Set the size of the text container to match the text."));

            GUILayout.Space(10f);
            GUILayout.Label("<b>Text Component Default Settings</b>", TMP_UIStyleManager.Label);
            EditorGUIUtility.labelWidth = 150;
            EditorGUILayout.PropertyField(prop_DefaultFontSize, new GUIContent("Default Font Size"), GUILayout.MinWidth(200), GUILayout.MaxWidth(200));

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.PrefixLabel(new GUIContent("Text Auto Size Ratios"));
                EditorGUIUtility.labelWidth = 35;
                EditorGUILayout.PropertyField(prop_DefaultAutoSizeMinRatio, new GUIContent("Min:"));
                EditorGUILayout.PropertyField(prop_DefaultAutoSizeMaxRatio, new GUIContent("Max:"));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUIUtility.labelWidth = 150;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(prop_WordWrapping); //, GUILayout.MaxWidth(200));
            EditorGUILayout.PropertyField(prop_Kerning);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(prop_ExtraPadding); //, GUILayout.MaxWidth(200));
            EditorGUILayout.PropertyField(prop_TintAllSprites);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(prop_ParseEscapeCharacters, new GUIContent("Parse Escape Sequence")); //, GUILayout.MaxWidth(200));
            EditorGUIUtility.fieldWidth = 10;
            EditorGUILayout.PropertyField(prop_MissingGlyphCharacter, new GUIContent("Missing Glyph Repl."));
            EditorGUILayout.EndHorizontal();

            EditorGUIUtility.labelWidth = 135;
            GUILayout.Space(10f);
            GUILayout.Label("<b>Disable warnings for missing glyphs on text objects.</b>", TMP_UIStyleManager.Label);
            EditorGUILayout.PropertyField(prop_WarningsDisabled, new GUIContent("Disable warnings"));
            EditorGUILayout.EndVertical();


            // SPRITE ASSET
            EditorGUILayout.BeginVertical(TMP_UIStyleManager.SquareAreaBox85G);
            GUILayout.Label("<b>Default Sprite Asset</b>", TMP_UIStyleManager.Label);
            //GUI.color = Color.yellow;
            GUILayout.Label("Select the Sprite Asset that will be assigned by default when using the <sprite> tag when no Sprite Asset is specified.", TMP_UIStyleManager.Label);
            GUILayout.Space(5f);
            //GUI.color = Color.white;
            EditorGUILayout.PropertyField(prop_SpriteAsset);
            GUILayout.Space(10f);
            //GUILayout.Label("Enable Emoji Support", TMP_UIStyleManager.Label);
            EditorGUILayout.PropertyField(prop_EnableEmojiSupport, new GUIContent("Enable Emoji Support", "Enables Emoji support for Touch Screen Keyboards on target devices."));
            GUILayout.Space(10f);
            GUILayout.Label("The relative path to a Resources folder where the Sprite Assets are located.\nExample \"Sprite Assets/\"", TMP_UIStyleManager.Label);
            EditorGUILayout.PropertyField(prop_SpriteAssetPath, new GUIContent("Path:        Resources/"));
            EditorGUILayout.EndVertical();


            // STYLE SHEET
            EditorGUILayout.BeginVertical(TMP_UIStyleManager.SquareAreaBox85G);
            GUILayout.Label("<b>Default Style Sheet</b>", TMP_UIStyleManager.Label);
            GUILayout.Label("Select the Style Sheet that will be used for all text objects in this project.", TMP_UIStyleManager.Label);
            GUILayout.Space(5f);
            EditorGUILayout.PropertyField(prop_StyleSheet);
            EditorGUILayout.EndVertical();


            // LINE BREAKING RULE
            EditorGUILayout.BeginVertical(TMP_UIStyleManager.SquareAreaBox85G);
            GUILayout.Label("<b>Line Breaking Resources for Asian languages</b>", TMP_UIStyleManager.Label);
            GUILayout.Label("Select the text assets that contain the Leading and Following characters which define the rules for line breaking with Asian languages.", TMP_UIStyleManager.Label);
            GUILayout.Space(5f);
            EditorGUILayout.PropertyField(prop_LeadingCharacters);
            EditorGUILayout.PropertyField(prop_FollowingCharacters);
            EditorGUILayout.EndVertical();


            if (serializedObject.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(target);
                TMPro_EventManager.ON_TMP_SETTINGS_CHANGED();
            }

        }
    }
}