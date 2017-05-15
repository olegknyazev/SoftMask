using UnityEngine;

namespace SoftMasking.Extensions {
    public static class MaterialOps {
        public static bool SupportsSoftMask(this Material mat) {
            return mat.HasProperty("_SoftMask");
        }

        public static bool HasDefaultUIShader(this Material mat) {
            return mat.shader == Canvas.GetDefaultCanvasMaterial().shader;
        }
#if UNITY_5_4_OR_NEWER
        public static bool HasDefaultETC1UIShader(this Material mat) {
            return mat.shader == Canvas.GetETC1SupportedCanvasMaterial().shader;
        }
#endif

        public static void EnableKeyword(this Material mat, string keyword, bool enabled) {
            if (enabled)
                mat.EnableKeyword(keyword);
            else
                mat.DisableKeyword(keyword);
        }
    }
}
