#ifndef SOFTMASK_INCLUDED
#define SOFTMASK_INCLUDED

#include "UnityUI.cginc"

/*  API Reference
    -------------

    #define SOFTMASK_COORDS(idx)
        Use it in a structure that is passed from vertex to fragment shader.
          idx    Number of interpolator to use. Specify first free TEXCOORD index.

    #define SOFTMASK_CALCULATE_COORDS(OUT, pos)
        Use it in a vertex shader to calculate mask-related data.
          pos    Source vertex position that was passed to vertex shader
          OUT    Instance of an output structure that will be passed to fragment shader.
                 It should be of type to which SOFTMASK_COORDS() was added.

    #define SOFTMASK_GET_MASK(IN)
        Use it in a fragment shader to get mask value for current pixel.
          IN     Instance of an vertex shader output structure, for which SOFTMASK_COORDS()
                 was defined.

    inline float SoftMask_GetMask(float2 maskPosition)
        Returns a mask value for a given pixel.
          maskPosition   Position of current pixel in mask's local space.
                         To get this position use macro SOFTMASK_CALCULATE_COORDS().

    inline float4 SoftMask_GetMaskTexture(float2 maskPosition)
        Returns a color of mask texture for a given pixel. maskPosition is the same
        as in SoftMask_GetMask(). This function returns original pixel of the mask,
        which may be useful for debugging.                 
*/

#if defined(SOFTMASK_SIMPLE) || defined(SOFTMASK_SLICED) || defined(SOFTMASK_TILED)
#   define __SOFTMASK_ENABLE
#   if defined(SOFTMASK_SLICED) || defined(SOFTMASK_TILED)
#       define __SOFTMASK_USE_BORDER
#   endif
#endif

#ifdef __SOFTMASK_ENABLE

# define SOFTMASK_COORDS(idx)                  float4 maskPosition : TEXCOORD ## idx;
# define SOFTMASK_CALCULATE_COORDS(OUT, pos)   (OUT).maskPosition = mul(_SoftMask_WorldToMask, pos);
# define SOFTMASK_GET_MASK(IN)                 SoftMask_GetMask((IN).maskPosition.xy)

    sampler2D _SoftMask;
    float4 _SoftMask_Rect;
    float4 _SoftMask_UVRect;
    float4x4 _SoftMask_WorldToMask;
    float4 _SoftMask_ChannelWeights;
# ifdef __SOFTMASK_USE_BORDER
    float4 _SoftMask_BorderRect;
    float4 _SoftMask_UVBorderRect;
# endif
# ifdef SOFTMASK_TILED
    float2 _SoftMask_TileRepeat;
# endif

    // On changing logic of the following functions, don't forget to update
    // according functions in SoftMask.MaterialParameters (C#).
    
    inline float2 __SoftMask_Inset(float2 a, float2 a1, float2 a2, float2 u1, float2 u2, float2 repeat) {
        return frac((a - a1) / (a2 - a1) * repeat) * (u2 - u1) + u1;
    }

    inline float2 __SoftMask_Inset(float2 a, float2 a1, float2 a2, float2 u1, float2 u2) {
        return (a - a1) / (a2 - a1) * (u2 - u1) + u1;
    }

    // Anti-aliased version of UnityGet2DClipping()
    inline float __SoftMask_Get2DClippingAntialiased(in float2 position, in float4 clipRect)
    {
        float2 inside = saturate(position.xy - clipRect.xy) * saturate(clipRect.zw - position.xy);
        return inside.x * inside.y;
    }

# ifdef __SOFTMASK_USE_BORDER
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

    inline float2 SoftMask_GetMaskUV(float2 maskPosition) {
        return
            __SoftMask_XY2UV(
                maskPosition,
                _SoftMask_Rect.xy, _SoftMask_BorderRect.xy, _SoftMask_BorderRect.zw, _SoftMask_Rect.zw,
                _SoftMask_UVRect.xy, _SoftMask_UVBorderRect.xy, _SoftMask_UVBorderRect.zw, _SoftMask_UVRect.zw);
    }
#   undef __SOFTMASK_REPEAT
# else
    inline float2 SoftMask_GetMaskUV(float2 maskPosition) {
        return 
            __SoftMask_Inset(
                maskPosition, 
                _SoftMask_Rect.xy, _SoftMask_Rect.zw, _SoftMask_UVRect.xy, _SoftMask_UVRect.zw);
    }
# endif
    inline float4 SoftMask_GetMaskTexture(float2 maskPosition) {
        return tex2D(_SoftMask, SoftMask_GetMaskUV(maskPosition));
    }

    inline float SoftMask_GetMask(float2 maskPosition) {
        float2 uv = SoftMask_GetMaskUV(maskPosition);
        float4 mask = tex2D(_SoftMask, uv) * _SoftMask_ChannelWeights;
        return (mask.r + mask.g + mask.b + mask.a) * __SoftMask_Get2DClippingAntialiased(maskPosition, _SoftMask_Rect);
    }
#else // __SOFTMASK_ENABLED

# define SOFTMASK_COORDS(idx)                  
# define SOFTMASK_CALCULATE_COORDS(OUT, pos)   
# define SOFTMASK_GET_MASK(IN)                 (1.0f)

    inline float4 SoftMask_GetMaskTexture(float2 maskPosition) { return 1.0f; }
    inline float SoftMask_GetMask(float2 maskPosition) { return 1.0f;  }
#endif

#endif
