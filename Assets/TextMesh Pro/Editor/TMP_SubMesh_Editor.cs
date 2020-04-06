// Copyright (C) 2014 - 2016 Stephan Bouchard - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms


using UnityEngine;
using UnityEditor;
using System.Collections;

namespace TMPro.EditorUtilities
{
    [CustomEditor(typeof(TMP_SubMesh)), CanEditMultipleObjects]
    public class TMP_SubMesh_Editor : Editor
    {
        private struct m_foldout
        { // Track Inspector foldout panel states, globally.
            //public static bool textInput = true;
            public static bool fontSettings = true;
            //public static bool extraSettings = false;
            //public static bool shadowSetting = false;
            //public static bool materialEditor = true;
        }

        private static string[] uiStateLabel = new string[] { "\t- <i>Click to expand</i> -", "\t- <i>Click to collapse</i> -" };

        private SerializedProperty fontAsset_prop;
        private SerializedProperty spriteAsset_prop;

        private TMP_SubMesh m_SubMeshComponent;
        private Renderer m_Renderer;

        public void OnEnable()
        {
            // Get the UI Skin and Styles for the various Editors
            TMP_UIStyleManager.GetUIStyles();

            fontAsset_prop = serializedObject.FindProperty("m_fontAsset");
            spriteAsset_prop = serializedObject.FindProperty("m_spriteAsset");

            m_SubMeshComponent = target as TMP_SubMesh;

            m_Renderer = m_SubMeshComponent.renderer;
        }


        public override void OnInspectorGUI()
        {

            EditorGUI.indentLevel = 0;

            // FONT SETTINGS SECTION
            if (GUILayout.Button("<b>SUB OBJECT SETTINGS</b>" + (m_foldout.fontSettings ? uiStateLabel[1] : uiStateLabel[0]), TMP_UIStyleManager.Section_Label))
                m_foldout.fontSettings = !m_foldout.fontSettings;

            if (m_foldout.fontSettings)
            {
                GUI.enabled = false;
                EditorGUILayout.PropertyField(fontAsset_prop);
                EditorGUILayout.PropertyField(spriteAsset_prop);
                GUI.enabled = true;
            }
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Sorting Layer");
            EditorGUI.BeginChangeCheck();

            //float old_LabelWidth = EditorGUIUtility.labelWidth;
            //float old_fieldWidth = EditorGUIUtility.fieldWidth;


            // SORTING LAYERS
            var sortingLayerNames = SortingLayerHelper.sortingLayerNames;

            // Look up the layer name using the current layer ID
            string oldName = SortingLayerHelper.GetSortingLayerNameFromID(m_Renderer.sortingLayerID);

            // Use the name to look up our array index into the names list
            int oldLayerIndex = System.Array.IndexOf(sortingLayerNames, oldName);

            // Show the pop-up for the names
            EditorGUIUtility.fieldWidth = 0f;
            int newLayerIndex = EditorGUILayout.Popup(string.Empty, oldLayerIndex, sortingLayerNames, GUILayout.MinWidth(80f));

            // If the index changes, look up the ID for the new index to store as the new ID
            if (newLayerIndex != oldLayerIndex)
            {
                //Undo.RecordObject(renderer, "Edit Sorting Layer");
                m_Renderer.sortingLayerID = SortingLayerHelper.GetSortingLayerIDForIndex(newLayerIndex);
                //EditorUtility.SetDirty(renderer);
            }

            // Expose the manual sorting order
            EditorGUIUtility.labelWidth = 40f;
            EditorGUIUtility.fieldWidth = 80f;
            int newSortingLayerOrder = EditorGUILayout.IntField("Order", m_Renderer.sortingOrder);
            if (newSortingLayerOrder != m_Renderer.sortingOrder)
            {
                //Undo.RecordObject(renderer, "Edit Sorting Order");
                m_Renderer.sortingOrder = newSortingLayerOrder;
            }
            EditorGUILayout.EndHorizontal();

            //    // If a Custom Material Editor exists, we use it.
            //    if (m_canvasRenderer != null && m_canvasRenderer.GetMaterial() != null)
            //    {
            //        Material mat = m_canvasRenderer.GetMaterial();

            //        //Debug.Log(mat + "  " + m_targetMaterial);

            //        if (mat != m_targetMaterial)
            //        {
            //            // Destroy previous Material Instance
            //            //Debug.Log("New Material has been assigned.");
            //            m_targetMaterial = mat;
            //            DestroyImmediate(m_materialEditor);
            //        }


            //        if (m_materialEditor == null)
            //        {
            //            m_materialEditor = Editor.CreateEditor(mat);
            //        }

            //        m_materialEditor.DrawHeader();


            //        m_materialEditor.OnInspectorGUI();
            //    }
        }
    }
}
