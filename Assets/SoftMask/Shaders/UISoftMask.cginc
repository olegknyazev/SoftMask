#ifndef SOFTMASK_INCLUDED
#define SOFTMASK_INCLUDED

#pragma multi_compile __ SOFTMASK_USE_BORDER

    sampler2D _SoftMask;
    float4 _SoftMask_Rect;
    float4 _SoftMask_UVRect;
#if SOFTMASK_USE_BORDER
    float4 _SoftMask_BorderRect;
    float4 _SoftMask_UVBorderRect;
#endif

    float __SoftMask_Inset(float x, float x1, float x2, float u1, float u2) {
        return (x - x1) / (x2 - x1) * (u2 - u1) + u1;
    }

#if SOFTMASK_USE_BORDER
    float __SoftMask_InsetWithBorder(float x, float x1, float x2, float x3, float x4, float u1, float u2, float u3, float u4) {
        if (x < x2)
            return __SoftMask_Inset(x, x1, x2, u1, u2);
        else if (x < x3)
            return __SoftMask_Inset(x, x2, x3, u2, u3);
        else
            return __SoftMask_Inset(x, x3, x4, u3, u4);
    }

    float2 SoftMask_GetMaskUV(float2 worldPosition) {
        return
            float2(
                __SoftMask_InsetWithBorder(
                    worldPosition.x,
                    _SoftMask_Rect.x, _SoftMask_BorderRect.x, _SoftMask_BorderRect.z, _SoftMask_Rect.z,
                    _SoftMask_UVRect.x, _SoftMask_UVBorderRect.x, _SoftMask_UVBorderRect.z, _SoftMask_UVRect.z),
                __SoftMask_InsetWithBorder(
                    worldPosition.y,
                    _SoftMask_Rect.y, _SoftMask_BorderRect.y, _SoftMask_BorderRect.w, _SoftMask_Rect.w,
                    _SoftMask_UVRect.y, _SoftMask_UVBorderRect.y, _SoftMask_UVBorderRect.w, _SoftMask_UVRect.w));
    }
#else
    float2 SoftMask_GetMaskUV(float2 worldPosition) {
        return 
            float2(
                __SoftMask_Inset(worldPosition.x, _SoftMask_Rect.x, _SoftMask_Rect.z, _SoftMask_UVRect.x, _SoftMask_UVRect.z),
                __SoftMask_Inset(worldPosition.y, _SoftMask_Rect.y, _SoftMask_Rect.w, _SoftMask_UVRect.y, _SoftMask_UVRect.w));
    }
#endif

    // Samples mask texture at given world position. It may be useful for debugging.
    float4 SoftMask_GetMaskTexture(float2 worldPosition) {
        return tex2D(_SoftMask, SoftMask_GetMaskUV(worldPosition));
    }

    float SoftMask_GetMask(float2 worldPosition) {
        return SoftMask_GetMaskTexture(worldPosition).a;
    }
#endif
