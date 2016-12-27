using UnityEngine;

namespace SoftMask.Extensions {
    public static class MaterialOps {
        public static bool SupportsSoftMask(this Material mat) {
            return mat.HasProperty("_SoftMask");
        }

        public static bool HasDefaultUIShader(this Material mat) {
            return mat.shader == Canvas.GetDefaultCanvasMaterial().shader;
        }

        public static void EnableKeyword(this Material mat, string keyword, bool enabled) {
            if (enabled)
                mat.EnableKeyword(keyword);
            else
                mat.DisableKeyword(keyword);
        }
    }
}
