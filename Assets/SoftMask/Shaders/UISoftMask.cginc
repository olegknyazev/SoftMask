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
#   define __SOFTMASK_REPEAT_PARAM , float repeat
#   define __SOFTMASK_REPEAT_ARG , repeat
#   define __SOFTMASK_REPEAT_NULL , 1
#   define __SOFTMASK_REPEAT_APPLY(x) frac((x) * repeat)
#   define __SOFTMASK_REPEAT_SEED(field) , _SoftMask_TileRepeat.field
#else
#   define __SOFTMASK_REPEAT_PARAM 
#   define __SOFTMASK_REPEAT_ARG 
#   define __SOFTMASK_REPEAT_NULL 
#   define __SOFTMASK_REPEAT_APPLY(x) (x) 
#   define __SOFTMASK_REPEAT_SEED(field)
#endif

    inline float __SoftMask_Inset(float x, float x1, float x2, float u1, float u2 __SOFTMASK_REPEAT_PARAM) {
        return __SOFTMASK_REPEAT_APPLY((x - x1) / (x2 - x1)) * (u2 - u1) + u1;
    }

#ifdef __SOFTMASK_USE_BORDER
    inline float __SoftMask_InsetWithBorder(float x, float x1, float x2, float x3, float x4, float u1, float u2, float u3, float u4 __SOFTMASK_REPEAT_PARAM) {
        if (x < x2)
            return __SoftMask_Inset(x, x1, x2, u1, u2 __SOFTMASK_REPEAT_NULL);
        else if (x < x3)
            return __SoftMask_Inset(x, x2, x3, u2, u3 __SOFTMASK_REPEAT_ARG);
        else
            return __SoftMask_Inset(x, x3, x4, u3, u4 __SOFTMASK_REPEAT_NULL);
    }
#endif

#if defined(SOFTMASK_SLICED) || defined(SOFTMASK_TILED)
    float2 SoftMask_GetMaskUV(float2 maskPosition) {
        return
            float2(
                __SoftMask_InsetWithBorder(
                    maskPosition.x,
                    _SoftMask_Rect.x, _SoftMask_BorderRect.x, _SoftMask_BorderRect.z, _SoftMask_Rect.z,
                    _SoftMask_UVRect.x, _SoftMask_UVBorderRect.x, _SoftMask_UVBorderRect.z, _SoftMask_UVRect.z
                    __SOFTMASK_REPEAT_SEED(x)),
                __SoftMask_InsetWithBorder(
                    maskPosition.y,
                    _SoftMask_Rect.y, _SoftMask_BorderRect.y, _SoftMask_BorderRect.w, _SoftMask_Rect.w,
                    _SoftMask_UVRect.y, _SoftMask_UVBorderRect.y, _SoftMask_UVBorderRect.w, _SoftMask_UVRect.w
                    __SOFTMASK_REPEAT_SEED(y)));
    }
#else
    float2 SoftMask_GetMaskUV(float2 maskPosition) {
        return 
            float2(
                __SoftMask_Inset(maskPosition.x, _SoftMask_Rect.x, _SoftMask_Rect.z, _SoftMask_UVRect.x, _SoftMask_UVRect.z),
                __SoftMask_Inset(maskPosition.y, _SoftMask_Rect.y, _SoftMask_Rect.w, _SoftMask_UVRect.y, _SoftMask_UVRect.w));
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
