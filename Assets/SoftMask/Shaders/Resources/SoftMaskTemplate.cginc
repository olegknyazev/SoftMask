// Template for SoftMask.shader and SoftMaskETC1.shader.
// The SOFTMASK_ETC1 macro defines whether alpha split texture should be supported.

#include "UnityCG.cginc"
#include "UnityUI.cginc"
#include "SoftMask.cginc"

    struct appdata_t
    {
        float4 vertex : POSITION;
        float4 color : COLOR;
        float2 texcoord : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct v2f
    {
        float4 vertex : SV_POSITION;
        fixed4 color : COLOR;
        float2 texcoord : TEXCOORD0;
        float4 worldPosition : TEXCOORD1;
    #if UNITY_VERSION >= 202000
        half4  mask : TEXCOORD2;
        SOFTMASK_COORDS(3)
    #endif
        UNITY_VERTEX_OUTPUT_STEREO
    #if UNITY_VERSION < 202000
        SOFTMASK_COORDS(2)
    #endif
    };

    fixed4 _Color;
    fixed4 _TextureSampleAdd;
    float4 _ClipRect;

    sampler2D _MainTex;
#ifdef SOFTMASK_ETC1
    sampler2D _AlphaTex;
#endif
    float4 _MainTex_ST;
#if UNITY_VERSION >= 202100
    float _UIMaskSoftnessX;
    float _UIMaskSoftnessY;
#elif UNITY_VERSION >= 202000
    float _MaskSoftnessX;
    float _MaskSoftnessY;
#endif

    v2f vert(appdata_t IN)
    {
        v2f OUT;

        UNITY_SETUP_INSTANCE_ID(IN);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
        OUT.worldPosition = IN.vertex;

#if UNITY_VERSION >= 202000
        float4 vPosition = UnityObjectToClipPos(IN.vertex);
        OUT.vertex = vPosition;

        float2 pixelSize = vPosition.w;
        pixelSize /= float2(1, 1) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

        float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
        float2 maskUV = (IN.vertex.xy - clampedRect.xy) / (clampedRect.zw - clampedRect.xy);
        OUT.texcoord = float4(IN.texcoord.x, IN.texcoord.y, maskUV.x, maskUV.y);
    #if UNITY_VERSION >= 202100
        half2 maskSoftness = half2(_UIMaskSoftnessX, _UIMaskSoftnessY);
    #else
        half2 maskSoftness = half2(_MaskSoftnessX, _MaskSoftnessY);
    #endif
        OUT.mask = half4(IN.vertex.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * maskSoftness + abs(pixelSize.xy)));
#else
        OUT.vertex = UnityObjectToClipPos(IN.vertex);

        OUT.texcoord = TRANSFORM_TEX(IN.texcoord, _MainTex);
#endif

        OUT.color = IN.color * _Color;
        SOFTMASK_CALCULATE_COORDS(OUT, IN.vertex)
        return OUT;
    }

    fixed4 frag(v2f IN) : SV_Target
    {
    #if UNITY_VERSION >= 202120
        const half alphaPrecision = half(0xff);
        const half invAlphaPrecision = half(1.0 / alphaPrecision);
        IN.color.a = round(IN.color.a * alphaPrecision) * invAlphaPrecision;
    #endif
        
    #ifdef SOFTMASK_ETC1
        half4 color = UnityGetUIDiffuseColor(IN.texcoord, _MainTex, _AlphaTex, _TextureSampleAdd) * IN.color;
    #else
        half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
    #endif

        color.a *= SOFTMASK_GET_MASK(IN);

#if defined(UNITY_UI_CLIP_RECT)
    #if UNITY_VERSION >= 202000
        half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
        color.a *= m.x * m.y;
    #else
        color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
    #endif
#endif

    #if defined(UNITY_UI_ALPHACLIP)
        clip(color.a - 0.001);
    #endif
    #if defined(SOFTMASK_PREMULTIPLIED_ALPHA)
        color.rgb *= color.a;
    #endif

        return color;
    }

// UNITY_SHADER_NO_UPGRADE
