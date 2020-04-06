// Copyright (C) 2014 - 2016 Stephan Bouchard - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms


using UnityEngine;
using UnityEditor;

namespace TMPro.EditorUtilities
{
    /// <summary>Base class for TextMesh Pro shader GUIs.</summary>
    public abstract class TMP_BaseShaderGUI : ShaderGUI
    {
        /// <summary>Representation of a #pragma shader_feature.</summary>
        /// <description>It is assumed that the first feature option is for no keyword (underscores).</description>
        protected class ShaderFeature
        {
            public string undoLabel;

            public GUIContent label;

            /// <summary>The keyword labels, for display. Include the no-keyword as the first option.</summary>
            public GUIContent[] keywordLabels;

            /// <summary>The shader keywords. Exclude the no-keyword option.</summary>
            public string[] keywords;

            int state;

            public bool Active
            {
                get { return state >= 0; }
            }

            public int State
            {
                get { return state; }
            }

            public void ReadState(Material material)
            {
                for (int i = 0; i < keywords.Length; i++)
                {
                    if (material.IsKeywordEnabled(keywords[i]))
                    {
                        state = i;
                        return;
                    }
                }
                state = -1;
            }

            public void SetActive(bool active, Material material)
            {
                state = active ? 0 : -1;
                SetStateKeywords(material);
            }

            public void DoPopup(MaterialEditor editor, Material material)
            {
                EditorGUI.BeginChangeCheck();
                int selection = EditorGUILayout.Popup(label, state + 1, keywordLabels);
                if (EditorGUI.EndChangeCheck())
                {
                    state = selection - 1;
                    editor.RegisterPropertyChangeUndo(undoLabel);
                    SetStateKeywords(material);
                }
            }

            void SetStateKeywords(Material material)
            {
                for (int i = 0; i < keywords.Length; i++)
                {
                    if (i == state)
                    { material.EnableKeyword(keywords[i]); }
                    else
                    { material.DisableKeyword(keywords[i]); }
                }
            }
        }

        /// <summary>Material panel that keeps track of whether the user opened or closed it.</summary>
        protected class MaterialPanel
        {
            string key, label;
            bool expanded;

            public bool Expanded
            {
                get { return expanded; }
            }

            public string Label
            {
                get { return label; }
            }

            public MaterialPanel(string name, bool expandedByDefault)
            {
                label = "<b>" + name + "</b> - <i>Settings</i> -";
                key = "TexMeshPro.material." + name + ".expanded";
                expanded = EditorPrefs.GetBool(key, expandedByDefault);
            }

            public void ToggleExpanded()
            {
                expanded = !expanded;
                EditorPrefs.SetBool(key, expanded);
            }
        }

        static GUIContent tempLabel = new GUIContent();

        static int undoRedoCount, lastSeenUndoRedoCount;

        static float[][] tempFloats = {
        null, new float[1], new float[2], new float[3], new float[4]
    };

        protected static GUIContent[] xywhVectorLabels = {
        new GUIContent("X"),
        new GUIContent("Y"),
        new GUIContent("W", "Width"),
        new GUIContent("H", "Height")
    };

        protected static GUIContent[] lbrtVectorLabels = {
        new GUIContent("L", "Left"),
        new GUIContent("B", "Bottom"),
        new GUIContent("R", "Right"),
        new GUIContent("T", "Top")
    };

        static TMP_BaseShaderGUI()
        {
            // Keep track of how many undo/redo events happened.
            Undo.undoRedoPerformed += () => undoRedoCount += 1;
        }

        bool isNewGUI = true;

        float dragAndDropMinY;

        protected MaterialEditor editor;

        protected Material material;

        protected MaterialProperty[] properties;

        void PrepareGUI()
        {
            isNewGUI = false;
            TMP_UIStyleManager.GetUIStyles();
            ShaderUtilities.GetShaderPropertyIDs();
            // New GUI just got constructed. This happens in response to a selection,
            // but also after undo/redo events.
            if (lastSeenUndoRedoCount != undoRedoCount)
            {
                // There's been at least one undo/redo since the last time this GUI got constructed.
                // Maybe the undo/redo was for this material? Assume that is was.
                TMPro_EventManager.ON_MATERIAL_PROPERTY_CHANGED(true, material as Material);
            }
            lastSeenUndoRedoCount = undoRedoCount;
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            editor = materialEditor;
            material = materialEditor.target as Material;
            this.properties = properties;

            if (isNewGUI)
            { PrepareGUI(); }

            EditorGUIUtility.labelWidth = 130f;
            EditorGUIUtility.fieldWidth = 50f;

            DoDragAndDropBegin();
            EditorGUI.BeginChangeCheck();
            DoGUI();
            if (EditorGUI.EndChangeCheck())
            { TMPro_EventManager.ON_MATERIAL_PROPERTY_CHANGED(true, material); }
            DoDragAndDropEnd();
        }

        /// <summary>Override this method to create the specific shader GUI.</summary>
        protected abstract void DoGUI();

        protected bool DoPanelHeader(MaterialPanel panel)
        {
            if (GUILayout.Button(panel.Label, TMP_UIStyleManager.Group_Label))
            { panel.ToggleExpanded(); }
            return panel.Expanded;
        }

        protected bool DoPanelHeader(MaterialPanel panel, ShaderFeature feature, bool readState = true)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 22f);
            GUI.Label(rect, GUIContent.none, TMP_UIStyleManager.Group_Label);
            if (GUI.Button(
                new Rect(rect.x, rect.y, 250f, rect.height), panel.Label, TMP_UIStyleManager.Group_Label_Left
            ))
            { panel.ToggleExpanded(); }

            if (readState)
            { feature.ReadState(material); }

            EditorGUI.BeginChangeCheck();
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 70f;
            bool enabled = EditorGUI.Toggle(
                new Rect(rect.width - 90f, rect.y + 3f, 90f, 22f), new GUIContent("Enable ->"),
                feature.Active
            );
            EditorGUIUtility.labelWidth = labelWidth;
            if (EditorGUI.EndChangeCheck())
            {
                editor.RegisterPropertyChangeUndo(feature.undoLabel);
                feature.SetActive(enabled, material);
            }
            return panel.Expanded;
        }

        MaterialProperty BeginProperty(string name)
        {
            MaterialProperty property = FindProperty(name, properties);
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = property.hasMixedValue;
            #if UNITY_2017_1_OR_NEWER
                editor.BeginAnimatedCheck(Rect.zero, property);
            #else
                editor.BeginAnimatedCheck(property);
            #endif
            return property;
        }

        bool EndProperty()
        {
            editor.EndAnimatedCheck();
            EditorGUI.showMixedValue = false;
            return EditorGUI.EndChangeCheck();
        }

        protected void DoPopup(string name, string label, GUIContent[] options)
        {
            MaterialProperty property = BeginProperty(name);
            tempLabel.text = label;
            int index = EditorGUILayout.Popup(tempLabel, (int)property.floatValue, options);
            if (EndProperty())
            { property.floatValue = index; }
        }

        protected void DoCubeMap(string name, string label)
        { DoTexture(name, label, typeof(Cubemap)); }

        protected void DoTexture2D(string name, string label, bool withTilingOffset = false, string[] speedNames = null)
        { DoTexture(name, label, typeof(Texture2D), withTilingOffset, speedNames); }

        void DoTexture(string name, string label, System.Type type, bool withTilingOffset = false, string[] speedNames = null)
        {
            MaterialProperty property = BeginProperty(name);
            Rect rect = EditorGUILayout.GetControlRect(true, 60f);
            float totalWidth = rect.width;
            rect.width = EditorGUIUtility.labelWidth + 60f;
            tempLabel.text = label;
            Object tex = EditorGUI.ObjectField(rect, tempLabel, property.textureValue, type, false);

            if (EndProperty())
            { property.textureValue = tex as Texture; }

            rect.x += rect.width + 4f;
            rect.width = totalWidth - rect.width - 4f;
            rect.height = EditorGUIUtility.singleLineHeight;

            if (withTilingOffset)
            {
                DoTilingOffset(rect, property);
                rect.y += (rect.height + 2f) * 2f;
            }

            if (speedNames != null)
            {
                DoUVSpeed(rect, speedNames);
            }
        }

        void DoTilingOffset(Rect rect, MaterialProperty property)
        {
            float labelWidth = EditorGUIUtility.labelWidth;
            int indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUIUtility.labelWidth = 40f;

            Vector4 vector = property.textureScaleAndOffset;

            bool changed = false;
            float[] values = tempFloats[2];

            tempLabel.text = "Tiling";
            Rect vectorRect = EditorGUI.PrefixLabel(rect, tempLabel);
            values[0] = vector.x;
            values[1] = vector.y;
            EditorGUI.BeginChangeCheck();
            EditorGUI.MultiFloatField(vectorRect, xywhVectorLabels, values);
            if (EndProperty())
            {
                vector.x = values[0];
                vector.y = values[1];
                changed = true;
            }

            rect.y += rect.height + 2f;
            tempLabel.text = "Offset";
            vectorRect = EditorGUI.PrefixLabel(rect, tempLabel);
            values[0] = vector.z;
            values[1] = vector.w;
            EditorGUI.BeginChangeCheck();
            EditorGUI.MultiFloatField(vectorRect, xywhVectorLabels, values);
            if (EndProperty())
            {
                vector.z = values[0];
                vector.w = values[1];
                changed = true;
            }

            if (changed)
            { property.textureScaleAndOffset = vector; }

            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUI.indentLevel = indentLevel;
        }

        protected void DoUVSpeed(Rect rect, string[] names)
        {
            float labelWidth = EditorGUIUtility.labelWidth;
            int indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUIUtility.labelWidth = 40f;

            tempLabel.text = "Speed";
            rect = EditorGUI.PrefixLabel(rect, tempLabel);

            EditorGUIUtility.labelWidth = 13f;
            rect.width = rect.width * 0.5f - 1f;
            DoFloat(rect, names[0], "X");
            rect.x += rect.width + 2f;
            DoFloat(rect, names[1], "Y");
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUI.indentLevel = indentLevel;
        }

        protected void DoToggle(string name, string label)
        {
            MaterialProperty property = BeginProperty(name);
            tempLabel.text = label;
            bool value = EditorGUILayout.Toggle(tempLabel, property.floatValue == 1f);
            if (EndProperty())
            { property.floatValue = value ? 1f : 0f; }
        }

        protected void DoFloat(string name, string label)
        {
            MaterialProperty property = BeginProperty(name);
            Rect rect = EditorGUILayout.GetControlRect();
            rect.width = 225f;
            tempLabel.text = label;
            float value = EditorGUI.FloatField(rect, tempLabel, property.floatValue);
            if (EndProperty())
            { property.floatValue = value; }
        }

        protected void DoColor(string name, string label)
        {
            MaterialProperty property = BeginProperty(name);
            tempLabel.text = label;
            Color value = EditorGUI.ColorField(EditorGUILayout.GetControlRect(), tempLabel, property.colorValue);
            if (EndProperty())
            { property.colorValue = value; }
        }

        void DoFloat(Rect rect, string name, string label)
        {
            MaterialProperty property = BeginProperty(name);
            tempLabel.text = label;
            float value = EditorGUI.FloatField(rect, tempLabel, property.floatValue);
            if (EndProperty())
            { property.floatValue = value; }
        }

        protected void DoSlider(string name, string label)
        {
            MaterialProperty property = BeginProperty(name);
            Vector2 range = property.rangeLimits;
            tempLabel.text = label;
            float value = EditorGUI.Slider(
                EditorGUILayout.GetControlRect(), tempLabel, property.floatValue, range.x, range.y
            );
            if (EndProperty())
            { property.floatValue = value; }
        }

        protected void DoVector3(string name, string label)
        {
            MaterialProperty property = BeginProperty(name);
            tempLabel.text = label;
            Vector4 value = EditorGUILayout.Vector3Field(tempLabel, property.vectorValue);
            if (EndProperty())
            { property.vectorValue = value; }
        }

        protected void DoVector(string name, string label, GUIContent[] subLabels)
        {
            MaterialProperty property = BeginProperty(name);
            Rect rect = EditorGUILayout.GetControlRect();
            tempLabel.text = label;
            rect = EditorGUI.PrefixLabel(rect, tempLabel);
            Vector4 vector = property.vectorValue;

            float[] values = tempFloats[subLabels.Length];
            for (int i = 0; i < subLabels.Length; i++)
            { values[i] = vector[i]; }

            EditorGUI.MultiFloatField(rect, subLabels, values);
            if (EndProperty())
            {
                for (int i = 0; i < subLabels.Length; i++)
                { vector[i] = values[i]; }
                property.vectorValue = vector;
            }
        }

        protected void DoEmptyLine()
        { GUILayout.Space(EditorGUIUtility.singleLineHeight); }

        void DoDragAndDropBegin()
        { dragAndDropMinY = GUILayoutUtility.GetRect(0f, 0f, GUILayout.ExpandWidth(true)).y; }

        void DoDragAndDropEnd()
        {
            Rect rect = GUILayoutUtility.GetRect(0f, 0f, GUILayout.ExpandWidth(true));
            Event evt = Event.current;
            if (evt.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                evt.Use();
            }
            else if (
                evt.type == EventType.DragPerform &&
                Rect.MinMaxRect(rect.xMin, dragAndDropMinY, rect.xMax, rect.yMax).Contains(evt.mousePosition)
            )
            {
                DragAndDrop.AcceptDrag();
                evt.Use();
                Material droppedMaterial = DragAndDrop.objectReferences[0] as Material;
                if (droppedMaterial && droppedMaterial != material)
                { PerformDrop(droppedMaterial); }
            }
        }

        void PerformDrop(Material droppedMaterial)
        {
            Texture droppedTex = droppedMaterial.GetTexture(ShaderUtilities.ID_MainTex);
            if (!droppedTex)
            { return; }

            Texture currentTex = material.GetTexture(ShaderUtilities.ID_MainTex);
            TMP_FontAsset requiredFontAsset = null;
            if (droppedTex != currentTex)
            {
                requiredFontAsset = TMP_EditorUtility.FindMatchingFontAsset(droppedMaterial);
                if (!requiredFontAsset)
                { return; }
            }

            foreach (GameObject o in Selection.gameObjects)
            {
                if (requiredFontAsset)
                {
                    TMP_Text textComponent = o.GetComponent<TMP_Text>();
                    if (textComponent)
                    {
                        Undo.RecordObject(textComponent, "Font Asset Change");
                        textComponent.font = requiredFontAsset;
                    }
                }
                TMPro_EventManager.ON_DRAG_AND_DROP_MATERIAL_CHANGED(o, material, droppedMaterial);
                EditorUtility.SetDirty(o);
            }
        }
    }
}