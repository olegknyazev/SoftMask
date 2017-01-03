Shader "Hidden/UI Default (Soft Masked)"
{
    // It is a standart UI shader with Soft Mask support added. You can use it as a guide to
    // implement your own shaders that supports Soft Mask. All places where something should 
    // be added to Soft Mask to work are marked with comment 'Soft Mask Support'.

    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)

        // Soft Mask support
        // This block isn't required. You may not include it in your material if you don't
        // want to modify these properties from Unity editor (you normally wont modify them).
        _SoftMask("Mask", 2D) = "white" {}
        _SoftMask_Rect("Mask Rect", Vector) = (0,0,0,0)
        _SoftMask_UVRect("Mask UV Rect", Vector) = (0,0,1,1)
        _SoftMask_BorderRect("Mask Border", Vector) = (0,0,0,0)
        _SoftMask_UVBorderRect("Mask UV Border", Vector) = (0,0,1,1)
        _SoftMask_ChannelWeights("Mask Channel Weights", Vector) = (0,0,0,1)
        _SoftMask_TileRepeat("Mask Tile Repeat", Vector) = (1,1,0,0)

        _StencilComp("Stencil Comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255

        _ColorMask("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip", Float) = 0
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

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #include "UISoftMask.cginc" // Soft Mask support

            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            // Soft Mask support
            #pragma multi_compile __ SOFTMASK_SLICED SOFTMASK_TILED

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
                // Soft Mask support
                // Like in standard Unity's UNITY_FOG_COORDS(), the number in braces determines
                // interpolator index that should be used by Soft Mask (it's `n` in TEXCOORDn).
                SOFT_MASK_COORDS(2)
            };

            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.worldPosition = IN.vertex;
                OUT.vertex = mul(UNITY_MATRIX_MVP, IN.vertex);

                OUT.texcoord = IN.texcoord;

            #ifdef UNITY_HALF_TEXEL_OFFSET
                OUT.vertex.xy += (_ScreenParams.zw - 1.0) * float2(-1, 1);
            #endif

                OUT.color = IN.color * _Color;
                SOFT_MASK_CALCULATE_COORDS(OUT, IN.vertex) // Soft Mask support
                return OUT;
            }

            sampler2D _MainTex;

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                color.a *= SoftMask_GetMask(IN.maskPosition.xy); // Soft Mask support
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
