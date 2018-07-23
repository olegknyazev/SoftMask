using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace SoftMasking.Editor {
    public static class PackageResources {
        static string _packagePath = string.Empty;
        static string _resourcesPath = string.Empty;

        public static string packagePath {
            get {
                if (string.IsNullOrEmpty(_packagePath)) {
                    _packagePath = SearchForAssetPath();
                    if (string.IsNullOrEmpty(_packagePath))
                        Debug.LogError(
                            "Unable to locate Soft Mask root folder. " +
                            "Make sure the package has been installed correctly.");
                }
                return _packagePath;
            }
        }

        public static string generatedShaderResourcesPath {
            get {
                if (string.IsNullOrEmpty(_resourcesPath))
                    _resourcesPath = CombinePath(packagePath, "Shaders", "Generated", "Resources");
                return _resourcesPath;
            }
        }

        const string PackageRelativePath = "/SoftMask/";

        static string SearchForAssetPath() {
            foreach (var assetPath in AssetDatabase.GetAllAssetPaths())
                if (assetPath.Contains(PackageRelativePath)) {
                    var relativePathEnd =
                        assetPath.LastIndexOf(PackageRelativePath, StringComparison.InvariantCulture)
                        + PackageRelativePath.Length;
                    return assetPath.Substring(0, relativePathEnd);
                }
            return "";
        }

        static string CombinePath(params string[] paths) {
            return (paths == null || paths.Length == 0) ? "" : paths.Aggregate(Path.Combine);
        }
    }
}
