using System;
using System.Linq;
using UnityEngine;

namespace SoftMasking.Tests {
    [Serializable] public class ScreenValidationRule {
        [SerializeField] public Rect validationRect;
        [SerializeField] public TextAnchor validationRectAnchor;
        [SerializeField, Range(0, 2)] public float tolerance = 0.01f;

        public static readonly ScreenValidationRule topLeftWholeScreen = new ScreenValidationRule();

        public bool Validate(Texture2D expected, Texture2D actual) {
            Pixels expectedPixels;
            Pixels actualPixels;
            ReadValidationAreas(expected, actual, out expectedPixels, out actualPixels);
            var diff = expectedPixels.Compare(actualPixels);
            return diff != null && diff.All(x => x <= tolerance);
        }

        public Texture2D Diff(Texture2D expected, Texture2D actual) {
            Pixels expectedPixels;
            Pixels actualPixels;
            ReadValidationAreas(expected, actual, out expectedPixels, out actualPixels);
            var diff = expectedPixels.Compare(actualPixels);
            var result = new Texture2D(expectedPixels.width, expectedPixels.height, TextureFormat.RGB24, false);
            var pixels = new Color[diff.Length];
            for (int i = 0; i < diff.Length; ++i)
                pixels[i] = diff[i] <= tolerance ? expectedPixels[i] : Color.red;
            result.SetPixels(pixels);
            result.Apply();
            return result;
        }

        void ReadValidationAreas(
                Texture2D expected, 
                Texture2D actual, 
                out Pixels expectedPixels, 
                out Pixels actualPixels) {
            var expectedSize = expected.Size();
            var actualArea = AnchorAligned(actual.Rect(), expectedSize);
            var validationArea =
                validationRect.IsEmpty()
                    ? expected.Rect()
                    : AnchorAligned(validationRect, expectedSize);
            expectedPixels = Pixels.FromTextureArea(expected, validationArea);
            actualPixels = Pixels.FromTextureArea(actual, validationArea.Move(-actualArea.position));
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
    
    [Serializable] public class ScreenValidationRuleKeyValuePair {
        [SerializeField] public int startStep = 0;
        [SerializeField] public int endStep = -1;
        [SerializeField] public ScreenValidationRule rule;

        public bool MatchesIndex(int index) {
            return index >= startStep
                && (index < endStep || endStep < 0);
        }
    }
}
