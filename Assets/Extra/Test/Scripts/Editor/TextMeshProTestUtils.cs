using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SoftMasking.Tests {
    // We need two things from TextMesh Pro while running multiversion tests:
    //   - import essential resources
    //   - convert GUIDs (because original 5.6 SoftMask uses source version of TMPro)
    // TextMesh Pro package has methods for this:
    //   - TMPro.TMP_PackageUtilities.ConvertProjectGUIDsMenu
    //   - TMPro.TMP_ProjectConversionUtility.UpdateProjectFiles
    // But these methods show up dialog boxes and want user input which isn't
    // acceptable in batch mode. So we've copied some disassembled code from
    // TextMesh Pro with minor changes and put it into this static class.
    static class TextMeshProTestUtils {
    #if UNITY_2019_1_OR_NEWER
	    struct AssetModificationRecord {
		    public string assetFilePath;
		    public string assetDataFile;
	    }

        [Serializable] class AssetConversionData {
	        public List<AssetConversionRecord> assetRecords;
        }

        [Serializable] struct AssetConversionRecord {
	        public string referencedResource;
	        public string target;
	        public string replacement;
        }

        public static void ImportProjectResources() {
            AssetDatabase.ImportPackage(GetPackageFullPath() + "/Package Resources/TMP Essential Resources.unitypackage", interactive: false);
        }

	    public static void ConvertProjectGUIDs() {
		    string projectPath = Path.GetFullPath("Assets/..");
		    string packageFullPath = GetPackageFullPath();
		    var modifiedAssetList = new List<AssetModificationRecord>();
		    var conversionData = JsonUtility.FromJson<AssetConversionData>(File.ReadAllText(packageFullPath + "/PackageConversionData.json"));
		    string[] projectGUIDs2 = AssetDatabase.FindAssets("t:Object");
		    var modifiedAsset = default(AssetModificationRecord);
		    foreach (string guid in projectGUIDs2) {
			    string assetFilePath = AssetDatabase.GUIDToAssetPath(guid);
			    Type assetType = AssetDatabase.GetMainAssetTypeAtPath(assetFilePath);
			    if (!(assetType == typeof(DefaultAsset)) 
                        && !(assetType == typeof(MonoScript)) 
                        && !(assetType == typeof(Texture2D)) 
                        && !(assetType == typeof(TextAsset)) 
                        && !(assetType == typeof(Shader))) {
				    string assetDataFile = File.ReadAllText(projectPath + "/" + assetFilePath);
				    bool hasFileChanged = false;
				    foreach (AssetConversionRecord record in conversionData.assetRecords) {
					    if (assetDataFile.Contains(record.target)) {
						    hasFileChanged = true;
						    assetDataFile = assetDataFile.Replace(record.target, record.replacement);
						    Debug.Log("Replacing Reference to [" + record.referencedResource + "] using [" + record.target + "] with [" + record.replacement + "] in asset file: [" + assetFilePath + "].");
					    }
				    }
				    if (hasFileChanged) {
					    Debug.Log("Adding [" + assetFilePath + "] to list of assets to be modified.");
					    modifiedAsset.assetFilePath = assetFilePath;
					    modifiedAsset.assetDataFile = assetDataFile;
					    modifiedAssetList.Add(modifiedAsset);
				    }
			    }
		    }
		    projectGUIDs2 = AssetDatabase.FindAssets("t:Object");
		    var modifiedAsset2 = default(AssetModificationRecord);
		    foreach (string guid2 in projectGUIDs2) {
			    string assetFilePath2 = AssetDatabase.GUIDToAssetPath(guid2);
			    string assetMetaFilePath = AssetDatabase.GetTextMetaFilePathFromAssetPath(assetFilePath2);
			    string assetMetaFile = File.ReadAllText(projectPath + "/" + assetMetaFilePath);
			    bool hasFileChanged2 = false;
			    foreach (AssetConversionRecord record2 in conversionData.assetRecords) {
				    if (assetMetaFile.Contains(record2.target)) {
					    hasFileChanged2 = true;
					    assetMetaFile = assetMetaFile.Replace(record2.target, record2.replacement);
					    Debug.Log((object)("Replacing Reference to [" + record2.referencedResource + "] using [" + record2.target + "] with [" + record2.replacement + "] in asset file: [" + assetMetaFilePath + "]."));
				    }
			    }
			    if (hasFileChanged2) {
				    Debug.Log((object)("Adding [" + assetMetaFilePath + "] to list of meta files to be modified."));
				    modifiedAsset2.assetFilePath = assetMetaFilePath;
				    modifiedAsset2.assetDataFile = assetMetaFile;
				    modifiedAssetList.Add(modifiedAsset2);
			    }
		    }
			for (int i = 0; i < modifiedAssetList.Count; i++)
                File.WriteAllText(projectPath + "/" + modifiedAssetList[i].assetFilePath, modifiedAssetList[i].assetDataFile);
		    AssetDatabase.Refresh();
	    }

        static string GetPackageFullPath() {
	        string packagePath = Path.GetFullPath("Packages/com.unity.textmeshpro");
	        if (Directory.Exists(packagePath))
		        return packagePath;
	        packagePath = Path.GetFullPath("Assets/..");
	        if (Directory.Exists(packagePath)) {
		        if (Directory.Exists(packagePath + "/Assets/Packages/com.unity.TextMeshPro/Editor Resources"))
			        return packagePath + "/Assets/Packages/com.unity.TextMeshPro";
		        if (Directory.Exists(packagePath + "/Assets/TextMesh Pro/Editor Resources"))
			        return packagePath + "/Assets/TextMesh Pro";
		        string[] matchingPaths = Directory.GetDirectories(packagePath, "TextMesh Pro", SearchOption.AllDirectories);
		        string path = ValidateLocation(matchingPaths, packagePath);
		        if (path != null)
			        return packagePath + path;
	        }
	        return null;
        }

        static string ValidateLocation(string[] paths, string projectPath) {
	        for (int i = 0; i < paths.Length; i++)
		        if (Directory.Exists(paths[i] + "/Editor Resources")) {
			        var folderPath = paths[i].Replace(projectPath, "");
			        folderPath = folderPath.TrimStart('\\', '/');
			        return folderPath;
		        }
	        return null;
        }
    #endif
    }
}