// Copyright (C) 2014 - 2016 Stephan Bouchard - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms


using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections;
using System.Collections.Generic;



namespace TMPro.EditorUtilities
{

    [CustomPropertyDrawer(typeof(TMP_FontWeights))]
    public class FontWeightDrawer : PropertyDrawer
    {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty prop_regular = property.FindPropertyRelative("regularTypeface");
            SerializedProperty prop_italic = property.FindPropertyRelative("italicTypeface");

            float width = position.width;

            position.width = 125;
            EditorGUI.LabelField(position, label);

            // NORMAL FACETYPE
            if (label.text[0] == '4') GUI.enabled = false;
            position.x = 140; position.width = (width - 140) / 2;
            EditorGUI.PropertyField(position, prop_regular, GUIContent.none);

            // ITALIC FACETYPE
            GUI.enabled = true;
            position.x += position.width + 17;
            EditorGUI.PropertyField(position, prop_italic, GUIContent.none);
        }
    }



    [CustomEditor(typeof(TMP_FontAsset))]
    public class TMP_FontAssetEditor : Editor
    {
        private struct UI_PanelState
        {
            public static bool fontInfoPanel = true;
            public static bool fontWeightPanel = true;
            public static bool fallbackFontAssetPanel = true;
            public static bool glyphInfoPanel = false;
            public static bool kerningInfoPanel = false;
        }

        private struct Warning
        {
            public bool isEnabled;
            public double expirationTime;
        }

        private int m_GlyphPage = 0;
        private int m_KerningPage = 0;

        private int m_selectedElement = -1;

        private string m_dstGlyphID;
        private const string k_placeholderUnicodeHex = "<i>Unicode Hex ID</i>";
        private string m_unicodeHexLabel = k_placeholderUnicodeHex;

        private Warning m_AddGlyphWarning;


        private string m_searchPattern;
        private List<int> m_searchList;
        private bool m_isSearchDirty;

        private const string k_UndoRedo = "UndoRedoPerformed";

        private SerializedProperty font_atlas_prop;
        private SerializedProperty font_material_prop;

        private SerializedProperty fontWeights_prop;

        //private SerializedProperty fallbackFontAssets_prop;
        private ReorderableList m_list;

        private SerializedProperty font_normalStyle_prop;
        private SerializedProperty font_normalSpacing_prop;

        private SerializedProperty font_boldStyle_prop;
        private SerializedProperty font_boldSpacing_prop;

        private SerializedProperty font_italicStyle_prop;
        private SerializedProperty font_tabSize_prop;

        private SerializedProperty m_fontInfo_prop;
        private SerializedProperty m_glyphInfoList_prop;

        private SerializedProperty m_kerningInfo_prop;
        private KerningTable m_kerningTable;

        private SerializedProperty m_kerningPair_prop;

        private TMP_FontAsset m_fontAsset;

        private Material[] m_materialPresets;

        private bool isAssetDirty = false;

        private int errorCode;

        private System.DateTime timeStamp;

        private string[] uiStateLabel = new string[] { "<i>(Click to expand)</i>", "<i>(Click to collapse)</i>" };

        public void OnEnable()
        {
            font_atlas_prop = serializedObject.FindProperty("atlas");
            font_material_prop = serializedObject.FindProperty("material");

            fontWeights_prop = serializedObject.FindProperty("fontWeights");

            m_list = new ReorderableList(serializedObject, serializedObject.FindProperty("fallbackFontAssets"), true, true, true, true);

            m_list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = m_list.serializedProperty.GetArrayElementAtIndex(index);
                rect.y += 2;
                EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), element, GUIContent.none);
            };

            m_list.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "<b>Fallback Font Asset List</b>", TMP_UIStyleManager.Label);
            };

            font_normalStyle_prop = serializedObject.FindProperty("normalStyle");
            font_normalSpacing_prop = serializedObject.FindProperty("normalSpacingOffset");

            font_boldStyle_prop = serializedObject.FindProperty("boldStyle");
            font_boldSpacing_prop = serializedObject.FindProperty("boldSpacing");

            font_italicStyle_prop = serializedObject.FindProperty("italicStyle");
            font_tabSize_prop = serializedObject.FindProperty("tabSize");

            m_fontInfo_prop = serializedObject.FindProperty("m_fontInfo");
            m_glyphInfoList_prop = serializedObject.FindProperty("m_glyphInfoList");
            m_kerningInfo_prop = serializedObject.FindProperty("m_kerningInfo");
            m_kerningPair_prop = serializedObject.FindProperty("m_kerningPair");

            m_fontAsset = target as TMP_FontAsset;
            m_kerningTable = m_fontAsset.kerningInfo;

            m_materialPresets = TMP_EditorUtility.FindMaterialReferences(m_fontAsset);

            // Get the UI Skin and Styles for the various Editors
            TMP_UIStyleManager.GetUIStyles();

            m_searchList = new List<int>();
        }


        public override void OnInspectorGUI()
        {
            // Check Warnings


            //Debug.Log("OnInspectorGUI Called.");
            Event currentEvent = Event.current;

            serializedObject.Update();

            GUILayout.Label("<b>TextMesh Pro! Font Asset</b>", TMP_UIStyleManager.Section_Label);

            // TextMeshPro Font Info Panel
            GUILayout.Label("Face Info", TMP_UIStyleManager.Section_Label);
            EditorGUI.indentLevel = 1;

            GUI.enabled = false; // Lock UI

            float labelWidth = EditorGUIUtility.labelWidth = 150f;
            float fieldWidth = EditorGUIUtility.fieldWidth;

            EditorGUILayout.PropertyField(m_fontInfo_prop.FindPropertyRelative("Name"), new GUIContent("Font Source"));
            EditorGUILayout.PropertyField(m_fontInfo_prop.FindPropertyRelative("PointSize"));

            GUI.enabled = true;
            EditorGUILayout.PropertyField(m_fontInfo_prop.FindPropertyRelative("Scale"));
            EditorGUILayout.PropertyField(m_fontInfo_prop.FindPropertyRelative("LineHeight"));

            EditorGUILayout.PropertyField(m_fontInfo_prop.FindPropertyRelative("Ascender"));
            EditorGUILayout.PropertyField(m_fontInfo_prop.FindPropertyRelative("CapHeight"));
            EditorGUILayout.PropertyField(m_fontInfo_prop.FindPropertyRelative("Baseline"));
            EditorGUILayout.PropertyField(m_fontInfo_prop.FindPropertyRelative("Descender"));
            EditorGUILayout.PropertyField(m_fontInfo_prop.FindPropertyRelative("Underline"), new GUIContent("Underline Offset"));
            EditorGUILayout.PropertyField(m_fontInfo_prop.FindPropertyRelative("strikethrough"), new GUIContent("Strikethrough Offset"));
            EditorGUILayout.PropertyField(m_fontInfo_prop.FindPropertyRelative("SuperscriptOffset"));
            EditorGUILayout.PropertyField(m_fontInfo_prop.FindPropertyRelative("SubscriptOffset"));
            
            SerializedProperty subSize_prop = m_fontInfo_prop.FindPropertyRelative("SubSize");
            EditorGUILayout.PropertyField(subSize_prop, new GUIContent("Super / Subscript Size"));
            subSize_prop.floatValue = Mathf.Clamp(subSize_prop.floatValue, 0.25f, 1f);
            

            GUI.enabled = false;
            //EditorGUILayout.PropertyField(m_fontInfo_prop.FindPropertyRelative("Padding"));

            //GUILayout.Label("Atlas Size");
            EditorGUI.indentLevel = 1;
            GUILayout.Space(18);
            EditorGUILayout.PropertyField(m_fontInfo_prop.FindPropertyRelative("Padding"));
            EditorGUILayout.PropertyField(m_fontInfo_prop.FindPropertyRelative("AtlasWidth"), new GUIContent("Width"));
            EditorGUILayout.PropertyField(m_fontInfo_prop.FindPropertyRelative("AtlasHeight"), new GUIContent("Height"));

            GUI.enabled = true;
            EditorGUI.indentLevel = 0;
            GUILayout.Space(20);
            GUILayout.Label("Font Sub-Assets", TMP_UIStyleManager.Section_Label);
                  
            GUI.enabled = false;
            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(font_atlas_prop, new GUIContent("Font Atlas:"));
            EditorGUILayout.PropertyField(font_material_prop, new GUIContent("Font Material:"));

            GUI.enabled = true;

            string evt_cmd = Event.current.commandName; // Get Current Event CommandName to check for Undo Events

            // FONT SETTINGS
            EditorGUI.indentLevel = 0;
            if (GUILayout.Button("Font Weights\t" + (UI_PanelState.fontWeightPanel ? uiStateLabel[1] : uiStateLabel[0]), TMP_UIStyleManager.Section_Label))
                UI_PanelState.fontWeightPanel = !UI_PanelState.fontWeightPanel;


            if (UI_PanelState.fontWeightPanel)
            {
                EditorGUIUtility.labelWidth = 120;
                EditorGUILayout.BeginVertical(TMP_UIStyleManager.SquareAreaBox85G);
                EditorGUI.indentLevel = 0;
                GUILayout.Label("Select the Font Assets that will be used for the following font weights.", TMP_UIStyleManager.Label);
                GUILayout.Space(10f);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("<b>Font Weight</b>", TMP_UIStyleManager.Label, GUILayout.Width(117));
                GUILayout.Label("<b>Normal Style</b>", TMP_UIStyleManager.Label);
                GUILayout.Label("<b>Italic Style</b>", TMP_UIStyleManager.Label);
                EditorGUILayout.EndHorizontal();

                //EditorGUILayout.PropertyField(fontWeights_prop.GetArrayElementAtIndex(1), new GUIContent("100 - Thin"));
                //EditorGUILayout.PropertyField(fontWeights_prop.GetArrayElementAtIndex(2), new GUIContent("200 - Extra-Light"));
                //EditorGUILayout.PropertyField(fontWeights_prop.GetArrayElementAtIndex(3), new GUIContent("300 - Light"));
                EditorGUILayout.PropertyField(fontWeights_prop.GetArrayElementAtIndex(4), new GUIContent("400 - Regular"));
                //EditorGUILayout.PropertyField(fontWeights_prop.GetArrayElementAtIndex(5), new GUIContent("500 - Medium"));
                //EditorGUILayout.PropertyField(fontWeights_prop.GetArrayElementAtIndex(6), new GUIContent("600 - Demi-Bold"));
                EditorGUILayout.PropertyField(fontWeights_prop.GetArrayElementAtIndex(7), new GUIContent("700 - Bold"));
                //EditorGUILayout.PropertyField(fontWeights_prop.GetArrayElementAtIndex(8), new GUIContent("800 - Heavy"));
                //EditorGUILayout.PropertyField(fontWeights_prop.GetArrayElementAtIndex(9), new GUIContent("900 - Black"));

                EditorGUILayout.EndVertical();

                //EditorGUI.indentLevel = 1;
                EditorGUIUtility.labelWidth = 120f;
                EditorGUILayout.BeginVertical(TMP_UIStyleManager.SquareAreaBox85G);
                GUILayout.Label("Settings used to simulate a typeface when no font asset is available.", TMP_UIStyleManager.Label);
                GUILayout.Space(5f);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(font_normalStyle_prop, new GUIContent("Normal Weight"));
                font_normalStyle_prop.floatValue = Mathf.Clamp(font_normalStyle_prop.floatValue, -3.0f, 3.0f);
                if (GUI.changed || evt_cmd == k_UndoRedo)
                {
                    GUI.changed = false;

                    // Modify the material property on matching material presets.
                    for (int i = 0; i < m_materialPresets.Length; i++)
                        m_materialPresets[i].SetFloat("_WeightNormal", font_normalStyle_prop.floatValue);
                }


                EditorGUILayout.PropertyField(font_boldStyle_prop, new GUIContent("Bold Weight"), GUILayout.MinWidth(100));
                font_boldStyle_prop.floatValue = Mathf.Clamp(font_boldStyle_prop.floatValue, -3.0f, 3.0f);
                if (GUI.changed || evt_cmd == k_UndoRedo)
                {
                    GUI.changed = false;

                    // Modify the material property on matching material presets.
                    for (int i = 0; i < m_materialPresets.Length; i++)
                        m_materialPresets[i].SetFloat("_WeightBold", font_boldStyle_prop.floatValue);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(font_normalSpacing_prop, new GUIContent("Spacing Offset"));
                font_normalSpacing_prop.floatValue = Mathf.Clamp(font_normalSpacing_prop.floatValue, -100, 100);
                if (GUI.changed || evt_cmd == k_UndoRedo)
                {
                    GUI.changed = false;
                }

                EditorGUILayout.PropertyField(font_boldSpacing_prop, new GUIContent("Bold Spacing"));
                font_boldSpacing_prop.floatValue = Mathf.Clamp(font_boldSpacing_prop.floatValue, 0, 100);
                if (GUI.changed || evt_cmd == k_UndoRedo)
                {
                    GUI.changed = false;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(font_italicStyle_prop, new GUIContent("Italic Style: "));
                font_italicStyle_prop.intValue = Mathf.Clamp(font_italicStyle_prop.intValue, 15, 60);

                EditorGUILayout.PropertyField(font_tabSize_prop, new GUIContent("Tab Multiple: "));

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            GUILayout.Space(5);

            // FALLBACK FONT ASSETS
            EditorGUI.indentLevel = 0;
            if (GUILayout.Button("Fallback Font Assets\t" + (UI_PanelState.fallbackFontAssetPanel ? uiStateLabel[1] : uiStateLabel[0]), TMP_UIStyleManager.Section_Label))
                UI_PanelState.fallbackFontAssetPanel = !UI_PanelState.fallbackFontAssetPanel;


            if (UI_PanelState.fallbackFontAssetPanel)
            {
                EditorGUIUtility.labelWidth = 120;
                EditorGUILayout.BeginVertical(TMP_UIStyleManager.SquareAreaBox85G);
                EditorGUI.indentLevel = 0;
                GUILayout.Label("Select the Font Assets that will be searched and used as fallback when characters are missing from this font asset.", TMP_UIStyleManager.Label);
                GUILayout.Space(10f);

                m_list.DoLayoutList();

                EditorGUILayout.EndVertical();
            }


            // GLYPH INFO TABLE
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUIUtility.fieldWidth = fieldWidth;
            GUILayout.Space(5);
            EditorGUI.indentLevel = 0;

            if (GUILayout.Button("Glyph Info\t" + (UI_PanelState.glyphInfoPanel ? uiStateLabel[1] : uiStateLabel[0]), TMP_UIStyleManager.Section_Label))
                UI_PanelState.glyphInfoPanel = !UI_PanelState.glyphInfoPanel;


            if (UI_PanelState.glyphInfoPanel)
            {
                int arraySize = m_glyphInfoList_prop.arraySize;
                int itemsPerPage = 15;

                // Display Glyph Management Tools
                EditorGUILayout.BeginVertical(TMP_UIStyleManager.Group_Label, GUILayout.ExpandWidth(true));
                {
                    // Search Bar implementation
                    #region DISPLAY SEARCH BAR
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUIUtility.labelWidth = 110f;
                        EditorGUI.BeginChangeCheck();
                        string searchPattern = EditorGUILayout.TextField("Glyph Search", m_searchPattern, "SearchTextField");
                        if (EditorGUI.EndChangeCheck() || m_isSearchDirty)
                        {
                            if (string.IsNullOrEmpty(searchPattern) == false)
                            {
                                //GUIUtility.keyboardControl = 0;
                                m_searchPattern = searchPattern; //.ToLower(System.Globalization.CultureInfo.InvariantCulture).Trim();
                                
                                // Search Glyph Table for potential matches
                                SearchGlyphTable(m_searchPattern, ref m_searchList);
                            }

                            m_isSearchDirty = false;
                        }

                        string styleName = string.IsNullOrEmpty(m_searchPattern) ? "SearchCancelButtonEmpty" : "SearchCancelButton";
                        if (GUILayout.Button(GUIContent.none, styleName))
                        {
                            GUIUtility.keyboardControl = 0;
                            m_searchPattern = string.Empty;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    #endregion

                    // Display Page Navigation
                    if (!string.IsNullOrEmpty(m_searchPattern))
                        arraySize = m_searchList.Count;

                    DisplayGlyphPageNavigation(arraySize, itemsPerPage);
                }
                EditorGUILayout.EndVertical();

                // Display Glyph Table Elements
                #region Glyph Table
                if (arraySize > 0)
                {
                    // Display each GlyphInfo entry using the GlyphInfo property drawer.
                    for (int i = itemsPerPage * m_GlyphPage; i < arraySize && i < itemsPerPage * (m_GlyphPage + 1); i++)
                    {
                        // Define the start of the selection region of the element.
                        Rect elementStartRegion = GUILayoutUtility.GetRect(0f, 0f, GUILayout.ExpandWidth(true));

                        int elementIndex = i;
                        if (!string.IsNullOrEmpty(m_searchPattern))
                            elementIndex = m_searchList[i];
                            
                        SerializedProperty glyphInfo = m_glyphInfoList_prop.GetArrayElementAtIndex(elementIndex);

                        EditorGUI.BeginDisabledGroup(i != m_selectedElement);
                        {
                            EditorGUILayout.BeginVertical(TMP_UIStyleManager.Group_Label);

                            EditorGUILayout.PropertyField(glyphInfo);

                            EditorGUILayout.EndVertical();
                        }
                        EditorGUI.EndDisabledGroup();

                        // Define the end of the selection region of the element.
                        Rect elementEndRegion = GUILayoutUtility.GetRect(0f, 0f, GUILayout.ExpandWidth(true));

                        // Check for Item selection
                        Rect selectionArea = new Rect(elementStartRegion.x, elementStartRegion.y, elementEndRegion.width, elementEndRegion.y - elementStartRegion.y);
                        if (DoSelectionCheck(selectionArea))
                        {
                            m_selectedElement = i;
                            m_AddGlyphWarning.isEnabled = false;
                            m_unicodeHexLabel = k_placeholderUnicodeHex;
                            GUIUtility.keyboardControl = 0;
                        }


                        // Draw Selection Highlight and Glyph Options
                        if (m_selectedElement == i)
                        {
                            TMP_EditorUtility.DrawBox(selectionArea, 2f, new Color32(40, 192, 255, 255));

                            // Draw Glyph management options
                            Rect controlRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight * 1f);
                            float optionAreaWidth = controlRect.width * 0.6f;
                            float btnWidth = optionAreaWidth / 3;

                            Rect position = new Rect(controlRect.x + controlRect.width * .4f, controlRect.y, btnWidth, controlRect.height);

                            // Copy Selected Glyph to Target Glyph ID
                            GUI.enabled = !string.IsNullOrEmpty(m_dstGlyphID);
                            if (GUI.Button(position, new GUIContent("Copy to")))
                            {
                                GUIUtility.keyboardControl = 0;

                                // Convert Hex Value to Decimal
                                int dstGlyphID = TMP_TextUtilities.StringToInt(m_dstGlyphID);

                                //Add new glyph at target Unicode hex id.
                                if (!AddNewGlyph(elementIndex, dstGlyphID))
                                {
                                    m_AddGlyphWarning.isEnabled = true;
                                    m_AddGlyphWarning.expirationTime = EditorApplication.timeSinceStartup + 1;
                                }

                                m_dstGlyphID = string.Empty;
                                m_isSearchDirty = true;

                                TMPro_EventManager.ON_FONT_PROPERTY_CHANGED(true, m_fontAsset);
                            }

                            // Target Glyph ID
                            GUI.enabled = true;
                            position.x += btnWidth;

                            GUI.SetNextControlName("GlyphID_Input");
                            m_dstGlyphID = EditorGUI.TextField(position, m_dstGlyphID);

                            // Placeholder text
                            EditorGUI.LabelField(position, new GUIContent(m_unicodeHexLabel, "The Unicode (Hex) ID of the duplicated Glyph"), TMP_UIStyleManager.Label);

                            // Only filter the input when the destination glyph ID text field has focus.
                            if (GUI.GetNameOfFocusedControl() == "GlyphID_Input")
                            {
                                m_unicodeHexLabel = string.Empty;

                                //Filter out unwanted characters.
                                char chr = Event.current.character;
                                if ((chr < '0' || chr > '9') && (chr < 'a' || chr > 'f') && (chr < 'A' || chr > 'F'))
                                {
                                    Event.current.character = '\0';
                                }
                            }
                            else
                                m_unicodeHexLabel = k_placeholderUnicodeHex;


                            // Remove Glyph
                            position.x += btnWidth;
                            if (GUI.Button(position, "Remove"))
                            {
                                GUIUtility.keyboardControl = 0;

                                RemoveGlyphFromList(elementIndex);

                                m_selectedElement = -1;
                                m_isSearchDirty = true;

                                TMPro_EventManager.ON_FONT_PROPERTY_CHANGED(true, m_fontAsset);

                                return;
                            }

                            if (m_AddGlyphWarning.isEnabled && EditorApplication.timeSinceStartup < m_AddGlyphWarning.expirationTime)
                            {
                                EditorGUILayout.HelpBox("The Destination Glyph ID already exists", MessageType.Warning);
                            }

                        }
                    }
                }

                DisplayGlyphPageNavigation(arraySize, itemsPerPage);
            }
            #endregion


            // KERNING TABLE PANEL
            #region Kerning Table
            GUILayout.Space(5);
            if (GUILayout.Button("Kerning Table Info\t" + (UI_PanelState.kerningInfoPanel ? uiStateLabel[1] : uiStateLabel[0]), TMP_UIStyleManager.Section_Label))
                UI_PanelState.kerningInfoPanel = !UI_PanelState.kerningInfoPanel;


            if (UI_PanelState.kerningInfoPanel)
            {
                
                Rect pos;

                SerializedProperty kerningPairs_prop = m_kerningInfo_prop.FindPropertyRelative("kerningPairs");

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Left Char", TMP_UIStyleManager.TMP_GUISkin.label);
                GUILayout.Label("Right Char", TMP_UIStyleManager.TMP_GUISkin.label);
                GUILayout.Label("Offset Value", TMP_UIStyleManager.TMP_GUISkin.label);
                GUILayout.Label(GUIContent.none, GUILayout.Width(20));
                EditorGUILayout.EndHorizontal();

                GUILayout.BeginVertical(TMP_UIStyleManager.TMP_GUISkin.label);

                int arraySize = kerningPairs_prop.arraySize;
                int itemsPerPage = 25;

                if (arraySize > 0)
                {
                    // Display each GlyphInfo entry using the GlyphInfo property drawer.
                    for (int i = itemsPerPage * m_KerningPage; i < arraySize && i < itemsPerPage * (m_KerningPage + 1); i++)
                    {
                        SerializedProperty kerningPair_prop = kerningPairs_prop.GetArrayElementAtIndex(i);

                        pos = EditorGUILayout.BeginHorizontal();

                        EditorGUI.PropertyField(new Rect(pos.x, pos.y, pos.width - 20f, pos.height), kerningPair_prop, GUIContent.none);

                        // Button to Delete Kerning Pair
                        if (GUILayout.Button("-", GUILayout.ExpandWidth(false)))
                        {
                            m_kerningTable.RemoveKerningPair(i);
                            m_fontAsset.ReadFontDefinition(); // Reload Font Definition.
                            serializedObject.Update(); // Get an updated version of the SerializedObject.
                            isAssetDirty = true;
                            break;
                        }

                        EditorGUILayout.EndHorizontal();
                    }
                }

                Rect pagePos = EditorGUILayout.GetControlRect(false, 20);
                pagePos.width /= 3;

                int shiftMultiplier = currentEvent.shift ? 10 : 1;

                // Previous Page
                if (m_KerningPage > 0) GUI.enabled = true;
                else GUI.enabled = false;

                if (GUI.Button(pagePos, "Previous Page"))
                    m_KerningPage -= 1 * shiftMultiplier;

                // Page Counter
                GUI.enabled = true;
                pagePos.x += pagePos.width;
                int totalPages = (int)(arraySize / (float)itemsPerPage + 0.999f);
                GUI.Label(pagePos, "Page " + (m_KerningPage + 1) + " / " + totalPages, GUI.skin.button);

                // Next Page
                pagePos.x += pagePos.width;
                if (itemsPerPage * (m_GlyphPage + 1) < arraySize) GUI.enabled = true;
                else GUI.enabled = false;

                if (GUI.Button(pagePos, "Next Page"))
                    m_KerningPage += 1 * shiftMultiplier;

                m_KerningPage = Mathf.Clamp(m_KerningPage, 0, arraySize / itemsPerPage);

                GUILayout.EndVertical();

                GUILayout.Space(10);


                // Add New Kerning Pair Section
                GUILayout.BeginVertical(TMP_UIStyleManager.SquareAreaBox85G);

                pos = EditorGUILayout.BeginHorizontal();

                // Draw Empty Kerning Pair 
                EditorGUI.PropertyField(new Rect(pos.x, pos.y, pos.width - 20f, pos.height), m_kerningPair_prop);
                GUILayout.Label(GUIContent.none, GUILayout.Height(19));

                EditorGUILayout.EndHorizontal();

                GUILayout.Space(5);

                if (GUILayout.Button("Add New Kerning Pair"))
                {
                    int asci_left = m_kerningPair_prop.FindPropertyRelative("AscII_Left").intValue;
                    int asci_right = m_kerningPair_prop.FindPropertyRelative("AscII_Right").intValue;
                    float xOffset = m_kerningPair_prop.FindPropertyRelative("XadvanceOffset").floatValue;

                    errorCode = m_kerningTable.AddKerningPair(asci_left, asci_right, xOffset);

                    // Sort Kerning Pairs & Reload Font Asset if new kerning pair was added.
                    if (errorCode != -1)
                    {
                        m_kerningTable.SortKerningPairs();
                        m_fontAsset.ReadFontDefinition(); // Reload Font Definition.
                        serializedObject.Update(); // Get an updated version of the SerializedObject.
                        isAssetDirty = true;
                    }
                    else
                    {
                        timeStamp = System.DateTime.Now.AddSeconds(5);
                    }
                }

                if (errorCode == -1)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Kerning Pair already <color=#ffff00>exists!</color>", TMP_UIStyleManager.Label);
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();

                    if (System.DateTime.Now > timeStamp)
                        errorCode = 0;
                }

                GUILayout.EndVertical();
            }
            #endregion


            if (serializedObject.ApplyModifiedProperties() || evt_cmd == k_UndoRedo || isAssetDirty)
            {
                //Debug.Log("Serialized properties have changed.");
                TMPro_EventManager.ON_FONT_PROPERTY_CHANGED(true, m_fontAsset);

                isAssetDirty = false;
                EditorUtility.SetDirty(target);
                //TMPro_EditorUtility.RepaintAll(); // Consider SetDirty
            }


            // Clear selection if mouse event was not consumed. 
            GUI.enabled = true;
            if (currentEvent.type == EventType.mouseDown && currentEvent.button == 0)
                m_selectedElement = -1;

        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="arraySize"></param>
        /// <param name="itemsPerPage"></param>
        void DisplayGlyphPageNavigation(int arraySize, int itemsPerPage)
        {
            Rect pagePos = EditorGUILayout.GetControlRect(false, 20);
            pagePos.width /= 3;

            int shiftMultiplier = Event.current.shift ? 10 : 1; // Page + Shift goes 10 page forward

            // Previous Page
            GUI.enabled = m_GlyphPage > 0;

            if (GUI.Button(pagePos, "Previous Page"))
            {
                m_GlyphPage -= 1 * shiftMultiplier;
                //m_isNewPage = true;
            }

            // Page Counter
            var pageStyle = new GUIStyle(GUI.skin.button) { normal = { background = null } };
            GUI.enabled = true;
            pagePos.x += pagePos.width;
            int totalPages = (int)(arraySize / (float)itemsPerPage + 0.999f);
            GUI.Button(pagePos, "Page " + (m_GlyphPage + 1) + " / " + totalPages, pageStyle);

            // Next Page
            pagePos.x += pagePos.width;
            GUI.enabled = itemsPerPage * (m_GlyphPage + 1) < arraySize;

            if (GUI.Button(pagePos, "Next Page"))
            {
                m_GlyphPage += 1 * shiftMultiplier;
                //m_isNewPage = true;
            }

            // Clamp page range
            m_GlyphPage = Mathf.Clamp(m_GlyphPage, 0, arraySize / itemsPerPage);

            GUI.enabled = true;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="srcGlyphID"></param>
        /// <param name="dstGlyphID"></param>
        bool AddNewGlyph(int srcIndex, int dstGlyphID)
        {
            // Make sure Destination Glyph ID doesn't already contain a Glyph
            if (m_fontAsset.characterDictionary.ContainsKey(dstGlyphID))
                return false;

            // Add new element to glyph list.
            m_glyphInfoList_prop.arraySize += 1;

            // Get a reference to the source glyph.
            SerializedProperty sourceGlyph = m_glyphInfoList_prop.GetArrayElementAtIndex(srcIndex);

            int dstIndex = m_glyphInfoList_prop.arraySize - 1;

            // Get a reference to the target / destination glyph.
            SerializedProperty targetGlyph = m_glyphInfoList_prop.GetArrayElementAtIndex(dstIndex);

            CopySerializedProperty(sourceGlyph, ref targetGlyph);

            // Update the ID of the glyph
            targetGlyph.FindPropertyRelative("id").intValue = dstGlyphID;

            serializedObject.ApplyModifiedProperties();

            m_fontAsset.SortGlyphs();

            m_fontAsset.ReadFontDefinition();

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="glyphID"></param>
        void RemoveGlyphFromList(int index)
        {
            if (index > m_glyphInfoList_prop.arraySize)
                return;

            m_glyphInfoList_prop.DeleteArrayElementAtIndex(index);

            serializedObject.ApplyModifiedProperties();

            m_fontAsset.ReadFontDefinition();
        }


        // Check if any of the Style elements were clicked on.
        private bool DoSelectionCheck(Rect selectionArea)
        {
            Event currentEvent = Event.current;

            switch (currentEvent.type)
            {
                case EventType.MouseDown:
                    if (selectionArea.Contains(currentEvent.mousePosition) && currentEvent.button == 0)
                    {
                        currentEvent.Use();
                        return true;
                    }

                    break;
            }

            return false;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        void CopySerializedProperty(SerializedProperty source, ref SerializedProperty target)
        {
            // TODO : Should make a generic function which copies each of the properties.
            target.FindPropertyRelative("id").intValue = source.FindPropertyRelative("id").intValue;
            target.FindPropertyRelative("x").floatValue = source.FindPropertyRelative("x").floatValue;
            target.FindPropertyRelative("y").floatValue = source.FindPropertyRelative("y").floatValue;
            target.FindPropertyRelative("width").floatValue = source.FindPropertyRelative("width").floatValue;
            target.FindPropertyRelative("height").floatValue = source.FindPropertyRelative("height").floatValue;
            target.FindPropertyRelative("xOffset").floatValue = source.FindPropertyRelative("xOffset").floatValue;
            target.FindPropertyRelative("yOffset").floatValue = source.FindPropertyRelative("yOffset").floatValue;
            target.FindPropertyRelative("xAdvance").floatValue = source.FindPropertyRelative("xAdvance").floatValue;
            target.FindPropertyRelative("scale").floatValue = source.FindPropertyRelative("scale").floatValue;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="searchPattern"></param>
        /// <returns></returns>
        void SearchGlyphTable (string searchPattern, ref List<int> searchResults)
        {
            if (searchResults == null) searchResults = new List<int>();

            searchResults.Clear();

            int arraySize = m_glyphInfoList_prop.arraySize;

            for (int i = 0; i < arraySize; i++)
            {
                SerializedProperty sourceGlyph = m_glyphInfoList_prop.GetArrayElementAtIndex(i);

                int id = sourceGlyph.FindPropertyRelative("id").intValue;

                // Check for potential match against a character.
                if (searchPattern.Length == 1 && id == searchPattern[0])
                    searchResults.Add(i);

                // Check for potential match against decimal id
                if (id.ToString().Contains(searchPattern))
                    searchResults.Add(i);

                if (id.ToString("x").Contains(searchPattern))
                    searchResults.Add(i);

                if (id.ToString("X").Contains(searchPattern))
                    searchResults.Add(i);
            }
        }
    }
}