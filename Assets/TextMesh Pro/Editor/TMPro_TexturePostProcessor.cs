// Copyright (C) 2014 - 2016 Stephan Bouchard - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms


using UnityEngine;
using UnityEditor;
using System.Collections;


namespace TMPro.EditorUtilities
{

    public class TMPro_TexturePostProcessor : AssetPostprocessor
    {

        void OnPostprocessTexture(Texture2D texture)
        {
            //var importer = assetImporter as TextureImporter;

            Texture2D tex = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Texture2D)) as Texture2D;

            // Send Event Sub Objects
            if (tex != null)
                TMPro_EventManager.ON_SPRITE_ASSET_PROPERTY_CHANGED(true, tex);
        }

    }


    public class TMPro_PackageImportPostProcessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            for (int i = 0; i < deletedAssets.Length; i++)
            {
                if (deletedAssets[i] == "Assets/TextMesh Pro")
                {
                    //Debug.Log("Asset [" + deletedAssets[i] + "] has been deleted.");
                    string currentBuildSettings = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

                    //Check for and inject TMP_PRESENT
                    if (currentBuildSettings.Contains("TMP_PRESENT;"))
                    {
                        currentBuildSettings = currentBuildSettings.Replace("TMP_PRESENT;", "");

                        PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, currentBuildSettings);
                    }
                    else if (currentBuildSettings.Contains("TMP_PRESENT"))
                    {
                        currentBuildSettings = currentBuildSettings.Replace("TMP_PRESENT", "");

                        PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, currentBuildSettings);
                    }
                }
            }
        }
    }
}