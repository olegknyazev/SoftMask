// Template for SoftMask.shader and SoftMaskETC1.shader.
// SOFTMASK_ETC1 define determines whether alpha split texture should be supported.

#include "UnityCG.cginc"
#include "UnityUI.cginc"
#include "SoftMask.cginc"

    struct appdata_t
    {
        float4 vertex : POSITION;
        float4 color : COLOR;
        float2 texcoord : TEXCOORD0;
#if UNITY_VERSION >= 550
        UNITY_VERTEX_INPUT_INSTANCE_ID
#endif
    };

    struct v2f
    {
        float4 vertex : SV_POSITION;
        fixed4 color : COLOR;
        half2 texcoord : TEXCOORD0;
        float4 worldPosition : TEXCOORD1;
#if UNITY_VERSION >= 550
        UNITY_VERTEX_OUTPUT_STEREO
#endif
        SOFTMASK_COORDS(2)
    };

    fixed4 _Color;
    fixed4 _TextureSampleAdd;
    float4 _ClipRect;

    v2f vert(appdata_t IN)
    {
        v2f OUT;
#if UNITY_VERSION >= 550
        UNITY_SETUP_INSTANCE_ID(IN);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
#endif
        OUT.worldPosition = IN.vertex;
#if UNITY_VERSION >= 540
        OUT.vertex = UnityObjectToClipPos(IN.vertex);
#else
        OUT.vertex = mul(UNITY_MATRIX_MVP, IN.vertex);
#endif

        OUT.texcoord = IN.texcoord;

#if UNITY_VERSION < 550
#  ifdef UNITY_HALF_TEXEL_OFFSET
        OUT.vertex.xy += (_ScreenParams.zw - 1.0) * float2(-1, 1);
#  endif
#endif

        OUT.color = IN.color * _Color;
        SOFTMASK_CALCULATE_COORDS(OUT, IN.vertex)
        return OUT;
    }

    sampler2D _MainTex;
#ifdef SOFTMASK_ETC1
    sampler2D _AlphaTex;
#endif

    fixed4 frag(v2f IN) : SV_Target
    {
#ifdef SOFTMASK_ETC1
        half4 color = UnityGetUIDiffuseColor(IN.texcoord, _MainTex, _AlphaTex, _TextureSampleAdd) * IN.color;
#else
        half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
#endif

        color.a *= SOFTMASK_GET_MASK(IN);

#if defined(UNITY_UI_CLIP_RECT) || UNITY_VERSION < 201720
        color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
#endif

#if defined(UNITY_UI_ALPHACLIP)
        clip(color.a - 0.001);
#endif

        return color;
    }

// UNITY_SHADER_NO_UPGRADE
