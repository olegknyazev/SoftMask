#ifndef SOFTMASK_INCLUDED
#define SOFTMASK_INCLUDED

#include "UnityUI.cginc"

#if defined(SOFTMASK_SLICED) || defined(SOFTMASK_TILED)
#   define __SOFTMASK_USE_BORDER
#endif

    sampler2D _SoftMask;
    float4 _SoftMask_Rect;
    float4 _SoftMask_UVRect;
    float4x4 _SoftMask_WorldToMask;
#ifdef __SOFTMASK_USE_BORDER
    float4 _SoftMask_BorderRect;
    float4 _SoftMask_UVBorderRect;
#endif
#ifdef SOFTMASK_TILED
    float2 _SoftMask_TileRepeat;
#endif

    inline float2 __SoftMask_Inset(float2 a, float2 a1, float2 a2, float2 u1, float2 u2, float2 repeat) {
        return frac((a - a1) / (a2 - a1) * repeat) * (u2 - u1) + u1;
    }

    inline float2 __SoftMask_Inset(float2 a, float2 a1, float2 a2, float2 u1, float2 u2) {
        return (a - a1) / (a2 - a1) * (u2 - u1) + u1;
    }

#if defined(SOFTMASK_SLICED) || defined(SOFTMASK_TILED)
#   if SOFTMASK_TILED
#       define __SOFTMASK_REPEAT , _SoftMask_TileRepeat
#   else
#       define __SOFTMASK_REPEAT
#   endif
    inline float2 __SoftMask_XY2UV(
            float2 a, 
            float2 a1, float2 a2, float2 a3, float2 a4, 
            float2 u1, float2 u2, float2 u3, float2 u4) {
        float2 s1 = step(a2, a);
        float2 s2 = step(a3, a);
        float2 s1i = 1 - s1;
        float2 s2i = 1 - s2;
        return __SoftMask_Inset(a, a1, a2, u1, u2)                   * s1i * s2i
             + __SoftMask_Inset(a, a2, a3, u2, u3 __SOFTMASK_REPEAT) * s1  * s2i
             + __SoftMask_Inset(a, a3, a4, u3, u4)                   * s1  * s2;
    }

    float2 SoftMask_GetMaskUV(float2 maskPosition) {
        return
            __SoftMask_XY2UV(
                maskPosition,
                _SoftMask_Rect.xy, _SoftMask_BorderRect.xy, _SoftMask_BorderRect.zw, _SoftMask_Rect.zw,
                _SoftMask_UVRect.xy, _SoftMask_UVBorderRect.xy, _SoftMask_UVBorderRect.zw, _SoftMask_UVRect.zw);
    }
#   undef REPEAT
#else
    float2 SoftMask_GetMaskUV(float2 maskPosition) {
        return 
            __SoftMask_Inset(
                maskPosition, 
                _SoftMask_Rect.xy, _SoftMask_Rect.zw, _SoftMask_UVRect.xy, _SoftMask_UVRect.zw);
    }
#endif

    // Samples mask texture at given world position. It may be useful for debugging.
    float4 SoftMask_GetMaskTexture(float2 maskPosition) {
        return tex2D(_SoftMask, SoftMask_GetMaskUV(maskPosition));
    }

    float SoftMask_GetMask(float2 maskPosition) {
        float2 uv = SoftMask_GetMaskUV(maskPosition);
        return tex2D(_SoftMask, uv).a * UnityGet2DClipping(maskPosition, _SoftMask_Rect);
    }
#endif
