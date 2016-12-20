#ifndef SOFTMASK_INCLUDED
#define SOFTMASK_INCLUDED

    sampler2D _SoftMask;
    float4 _SoftMask_Rect;
    float4 _SoftMask_BorderRect;
    float4 _SoftMask_UVRect;
    float4 _SoftMask_UVBorderRect;

    float __SoftMask_ThreeStageTransform(float x, float x1, float x2, float x3, float x4, float u1, float u2, float u3, float u4) {
        if (x < x2)
            return (x - x1) / (x2 - x1) * (u2 - u1) + u1;
        else if (x < x3)
            return (x - x2) / (x3 - x2) * (u3 - u2) + u2;
        else
            return (x - x3) / (x4 - x3) * (u4 - u3) + u3;
    }

    float2 SoftMask_GetMaskUV(float2 worldPosition) {
        return
            float2(
                __SoftMask_ThreeStageTransform(
                    worldPosition.x,
                    _SoftMask_Rect.x, _SoftMask_BorderRect.x, _SoftMask_BorderRect.z, _SoftMask_Rect.z,
                    _SoftMask_UVRect.x, _SoftMask_UVBorderRect.x, _SoftMask_UVBorderRect.z, _SoftMask_UVRect.z),
                __SoftMask_ThreeStageTransform(
                    worldPosition.y,
                    _SoftMask_Rect.y, _SoftMask_BorderRect.y, _SoftMask_BorderRect.w, _SoftMask_Rect.w,
                    _SoftMask_UVRect.y, _SoftMask_UVBorderRect.y, _SoftMask_UVBorderRect.w, _SoftMask_UVRect.w));
    }

    // Samples mask texture at given world position. It may be useful for debugging.
    float4 SoftMask_GetMaskTexture(float2 worldPosition) {
        return tex2D(_SoftMask, SoftMask_GetMaskUV(worldPosition));
    }

    float SoftMask_GetMask(float2 worldPosition) {
        return SoftMask_GetMaskTexture(worldPosition).a;
    }
#endif
