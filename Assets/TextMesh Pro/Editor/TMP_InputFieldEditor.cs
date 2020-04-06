// Copyright (C) 2014 - 2016 Stephan Bouchard - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms


using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.UI;
using UnityEditor.AnimatedValues;


namespace TMPro.EditorUtilities
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(TMP_InputField), true)]
    public class TMP_InputFieldEditor : SelectableEditor
    {
        private struct m_foldout
        { // Track Inspector foldout panel states, globally.
            public static bool textInput = true;
            public static bool fontSettings = true;
            public static bool extraSettings = true;
            //public static bool shadowSetting = false;
            //public static bool materialEditor = true;
        }

        private static string[] uiStateLabel = new string[] { "\t- <i>Click to expand</i> -", "\t- <i>Click to collapse</i> -" };
        //private GUIStyle toggleStyle;

        SerializedProperty m_TextViewport;
        SerializedProperty m_TextComponent;
        SerializedProperty m_Text;
        SerializedProperty m_ContentType;
        SerializedProperty m_LineType;
        SerializedProperty m_InputType;
        SerializedProperty m_CharacterValidation;
        SerializedProperty m_InputValidator;
        SerializedProperty m_RegexValue;
        SerializedProperty m_KeyboardType;
        SerializedProperty m_CharacterLimit;
        SerializedProperty m_CaretBlinkRate;
        SerializedProperty m_CaretWidth;
        SerializedProperty m_CaretColor;
        SerializedProperty m_CustomCaretColor;
        SerializedProperty m_SelectionColor;
        SerializedProperty m_HideMobileInput;
        SerializedProperty m_Placeholder;
        SerializedProperty m_VerticalScrollbar;
        SerializedProperty m_ScrollbarScrollSensitivity;
        SerializedProperty m_OnValueChanged;
        SerializedProperty m_OnEndEdit;
        SerializedProperty m_OnSelect;
        SerializedProperty m_OnDeselect;
        SerializedProperty m_ReadOnly;
        SerializedProperty m_RichText;
        SerializedProperty m_RichTextEditingAllowed;
        SerializedProperty m_ResetOnDeActivation;
        SerializedProperty m_RestoreOriginalTextOnEscape;

        SerializedProperty m_OnFocusSelectAll;
        SerializedProperty m_GlobalPointSize;
        SerializedProperty m_GlobalFontAsset;

        AnimBool m_CustomColor;

        TMP_InputValidator m_ValidationScript;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_TextViewport = serializedObject.FindProperty("m_TextViewport");
            m_TextComponent = serializedObject.FindProperty("m_TextComponent");
            m_Text = serializedObject.FindProperty("m_Text");
            m_ContentType = serializedObject.FindProperty("m_ContentType");
            m_LineType = serializedObject.FindProperty("m_LineType");
            m_InputType = serializedObject.FindProperty("m_InputType");
            m_CharacterValidation = serializedObject.FindProperty("m_CharacterValidation");
            m_InputValidator = serializedObject.FindProperty("m_InputValidator");
            m_RegexValue = serializedObject.FindProperty("m_RegexValue");
            m_KeyboardType = serializedObject.FindProperty("m_KeyboardType");
            m_CharacterLimit = serializedObject.FindProperty("m_CharacterLimit");
            m_CaretBlinkRate = serializedObject.FindProperty("m_CaretBlinkRate");
            m_CaretWidth = serializedObject.FindProperty("m_CaretWidth");
            m_CaretColor = serializedObject.FindProperty("m_CaretColor");
            m_CustomCaretColor = serializedObject.FindProperty("m_CustomCaretColor");
            m_SelectionColor = serializedObject.FindProperty("m_SelectionColor");
            m_HideMobileInput = serializedObject.FindProperty("m_HideMobileInput");

            m_Placeholder = serializedObject.FindProperty("m_Placeholder");
            m_VerticalScrollbar = serializedObject.FindProperty("m_VerticalScrollbar");
            m_ScrollbarScrollSensitivity = serializedObject.FindProperty("m_ScrollSensitivity");

            m_OnValueChanged = serializedObject.FindProperty("m_OnValueChanged");
            m_OnEndEdit = serializedObject.FindProperty("m_OnEndEdit");
            m_OnSelect = serializedObject.FindProperty("m_OnSelect");
            m_OnDeselect = serializedObject.FindProperty("m_OnDeselect");
            m_ReadOnly = serializedObject.FindProperty("m_ReadOnly");
            m_RichText = serializedObject.FindProperty("m_RichText");
            m_RichTextEditingAllowed = serializedObject.FindProperty("m_isRichTextEditingAllowed");
            m_ResetOnDeActivation = serializedObject.FindProperty("m_ResetOnDeActivation");
            m_RestoreOriginalTextOnEscape = serializedObject.FindProperty("m_RestoreOriginalTextOnEscape");

            m_OnFocusSelectAll = serializedObject.FindProperty("m_OnFocusSelectAll");
            m_GlobalPointSize = serializedObject.FindProperty("m_GlobalPointSize");
            m_GlobalFontAsset = serializedObject.FindProperty("m_GlobalFontAsset");

            m_CustomColor = new AnimBool(m_CustomCaretColor.boolValue);
            m_CustomColor.valueChanged.AddListener(Repaint);

            // Get the UI Skin and Styles for the various Editors
            TMP_UIStyleManager.GetUIStyles();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            m_CustomColor.valueChanged.RemoveListener(Repaint);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            base.OnInspectorGUI();

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_TextViewport);

            EditorGUILayout.PropertyField(m_TextComponent);

            TextMeshProUGUI text = null;
            if (m_TextComponent != null && m_TextComponent.objectReferenceValue != null)
            {
                text = m_TextComponent.objectReferenceValue as TextMeshProUGUI;
                //if (text.supportRichText)
                //{
                //    EditorGUILayout.HelpBox("Using Rich Text with input is unsupported.", MessageType.Warning);
                //}
            }

            EditorGUI.BeginDisabledGroup(m_TextComponent == null || m_TextComponent.objectReferenceValue == null);

            // TEXT INPUT BOX
            Rect rect = EditorGUILayout.GetControlRect(false, 25);
            EditorGUIUtility.labelWidth = 130f;
            //EditorGUIUtility.fieldWidth;

            rect.y += 2;
            GUI.Label(rect, "<b>TEXT INPUT BOX</b>" + (m_foldout.textInput ? uiStateLabel[1] : uiStateLabel[0]), TMP_UIStyleManager.Section_Label);
            if (GUI.Button(new Rect(rect.x, rect.y, rect.width - 150, rect.height), GUIContent.none, GUI.skin.label))
                m_foldout.textInput = !m_foldout.textInput;

            // Toggle showing Rich Tags
            //GUI.Label(new Rect(rect.width - 125, rect.y + 4, 125, 24), "<i>Enable RTL Editor</i>", toggleStyle);

            if (m_foldout.textInput)
            {
                EditorGUI.BeginChangeCheck();
                m_Text.stringValue = EditorGUILayout.TextArea(m_Text.stringValue, TMP_UIStyleManager.TextAreaBoxEditor, GUILayout.Height(125), GUILayout.ExpandWidth(true));
            }


            // INPUT FIELD SETTINGS
            #region INPUT FIELD SETTINGS
            if (GUILayout.Button("<b>INPUT FIELD SETTINGS</b>" + (m_foldout.fontSettings ? uiStateLabel[1] : uiStateLabel[0]), TMP_UIStyleManager.Section_Label))
                m_foldout.fontSettings = !m_foldout.fontSettings;

            if (m_foldout.fontSettings)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_GlobalFontAsset, new GUIContent("Font Asset", "Set the Font Asset for both Placeholder and Input Field text object."));
                if (EditorGUI.EndChangeCheck())
                {
                    TMP_InputField inputField = target as TMP_InputField;
                    inputField.SetGlobalFontAsset(m_GlobalFontAsset.objectReferenceValue as TMP_FontAsset);
                }


                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_GlobalPointSize, new GUIContent("Point Size", "Set the point size of both Placeholder and Input Field text object."));
                if (EditorGUI.EndChangeCheck())
                {
                    TMP_InputField inputField = target as TMP_InputField;
                    inputField.SetGlobalPointSize(m_GlobalPointSize.floatValue);
                }

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_CharacterLimit);

                EditorGUILayout.Space();

                EditorGUILayout.PropertyField(m_ContentType);
                if (!m_ContentType.hasMultipleDifferentValues)
                {
                    EditorGUI.indentLevel++;

                    if (m_ContentType.enumValueIndex == (int)TMP_InputField.ContentType.Standard ||
                        m_ContentType.enumValueIndex == (int)TMP_InputField.ContentType.Autocorrected ||
                        m_ContentType.enumValueIndex == (int)TMP_InputField.ContentType.Custom)
                    {
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(m_LineType);
                        if (EditorGUI.EndChangeCheck())
                        {
                            if (text != null)
                            {
                                if (m_LineType.enumValueIndex == (int)TMP_InputField.LineType.SingleLine)
                                    text.enableWordWrapping = false;
                                else
                                    text.enableWordWrapping = true;
                            }
                        }
                    }

                    if (m_ContentType.enumValueIndex == (int)TMP_InputField.ContentType.Custom)
                    {
                        EditorGUILayout.PropertyField(m_InputType);
                        EditorGUILayout.PropertyField(m_KeyboardType);
                        EditorGUILayout.PropertyField(m_CharacterValidation);
                        if (m_CharacterValidation.enumValueIndex == (int)TMP_InputField.CharacterValidation.Regex)
                        {
                            EditorGUILayout.PropertyField(m_RegexValue);
                        }
                        else if (m_CharacterValidation.enumValueIndex == (int)TMP_InputField.CharacterValidation.CustomValidator)
                        {
                            EditorGUILayout.PropertyField(m_InputValidator);
                        }
                    }

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space();

                EditorGUILayout.PropertyField(m_Placeholder);
                EditorGUILayout.PropertyField(m_VerticalScrollbar);

                if (m_VerticalScrollbar.objectReferenceValue != null)
                    EditorGUILayout.PropertyField(m_ScrollbarScrollSensitivity);

                EditorGUILayout.PropertyField(m_CaretBlinkRate);
                EditorGUILayout.PropertyField(m_CaretWidth);

                EditorGUILayout.PropertyField(m_CustomCaretColor);

                m_CustomColor.target = m_CustomCaretColor.boolValue;

                if (EditorGUILayout.BeginFadeGroup(m_CustomColor.faded))
                {
                    EditorGUILayout.PropertyField(m_CaretColor);
                }
                EditorGUILayout.EndFadeGroup();

                EditorGUILayout.PropertyField(m_SelectionColor);
            }
            #endregion


            // CONTROL SETTINGS
            #region CONTROL SETTINGS
            if (GUILayout.Button("<b>CONTROL SETTINGS</b>" + (m_foldout.extraSettings ? uiStateLabel[1] : uiStateLabel[0]), TMP_UIStyleManager.Section_Label))
                m_foldout.extraSettings = !m_foldout.extraSettings;

            if (m_foldout.extraSettings)
            {
                EditorGUILayout.PropertyField(m_OnFocusSelectAll, new GUIContent("OnFocus - Select All", "Should all the text be selected when the Input Field is selected."));
                EditorGUILayout.PropertyField(m_ResetOnDeActivation, new GUIContent("Reset On DeActivation", "Should the Text and Caret position be reset when Input Field is DeActivated."));
                EditorGUILayout.PropertyField(m_RestoreOriginalTextOnEscape, new GUIContent("Restore On ESC Key", "Should the original text be restored when pressing ESC."));
                EditorGUILayout.PropertyField(m_HideMobileInput);
                EditorGUILayout.PropertyField(m_ReadOnly);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(m_RichText);
                EditorGUIUtility.labelWidth = 140f;
                EditorGUILayout.PropertyField(m_RichTextEditingAllowed, new GUIContent("Allow Rich Text Editing"));
                EditorGUIUtility.labelWidth = 130f;
                EditorGUILayout.EndHorizontal();
            }
            #endregion


            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_OnValueChanged);
            EditorGUILayout.PropertyField(m_OnEndEdit);
            EditorGUILayout.PropertyField(m_OnSelect);
            EditorGUILayout.PropertyField(m_OnDeselect);

            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
