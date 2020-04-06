using UnityEngine;

namespace SoftMasking.Tests {
    public static class RectMethods {
        public static Rect Move(this Rect rect, Vector2 offset) {
            return new Rect(rect.position + offset, rect.size);
        }

        public static Rect Rect(this Texture2D texture) {
            return new Rect(0f, 0f, texture.width, texture.height);
        }

        public static Vector2 Size(this Texture2D texture) {
            return new Vector2(texture.width, texture.height);
        }
        
        public static bool IsEmpty(this Rect rect) {
            return rect.width == 0f || rect.height == 0f;
        }
        
        public static Rect ClampToSize(this Rect rect, Vector2 size) {
            return UnityEngine.Rect.MinMaxRect(
                Mathf.Max(rect.xMin, 0f),
                Mathf.Max(rect.yMin, 0f),
                Mathf.Min(rect.xMax, size.x),
                Mathf.Min(rect.yMax, size.y));
        }
        
        public static Rect Round(this Rect rect) {
            var result = rect;
            result.xMin = Mathf.Round(rect.xMin);
            result.yMin = Mathf.Round(rect.yMin);
            result.xMax = Mathf.Round(rect.xMax);
            result.yMax = Mathf.Round(rect.yMax);
            return result;
        }
    }
}
