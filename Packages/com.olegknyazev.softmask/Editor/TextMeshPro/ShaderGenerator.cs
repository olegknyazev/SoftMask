using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using SoftMasking.Editor;
using UnityEngine.Assertions;

namespace SoftMasking.TextMeshPro.Editor {
    public static class ShaderGenerator {
        public class ShaderResource {
            public readonly Shader shader;
            public readonly string text;
            public readonly string name;

            public ShaderResource(string path) {
                shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                text = ReadResource(path);
                name = Path.GetFileNameWithoutExtension(path);
            }
        }

        [MenuItem("Tools/Soft Mask/Update TestMesh Pro Integration")]
        public static void UpdateShaders() {
            var tmproShaders = CollectTMProShaders().ToList();
            if (tmproShaders.Count == 0) {
                Debug.LogError(
                    "Could not update integration because TextMesh Pro shaders are not found. " +
                    "Make sure that TextMesh Pro package is installed and its essential " +
                    "resources are imported (Window / TextMeshPro / Import TMP Essential Resources).");
                return;
            }
            foreach (var shader in tmproShaders) {
                try {
                    var newText = ShaderPatcher.Patch(shader.text);
                    var replacementFileName = shader.name + " - SoftMask.shader";
                    var generatedShadersPath = PackageResources.generatedShaderResourcesPath;
                    if (!Directory.Exists(generatedShadersPath))
                        Directory.CreateDirectory(generatedShadersPath);
                    var outputFile = Path.Combine(generatedShadersPath, replacementFileName);
                    File.WriteAllText(outputFile, UpdateIncludes(newText));
                    AssetDatabase.ImportAsset(outputFile);
                } catch (Exception ex) {
                    Debug.LogErrorFormat(
                        "Unable to patch TextMesh Pro shader {0}: {1}",
                        shader.name, ex);
                }
            }
            InvalidateSoftMasks();
        }

        static IEnumerable<ShaderResource> CollectTMProShaders() {
            return
                TMProShaderGUIDs.Concat(TMProShaderPackageGUIDs)
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Select(x => new ShaderResource(x))
                    .Where(x => CheckIsUIShader(x.shader));
        }

        static readonly List<string> TMProShaderGUIDs = new List<string> {
            "d1cf17907700cb647aa3ea423ba38f2e", // TMP_Bitmap-Mobile
            "edfcf888cd11d9245b91d2883049a57e", // TMP_Bitmap
            "afc255f7c2be52e41973a3d10a2e632d", // TMP_SDF-Mobile Masking
            "cafd18099dfc0114896e0a8b277b81b6", // TMP_SDF-Mobile
            "dca26082f9cb439469295791d9f76fe5", // TMP_SDF
            "3a1c68c8292caf046bd21158886c5e40"  // TMP_Sprite
        };

        static readonly List<string> TMProShaderPackageGUIDs = new List<string> {
            "48bb5f55d8670e349b6e614913f9d910", // TMP_Bitmap-Mobile-Custom-Atlas
            "1e3b057af24249748ff873be7fafee47", // TMP_Bitmap-Mobile
            "128e987d567d4e2c824d754223b3f3b0", // TMP_Bitmap
            "bc1ede39bf3643ee8e493720e4259791", // TMP_SDF-Mobile Masking
            "fe393ace9b354375a9cb14cdbbc28be4", // TMP_SDF-Mobile
            "68e6db2ebdc24f95958faec2be5558d6", // TMP_SDF
            "cf81c85f95fe47e1a27f6ae460cf182c"  // TMP_Sprite
        };

        static readonly Dictionary<string, List<string>> KnownIncludeGUIDs = new Dictionary<string, List<string>> {
            { "TMPro_Properties.cginc", new List<string> { 
                "bc2d34f37efcbdf429ed46cb34aa2ad5",
                "3997e2241185407d80309a82f9148466"} },
            { "TMPro.cginc", new List<string> { 
                "438defe6a2827704f90bdf852732bc11",
                "407bc68d299748449bbf7f48ee690f8d"} },
            // We do not have to use absolute path for SoftMask.cginc because patched shaders
            // reside in a subfolder but it's convenient to reuse mechanism made for TMPro includes.
            { "SoftMask.cginc", new List<string> {
                "0f47072ab362848c2b950a1cdd7c45e5" } }
        };

        static Dictionary<string, string> s_knownIncludes;
        static Dictionary<string, string> knownIncludes {
            get {
                if (s_knownIncludes == null)
                    s_knownIncludes = 
                        KnownIncludeGUIDs
                            .ToDictionary(
                                kv => kv.Key,
                                kv => kv.Value
                                    .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                                    .First(x => !string.IsNullOrEmpty(x)));
                return s_knownIncludes;
            }
        }

        static string UpdateInclude(string includePath) {
            string result;
            return
                knownIncludes.TryGetValue(Path.GetFileName(includePath), out result)
                    ? result
                    : includePath;
        }

        static string UpdateIncludes(string shaderCode) {
            return
                Regex.Replace(
                    shaderCode,
                    "#include \"(.+)\"",
                    match => string.Format(
                        "#include \"{0}\"",
                        UpdateInclude(match.Groups[1].Value)));
        }
        
        static bool CheckIsUIShader(Shader shader) {
            var material = new Material(shader) {
                hideFlags = HideFlags.HideAndDontSave
            };
            var result = material.HasProperty(Ids.Stencil);
            UnityEngine.Object.DestroyImmediate(material);
            return result;
        }

        static string ReadResource(string path) { return File.ReadAllText(path); }

        static void InvalidateSoftMasks() {
            var softMaskPath = AssetDatabase.GUIDToAssetPath(PackageResources.SoftMaskCsGUID);
            Assert.IsFalse(string.IsNullOrEmpty(softMaskPath));
            AssetDatabase.ImportAsset(softMaskPath);
        }

        static class Ids {
            public static readonly int Stencil = Shader.PropertyToID("_Stencil");
        }
    }
}
