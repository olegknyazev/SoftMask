// Copyright (C) 2014 - 2016 Stephan Bouchard - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms


using UnityEngine;
using UnityEditor;

namespace TMPro.EditorUtilities
{

    public class TMP_SDFShaderGUI : TMP_BaseShaderGUI
    {
        static MaterialPanel
            facePanel, outlinePanel, underlayPanel,
            bevelPanel, lightingPanel, bumpMapPanel, envMapPanel, glowPanel, debugPanel;

        static ShaderFeature outlineFeature, underlayFeature, bevelFeature, glowFeature, maskFeature;

        static string[]
            faceUVSpeedNames = { "_FaceUVSpeedX", "_FaceUVSpeedY" },
            outlineUVSpeedNames = { "_OutlineUVSpeedX", "_OutlineUVSpeedY" };

        static TMP_SDFShaderGUI()
        {
            facePanel = new MaterialPanel("Face", true);
            outlinePanel = new MaterialPanel("Outline", true);
            underlayPanel = new MaterialPanel("Underlay", false);
            bevelPanel = new MaterialPanel("Bevel", false);
            lightingPanel = new MaterialPanel("Lighting", false);
            bumpMapPanel = new MaterialPanel("BumpMap", false);
            envMapPanel = new MaterialPanel("EnvMap", false);
            glowPanel = new MaterialPanel("Glow", false);
            debugPanel = new MaterialPanel("Debug", false);

            outlineFeature = new ShaderFeature()
            {
                undoLabel = "Outline",
                keywords = new string[] { "OUTLINE_ON" }
            };

            underlayFeature = new ShaderFeature()
            {
                undoLabel = "Underlay",
                keywords = new string[] { "UNDERLAY_ON", "UNDERLAY_INNER" },
                label = new GUIContent("Underlay Type"),
                keywordLabels = new GUIContent[] {
                    new GUIContent("None"), new GUIContent("Normal"), new GUIContent("Inner")
                }
            };

            bevelFeature = new ShaderFeature()
            {
                undoLabel = "Bevel",
                keywords = new string[] { "BEVEL_ON" }
            };

            glowFeature = new ShaderFeature()
            {
                undoLabel = "Glow",
                keywords = new string[] { "GLOW_ON" }
            };

            maskFeature = new ShaderFeature()
            {
                undoLabel = "Mask",
                keywords = new string[] { "MASK_HARD", "MASK_SOFT" },
                label = new GUIContent("Mask"),
                keywordLabels = new GUIContent[] {
                    new GUIContent("Mask Off"), new GUIContent("Mask Hard"), new GUIContent("Mask Soft")
                }
            };
        }

        protected override void DoGUI()
        {
            if (DoPanelHeader(facePanel))
            { DoFacePanel(); }
            if (
                material.HasProperty(ShaderUtilities.ID_OutlineTex) ?
                DoPanelHeader(outlinePanel) : DoPanelHeader(outlinePanel, outlineFeature)
            )
            { DoOutlinePanel(); }
            if (material.HasProperty(ShaderUtilities.ID_UnderlayColor) && DoPanelHeader(underlayPanel, underlayFeature))
            { DoUnderlayPanel(); }
            if (material.HasProperty("_SpecularColor"))
            {
                if (DoPanelHeader(bevelPanel, bevelFeature))
                { DoBevelPanel(); }
                if (DoPanelHeader(lightingPanel, bevelFeature, false))
                { DoLocalLightingPanel(); }
                if (DoPanelHeader(bumpMapPanel, bevelFeature, false))
                { DoBumpMapPanel(); }
                if (DoPanelHeader(envMapPanel, bevelFeature, false))
                { DoEnvMapPanel(); }
            }
            else if (material.HasProperty("_SpecColor"))
            {
                if (DoPanelHeader(bevelPanel))
                { DoBevelPanel(); }
                if (DoPanelHeader(lightingPanel))
                { DoSurfaceLightingPanel(); }
                if (DoPanelHeader(bumpMapPanel))
                { DoBumpMapPanel(); }
                if (DoPanelHeader(envMapPanel))
                { DoEnvMapPanel(); }
            }
            if (material.HasProperty(ShaderUtilities.ID_GlowColor) && DoPanelHeader(glowPanel, glowFeature))
            { DoGlowPanel(); }
            if (DoPanelHeader(debugPanel))
            { DoDebugPanel(); }
        }

        void DoFacePanel()
        {
            EditorGUI.indentLevel += 1;
            DoColor("_FaceColor", "Color");
            if (material.HasProperty(ShaderUtilities.ID_FaceTex))
            {
                if (material.HasProperty("_FaceUVSpeedX"))
                { DoTexture2D("_FaceTex", "Texture", true, faceUVSpeedNames); }
                else
                { DoTexture2D("_FaceTex", "Texture", true); }
            }
            DoSlider("_OutlineSoftness", "Softness");
            DoSlider("_FaceDilate", "Dilate");
            if (material.HasProperty(ShaderUtilities.ID_Shininess))
            { DoSlider("_FaceShininess", "Gloss"); }
            EditorGUI.indentLevel -= 1;
        }

        void DoOutlinePanel()
        {
            EditorGUI.indentLevel += 1;
            DoColor("_OutlineColor", "Color");
            if (material.HasProperty(ShaderUtilities.ID_OutlineTex))
            {
                if (material.HasProperty("_OutlineUVSpeedX"))
                { DoTexture2D("_OutlineTex", "Texture", true, outlineUVSpeedNames); }
                else
                { DoTexture2D("_OutlineTex", "Texture", true); }
            }
            DoSlider("_OutlineWidth", "Thickness");
            if (material.HasProperty("_OutlineShininess"))
            { DoSlider("_OutlineShininess", "Gloss"); }
            EditorGUI.indentLevel -= 1;
        }

        void DoUnderlayPanel()
        {
            EditorGUI.indentLevel += 1;
            underlayFeature.DoPopup(editor, material);
            DoColor("_UnderlayColor", "Color");
            DoSlider("_UnderlayOffsetX", "Offset X");
            DoSlider("_UnderlayOffsetY", "Offset Y");
            DoSlider("_UnderlayDilate", "Dilate");
            DoSlider("_UnderlaySoftness", "Softness");
            EditorGUI.indentLevel -= 1;
        }

        private static GUIContent[] bevelTypeLabels = {
        new GUIContent("Outer Bevel"),
        new GUIContent("Inner Bevel")
    };

        void DoBevelPanel()
        {
            EditorGUI.indentLevel += 1;
            DoPopup("_ShaderFlags", "Type", bevelTypeLabels);
            DoSlider("_Bevel", "Amount");
            DoSlider("_BevelOffset", "Offset");
            DoSlider("_BevelWidth", "Width");
            DoSlider("_BevelRoundness", "Roundness");
            DoSlider("_BevelClamp", "Clamp");
            EditorGUI.indentLevel -= 1;
        }

        void DoLocalLightingPanel()
        {
            EditorGUI.indentLevel += 1;
            DoSlider("_LightAngle", "Light Angle");
            DoColor("_SpecularColor", "Specular Color");
            DoSlider("_SpecularPower", "Specular Power");
            DoSlider("_Reflectivity", "Reflectivity Power");
            DoSlider("_Diffuse", "Diffuse Shadow");
            DoSlider("_Ambient", "Ambient Shadow");
            EditorGUI.indentLevel -= 1;
        }

        void DoSurfaceLightingPanel()
        {
            EditorGUI.indentLevel += 1;
            DoColor("_SpecColor", "Specular Color");
            EditorGUI.indentLevel -= 1;
        }

        void DoBumpMapPanel()
        {
            EditorGUI.indentLevel += 1;
            DoTexture2D("_BumpMap", "Texture");
            DoSlider("_BumpFace", "Face");
            DoSlider("_BumpOutline", "Outline");
            EditorGUI.indentLevel -= 1;
        }

        void DoEnvMapPanel()
        {
            EditorGUI.indentLevel += 1;
            DoColor("_ReflectFaceColor", "Face Color");
            DoColor("_ReflectOutlineColor", "Outline Color");
            DoCubeMap("_Cube", "Texture");
            DoVector3("_EnvMatrixRotation", "EnvMap Rotation");
            EditorGUI.indentLevel -= 1;
        }

        void DoGlowPanel()
        {
            EditorGUI.indentLevel += 1;
            DoColor("_GlowColor", "Color");
            DoSlider("_GlowOffset", "Offset");
            DoSlider("_GlowInner", "Inner");
            DoSlider("_GlowOuter", "Outer");
            DoSlider("_GlowPower", "Power");
            EditorGUI.indentLevel -= 1;
        }

        void DoDebugPanel()
        {
            EditorGUI.indentLevel += 1;
            DoTexture2D("_MainTex", "Font Atlas");
            DoFloat("_GradientScale", "Gradient Scale");
            DoFloat("_TextureWidth", "Texture Width");
            DoFloat("_TextureHeight", "Texture Height");
            DoEmptyLine();
            DoFloat("_ScaleX", "Scale X");
            DoFloat("_ScaleY", "Scale Y");
            DoSlider("_PerspectiveFilter", "Perspective Filter");
            DoEmptyLine();
            DoFloat("_VertexOffsetX", "Offset X");
            DoFloat("_VertexOffsetY", "Offset Y");

            if (material.HasProperty(ShaderUtilities.ID_MaskCoord))
            {
                DoEmptyLine();
                maskFeature.ReadState(material);
                maskFeature.DoPopup(editor, material);
                if (maskFeature.Active)
                { DoMaskSubgroup(); }
                DoEmptyLine();
                DoVector("_ClipRect", "Clip Rect", lbrtVectorLabels);
            }
            else if (material.HasProperty("_MaskTex"))
            { DoMaskTexSubgroup(); }
            else if (material.HasProperty(ShaderUtilities.ID_MaskSoftnessX))
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

            DoEmptyLine();

            EditorGUI.BeginChangeCheck();
            bool useRatios = EditorGUILayout.Toggle("Use Ratios?", !material.IsKeywordEnabled("RATIOS_OFF"));
            if (EditorGUI.EndChangeCheck())
            {
                editor.RegisterPropertyChangeUndo("Use Ratios");
                if (useRatios)
                { material.DisableKeyword("RATIOS_OFF"); }
                else
                { material.EnableKeyword("RATIOS_OFF"); }
            }

            EditorGUI.BeginDisabledGroup(true);
            DoFloat("_ScaleRatioA", "Scale Ratio A");
            DoFloat("_ScaleRatioB", "Scale Ratio B");
            DoFloat("_ScaleRatioC", "Scale Ratio C");
            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel -= 1;
        }

        void DoMaskSubgroup()
        {
            DoVector("_MaskCoord", "Mask Bounds", xywhVectorLabels);
            if (Selection.activeGameObject != null)
            {
                Renderer renderer = Selection.activeGameObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Rect rect = EditorGUILayout.GetControlRect();
                    rect.x += EditorGUIUtility.labelWidth;
                    rect.width -= EditorGUIUtility.labelWidth;
                    if (GUI.Button(rect, "Match Renderer Bounds"))
                    {
                        FindProperty("_MaskCoord", properties).vectorValue = new Vector4(
                            0,
                            0,
                            Mathf.Round(renderer.bounds.extents.x * 1000) / 1000,
                            Mathf.Round(renderer.bounds.extents.y * 1000) / 1000
                        );
                    }
                }
            }
            if (maskFeature.State == 1)
            {
                DoFloat("_MaskSoftnessX", "Softness X");
                DoFloat("_MaskSoftnessY", "Softness Y");
            }
        }

        void DoMaskTexSubgroup()
        {
            DoEmptyLine();
            DoTexture2D("_MaskTex", "Mask Texture");
            DoToggle("_MaskInverse", "Inverse Mask");
            DoColor("_MaskEdgeColor", "Edge Color");
            DoSlider("_MaskEdgeSoftness", "Edge Softness");
            DoSlider("_MaskWipeControl", "Wipe Position");
            DoFloat("_MaskSoftnessX", "Softness X");
            DoFloat("_MaskSoftnessY", "Softness Y");
            DoVector("_ClipRect", "Clip Rect", lbrtVectorLabels);
        }
    }
}
