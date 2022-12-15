using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SoftMasking.Tests {
    [Serializable]
    public class ScreenValidationRule {
        [SerializeField] public Rect validationRect;
        [SerializeField] public TextAnchor validationRectAnchor;
        [SerializeField, Range(0, 2)] public float tolerance = 0.01f;

        public static readonly ScreenValidationRule topLeftWholeScreen = new ScreenValidationRule();

        public bool Validate(Texture2D expected, Texture2D actual) {
            return WithComparisonResults(
                expected,
                actual,
                (imagesAreEqual, _) => imagesAreEqual);
        }

        T WithTemporaryDirectory<T>(Func<string, T> func) {
            var dir = Path.Combine(Application.temporaryCachePath, FileUtil.GetUniqueTempPathInProject());
            Directory.CreateDirectory(dir);
            try {
                return func(dir);
            } finally {
                Directory.Delete(dir, recursive: true);
            }
        }

        void SaveAsPNG(Texture2D texture, string path, Rect validationArea) {
            var area = validationArea.ClampToSize(texture.Size());
            var subTexture = new Texture2D((int)area.width, (int)area.height, texture.format, mipChain: false);
            try {
                subTexture.SetPixels(
                    texture.GetPixels((int)area.xMin, (int)area.yMin, (int)area.width, (int)area.height));
                File.WriteAllBytes(path, subTexture.EncodeToPNG());
            } finally {
                UnityEngine.Object.Destroy(subTexture);
            }
        }

        bool PerceptuallyCompare(string firstPath, string secondPath, string outputPath) {
            using var process = Process.Start(
                "perceptualdiff",
                $@"-output ""{outputPath}"" ""{firstPath}"" ""{secondPath}""");
            process.WaitForExit();
            return process.ExitCode == 0;
        }

        public Texture2D Diff(Texture2D expected, Texture2D actual) {
            return WithComparisonResults(
                expected,
                actual,
                (_, diffPath) => {
                    if (File.Exists(diffPath)) {
                        var diff = new Texture2D(0, 0);
                        diff.LoadImage(File.ReadAllBytes(diffPath));
                        return diff;
                    } else
                        return null;
                });
        }

        T WithComparisonResults<T>(Texture2D expected, Texture2D actual, Func<bool, string, T> func) {
            return WithTemporaryDirectory(
                directory => {
                    var expectedSize = expected.Size();
                    var actualArea = AnchorAligned(actual.Rect(), expectedSize);
                    var validationArea =
                        validationRect.IsEmpty()
                            ? expected.Rect()
                            : AnchorAligned(validationRect, expectedSize);
                    var expectedPath = Path.Combine(directory, "expected.png");
                    var actualPath = Path.Combine(directory, "actual.png");
                    var diffPath = Path.Combine(directory, "diff.png");
                    SaveAsPNG(expected, expectedPath, validationArea);
                    SaveAsPNG(actual, actualPath, validationArea.Move(-actualArea.position));
                    var imagesEqual = PerceptuallyCompare(expectedPath, actualPath, diffPath);
                    return func(imagesEqual, diffPath);
                });
        }

        public Rect ValidationRect(Rect parentRect) {
            var result = AnchorAligned(validationRect, parentRect.size);
            result.position += parentRect.position;
            return result;
        }

        public void RoundRect() {
            validationRect = validationRect.Round();
        }

        Vector2 AnchorOf(Vector2 childSize, Vector2 parentSize) {
            var deltaSizeX = parentSize.x - childSize.x;
            var deltaSizeY = parentSize.y - childSize.y;
            switch (validationRectAnchor) {
                case TextAnchor.UpperLeft: return new Vector2(0f, deltaSizeY);
                case TextAnchor.UpperCenter: return new Vector2(deltaSizeX / 2f, deltaSizeY);
                case TextAnchor.UpperRight: return new Vector2(deltaSizeX, deltaSizeY);
                case TextAnchor.MiddleLeft: return new Vector2(0f, deltaSizeY / 2f);
                case TextAnchor.MiddleCenter: return new Vector2(deltaSizeX / 2f, deltaSizeY / 2f);
                case TextAnchor.MiddleRight: return new Vector2(deltaSizeX, deltaSizeY / 2f);
                case TextAnchor.LowerLeft: return new Vector2(0f, 0f);
                case TextAnchor.LowerCenter: return new Vector2(deltaSizeX / 2f, 0f);
                case TextAnchor.LowerRight: return new Vector2(deltaSizeX, 0f);
                default: return new Vector2(0f, 0f);
            }
        }

        Rect AnchorAligned(Rect childRect, Vector2 parentSize) {
            return new Rect(childRect.position + AnchorOf(childRect.size, parentSize), childRect.size);
        }
    }

    [Serializable]
    public class ScreenValidationRuleKeyValuePair {
        [SerializeField] public int startStep = 0;
        [SerializeField] public int endStep = -1;
        [SerializeField] public ScreenValidationRule rule;

        public bool MatchesIndex(int index) {
            return index >= startStep
                && (index < endStep || endStep < 0);
        }
    }
}
