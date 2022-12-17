using System.IO;
using UnityEngine;
using UnityEditor;

namespace SoftMasking.Editor {
    public static class PackageResources {
        static string _packageAssetsPath = string.Empty;

        public static string packageAssetsPath {
            get {
                if (string.IsNullOrEmpty(_packageAssetsPath)) {
                    _packageAssetsPath = SearchForPackageRootPath();
                    if (!string.IsNullOrEmpty(_packageAssetsPath))
                        _packageAssetsPath = Path.Combine(_packageAssetsPath, "Assets");
                    else
                        Debug.LogError(
                            "Unable to locate Soft Mask root folder. " +
                            "Make sure the package has been installed correctly.");
                }
                return _packageAssetsPath;
            }
        }

        public const string SoftMaskCsGUID = "0bac33ade27cf4542bd53b1b13d90941";

        static string SearchForPackageRootPath() {
            var softMaskCsPath = AssetDatabase.GUIDToAssetPath(SoftMaskCsGUID);
            if (string.IsNullOrEmpty(softMaskCsPath))
                return "";
            var scriptsDir = Path.GetDirectoryName(softMaskCsPath);
            var packageDir = Path.GetDirectoryName(scriptsDir);
            return packageDir;
        }
    }
}
