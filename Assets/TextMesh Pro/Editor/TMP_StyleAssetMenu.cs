// Copyright (C) 2014 - 2016 Stephan Bouchard - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms


using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;


namespace TMPro.EditorUtilities
{

    public static class TMP_StyleAssetMenu
    {

        [MenuItem("Assets/Create/TextMeshPro/Style Sheet", false, 120)]
        public static void CreateTextMeshProObjectPerform()
        {
            string filePath;
            if (Selection.assetGUIDs.Length == 0)
            {
                // No asset selected.
                filePath = "Assets";
            }
            else
            {
                // Get the path of the selected folder or asset.
                filePath = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);

                // Get the file extension of the selected asset as it might need to be removed.
                string fileExtension = Path.GetExtension(filePath);
                if (fileExtension != "")
                {
                    filePath = Path.GetDirectoryName(filePath);
                }
            }


            string filePathWithName = AssetDatabase.GenerateUniqueAssetPath(filePath + "/TMP StyleSheet.asset");

            //// Create new Style Sheet Asset.
            TMP_StyleSheet styleSheet = ScriptableObject.CreateInstance<TMP_StyleSheet>();

            AssetDatabase.CreateAsset(styleSheet, filePathWithName);

            EditorUtility.SetDirty(styleSheet);

            AssetDatabase.SaveAssets();
        }
    }

}