using UnityEngine;
using UnityEditor;
using System.IO;

[InitializeOnLoad]
public static class SoftMaskForTextMeshProRemover {
    static SoftMaskForTextMeshProRemover() {
        FindAndRemovePackage();
    }

    static void FindAndRemovePackage() {
        var packagePath = FindPackage();
        if (!string.IsNullOrEmpty(packagePath))
            if (DoesUserWantToRemoveOldPackage())
                RemovePackage(packagePath);
    }

    static readonly string PackageName = "SoftMask for TextMesh Pro";
    static readonly string MaterialReplacerGUID = "e02afb692a3072842b6746cf08904cea";

    static string FindPackage() {
        var path = AssetDatabase.GUIDToAssetPath(MaterialReplacerGUID);
        if (!string.IsNullOrEmpty(path)) {
            var scriptsDir = Path.GetDirectoryName(path);
            var packageDir = Path.GetDirectoryName(scriptsDir);
            if (!string.IsNullOrEmpty(packageDir))
                if (Path.GetFileName(packageDir) == PackageName)
                    return packageDir;
        }
        return "";
    }
    
    static void RemovePackage(string packagePath) {
        if (AssetDatabase.DeleteAsset(packagePath))
            Debug.LogFormat("{0} was successfully removed", PackageName);
        else
            Debug.LogWarningFormat("Unable to remove {0} package", PackageName);
    }

    static bool DoesUserWantToRemoveOldPackage() {
        return EditorUtility.DisplayDialog(
            "Soft Mask",
            string.Format(
                "We've found the old {0} package. Soft Mask doesn't require this package to"
                + " work with TextMesh Pro since version 1.3. Do you want to remove the old"
                + " integration package now? The project will not compile until it's removed.",
                PackageName),
            "Remove the old package",
            "Leave it, I'll remove it later");
    }
}
