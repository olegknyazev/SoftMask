Shader "UI/Soft Mask/Custom Shader with Soft Mask support"
{
    // This is an example of UI shader with Soft Mask support added. All places where
    // something related to Soft Mask support was added marked with comment
    // 'Soft Mask Support'.

    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)

        _Amplitude("Amplitude", Float) = 0.25
        _Waves("Waves", Float) = 2.0

        _StencilComp("Stencil Comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255

        _ColorMask("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip", Float) = 0

        // Soft Mask support
        // Soft Mask determines that shader supports soft masking by presence of this property.
        [PerRendererData] _SoftMask("Mask", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref[_Stencil]
            Comp[_StencilComp]
            Pass[_StencilOp]
            ReadMask[_StencilReadMask]
            WriteMask[_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest[unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask[_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            // Soft Mask Support
            // You also can use full path (Assets/...)
            #include "../../Shaders/Resources/SoftMask.cginc"

            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            // Soft Mask Support
            #pragma multi_compile __ SOFTMASK_SIMPLE SOFTMASK_SLICED SOFTMASK_TILED

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                half2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                // Soft Mask Support
                // The number in braces determines what TEXCOORDn Soft Mask may use
                // (it required only one TEXCOORD).
                SOFTMASK_COORDS(2)
            };

            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            float _Amplitude;
            float _Waves;

            inline float2 Wave(float2 uv)
            {
                return float2(uv.x, uv.y + sin(uv.x * _Waves + _Time.w) * _Amplitude);
            }

            inline float SmoothedClip(float2 position, float4 clipRect)
            {
                float2 inside = saturate((position.xy - clipRect.xy) * 500) * saturate((clipRect.zw - position.xy) * 200);
                return inside.x * inside.y;
            }

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.worldPosition = IN.vertex;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
            #ifdef UNITY_HALF_TEXEL_OFFSET
                OUT.vertex.xy += (_ScreenParams.zw - 1.0) * float2(-1, 1);
            #endif
                OUT.color = IN.color * _Color;
                SOFTMASK_CALCULATE_COORDS(OUT, IN.vertex) // Soft Mask Support
                return OUT;
            }

            sampler2D _MainTex;

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = Wave(IN.texcoord);
                half4 tex = tex2D(_MainTex, uv);
                tex.a *= SmoothedClip(uv, float4(0, 0, 1, 1));
                half4 color = (tex + _TextureSampleAdd) * IN.color;

                color.a *= SOFTMASK_GET_MASK(IN); // Soft Mask Support
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);

            #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
            #endif

                return color;
            }

            ENDCG
        }
    }
}

// UNITY_SHADER_NO_UPGRADE
