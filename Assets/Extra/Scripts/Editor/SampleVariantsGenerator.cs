using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SoftMasking.TextMeshPro.Editor {
    // Generates Binary and Package variants of samples from Source samples.
    public static class SampleVariantsGenerator {
        const string samplesRoot = "Assets/SoftMask/Samples/TextMeshPro";
        const string sourcePrefix = "SourceTMPro";
        const string binaryPrefix = "BinaryTMPro";
        const string packagePrefix = "PackageTMPro";

        struct Bucket {
            public string source;
            public string[] destinations;

            public Bucket(string source, params string[] destinations) {
                this.source = source;
                this.destinations = destinations;
            }

            public Bucket Map(Func<string, string> f) {
                return new Bucket(f(source), destinations.Select(f).ToArray());
            }
        }

        static readonly List<Bucket> tmproReplacements = new List<Bucket> {
            new Bucket( // TMP_FontAsset (ScriptableObject)
                "fileID: 11500000, guid: 74dfce233ddb29b4294c3e23c1d3650d",
                "fileID: -667331979, guid: 89f0137620f6af44b9ba852b4190e64e",
                "fileID: 11500000, guid: 71c1514a6bd24e1e882cebbe1904ce04"),
            new Bucket( // TextMeshProUGUI (MonoBehaviour)
                "fileID: 11500000, guid: 496f2e385b0c62542b5c739ccfafd8da",
                "fileID: 1453722849, guid: 89f0137620f6af44b9ba852b4190e64e",
                "fileID: 11500000, guid: f4688fdb7df04437aeb418b961361dc5"),
            new Bucket( // TMP_SDF (shader)
                "guid: dca26082f9cb439469295791d9f76fe5", // Source and Binary
                "guid: dca26082f9cb439469295791d9f76fe5", // use the same shaders
                "guid: 68e6db2ebdc24f95958faec2be5558d6")
        };

        [MenuItem("Tools/Soft Mask/Generate Binary and Package sample variants")]
        public static void GenerateAssets() {
            var assetBuckets = CollectAssets();
            var assetReplacements = ToGUIDs(assetBuckets);
            var allReplacements = tmproReplacements.Concat(assetReplacements);
            foreach (var assets in assetBuckets) {
                Debug.LogFormat("Updating {0}", assets.source);
                ReplaceInAssetFiles(assets, allReplacements);
            }
            AssetDatabase.Refresh();
        }

        static IEnumerable<Bucket> CollectAssets() {
            var srcPath = Path.Combine(samplesRoot, sourcePrefix);
            var binaryPath = Path.Combine(samplesRoot, binaryPrefix);
            var packagePath = Path.Combine(samplesRoot, packagePrefix);
            return
                AssetDatabase
                    .FindAssets("t:Prefab t:Scene t:ScriptableObject", new[] { srcPath })
                    .Select(
                        asset => {
                            var srcAssetPath = AssetDatabase.GUIDToAssetPath(asset);
                            var srcAssetRelativePath = srcAssetPath.Substring(srcPath.Length + 1);
                            var binaryAssetPath = Path.Combine(binaryPath, srcAssetRelativePath);
                            var packageAssetPath = Path.Combine(packagePath, srcAssetRelativePath);
                            return new Bucket(srcAssetPath, binaryAssetPath, packageAssetPath);
                        });
        }

        static IEnumerable<Bucket> ToGUIDs(IEnumerable<Bucket> assetPathBuckets) {
            return assetPathBuckets.Select(r => r.Map(AssetDatabase.AssetPathToGUID));
        }

        static void ReplaceInAssetFiles(Bucket files, IEnumerable<Bucket> replacements) {
            var fileContent = File.ReadAllText(files.source);
            for (int i = 0; i < files.destinations.Length; ++i)
                File.WriteAllText(
                    files.destinations[i],
                    replacements.Aggregate(
                        fileContent,
                        (content, repl) => content.Replace(repl.source, repl.destinations[i])));
        }
    }
}
