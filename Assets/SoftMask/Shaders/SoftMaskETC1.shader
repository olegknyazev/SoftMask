Shader "Hidden/UI Default ETC1 (Soft Masked)"
{
    // ETC1-version (with alpha split texture) of SoftMask.shader.

    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        [PerRendererData] _AlphaTex("Sprite Alpha Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)

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
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #pragma multi_compile __ UNITY_UI_ALPHACLIP
            #pragma multi_compile __ SOFTMASK_SIMPLE SOFTMASK_SLICED SOFTMASK_TILED

            #define SOFTMASK_ETC1
            #include "SoftMaskTemplate.cginc"
        ENDCG
        }
    }
}
