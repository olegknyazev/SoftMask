// Copyright (C) 2014 - 2016 Stephan Bouchard - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms


using UnityEngine;
using UnityEditor;

namespace TMPro.EditorUtilities
{
    public class TMP_BitmapShaderGUI : TMP_BaseShaderGUI
    {
        static MaterialPanel
            facePanel, debugPanel;

        static TMP_BitmapShaderGUI()
        {
            facePanel = new MaterialPanel("Face", true);
            debugPanel = new MaterialPanel("Debug", false);
        }

        protected override void DoGUI()
        {
            if (DoPanelHeader(facePanel))
            { DoFacePanel(); }
            if (DoPanelHeader(debugPanel))
            { DoDebugPanel(); }
        }

        void DoFacePanel()
        {
            EditorGUI.indentLevel += 1;
            if (material.HasProperty(ShaderUtilities.ID_FaceTex))
            {
                DoColor("_FaceColor", "Color");
                DoTexture2D("_FaceTex", "Texture", true);
            }
            else
            {
                DoColor("_Color", "Color");
                DoSlider("_DiffusePower", "Diffuse Power");
            }
            EditorGUI.indentLevel -= 1;
        }

        void DoDebugPanel()
        {
            EditorGUI.indentLevel += 1;
            DoTexture2D("_MainTex", "Font Atlas");
            if (material.HasProperty(ShaderUtilities.ID_VertexOffsetX))
            {
                DoEmptyLine();
                DoFloat("_VertexOffsetX", "Offset X");
                DoFloat("_VertexOffsetY", "Offset Y");
            }
            if (material.HasProperty(ShaderUtilities.ID_MaskSoftnessX))
            {
                DoEmptyLine();
                DoFloat("_MaskSoftnessX", "Softness X");
                DoFloat("_MaskSoftnessY", "Softness Y");
                DoVector("_ClipRect", "Clip Rect", lbrtVectorLabels);
            }
            if (material.HasProperty(ShaderUtilities.ID_StencilID))
            {
                DoEmptyLine();
                DoFloat("_Stencil", "Stencil ID");
                DoFloat("_StencilComp", "Stencil Comp");
            }
            EditorGUI.indentLevel -= 1;
        }
    }
}