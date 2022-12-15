using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SoftMasking.TextMeshPro.Editor {
    public static class ReferenceShaders {
        [MenuItem("Tools/Soft Mask/Reference Shaders/Generate")]
        public static void Generate() {
            foreach (var shader in IterateShaders()) {
                shader.WriteReference(ShaderPatcher.Patch(shader.ReadExample()));
                AssetDatabase.ImportAsset(shader.referencePath);
            }
        }

        [MenuItem("Tools/Soft Mask/Reference Shaders/Test")]
        public static void Compare() {
            int total = 0;
            int failed = 0;
            foreach (var shader in IterateShaders()) {
                var actual = ShaderPatcher.Patch(shader.ReadExample());
                var expected = shader.ReadReference();
                if (actual != expected) {
                    Debug.LogErrorFormat("Patched shader {0} doesn't match the reference", shader.examplePath);
                    ++failed;
                }
                ++total;
            }
            if (failed > 0)
                Debug.LogErrorFormat("{0} of {1} shaders failed check", failed, total);
            else
                Debug.LogFormat("Patched shaders match references. {0} shaders tested.", total);
        }

        [MenuItem("Tools/Soft Mask/Reference Shaders/Test", true)]
        public static bool CompareAvailable() {
            return IterateShaders().All(x => x.referenceExists);
        }

        class ReferenceShader {
            public ReferenceShader(string examplePath) {
                this.examplePath = examplePath;
                this.referencePath =
                    examplePath
                        .Replace("Examples", "References")
                        .Replace(".txt", ".reference.txt");
            }

            public string examplePath { get; }
            public string referencePath { get; }
            public bool referenceExists => File.Exists(referencePath);

            public void WriteReference(string text) { File.WriteAllText(referencePath, text); }
            public string ReadReference() { return File.ReadAllText(referencePath); }
            public string ReadExample() { return File.ReadAllText(examplePath); }
        }

        static IEnumerable<ReferenceShader> IterateShaders() {
            foreach (var guid in AssetDatabase.FindAssets(
                    "t:TextAsset",
                    new[] {"Assets/Extra/Test/TMProReferenceShaders/Examples"})) {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                yield return new ReferenceShader(path);
            }
        }
    }
}
