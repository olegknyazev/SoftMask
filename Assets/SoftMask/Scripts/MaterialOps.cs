using UnityEngine;

namespace SoftMask.Extensions {
    public static class MaterialOps {
        public static bool SupportsSoftMask(this Material mat) {
            return mat.HasProperty("_SoftMask");
        }
        public static bool HasDefaultUIShader(this Material mat) {
            return mat.shader == Canvas.GetDefaultCanvasMaterial().shader;
        }
    }
}
