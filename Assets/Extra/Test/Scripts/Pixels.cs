using System.Linq;
using UnityEngine;

namespace SoftMasking.Tests {
    public class Pixels {
        Color[] _colors;
        int _width;

        public Pixels(Color[] data, int width) {
            _colors = data;
            _width = width;
        }

        public static Pixels FromTextureArea(Texture2D texture, Rect rect) {
            var r = rect.ClampToSize(texture.Size());
            return new Pixels(
                texture.GetPixels((int)r.xMin, (int)r.yMin, (int)r.width, (int)r.height), 
                (int)r.width);
        }

        public int width => _width;
        public int height => _colors.Length / width;

        public Color this[int idx] => _colors[idx];
        public Color this[int x, int y] => _colors[y * _width + x];

        public override bool Equals(object obj) {
            var other = obj as Pixels;
            return other != null ? this == other : false;
        }

        public override int GetHashCode() {
            return _colors.GetHashCode();
        }

        public static bool operator==(Pixels left, Pixels right) {
            return Enumerable.SequenceEqual(left._colors, right._colors);
        }

        public static bool operator!=(Pixels left, Pixels right) {
            return !(left == right);
        }

        public float[] Compare(Pixels other) {
            if (_colors.Length != other._colors.Length)
                return null;
            var result = new float[_colors.Length];
            for (int i = 0; i < _colors.Length; ++i)
                result[i] = Distance(_colors[i], other._colors[i]);
            return result;
        }

        static float Distance(Color left, Color right) {
            var r = left.r - right.r;
            var g = left.g - right.g;
            var b = left.b - right.b;
            return Mathf.Sqrt(r * r + g * g + b * b);
        }
    }
}
