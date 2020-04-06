// Copyright (C) 2014 - 2016 Stephan Bouchard - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms


using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using TMPro.EditorUtilities;
using TMPro.SpriteAssetUtilities;

namespace TMPro
{
#if UNITY_5_3_OR_NEWER
    public class TMP_SpriteAssetImporter : EditorWindow
    {
        // Create Sprite Asset Editor Window
        [MenuItem("Window/TextMeshPro/Sprite Importer")]
        public static void ShowFontAtlasCreatorWindow()
        {
            var window = GetWindow<TMP_SpriteAssetImporter>();
            window.titleContent = new GUIContent("Sprite Importer");
            window.Focus();
        }

        private Texture2D m_SpriteAtlas;
        private SpriteAssetImportFormats m_SpriteDataFormat = SpriteAssetImportFormats.TexturePacker;
        private TextAsset m_JsonFile;

        private string m_CreationFeedback;

        private TMP_SpriteAsset m_SpriteAsset;
        private List<TMP_Sprite> m_SpriteInfoList = new List<TMP_Sprite>();


        void OnEnable()
        {
            // Set Editor Window Size
            SetEditorWindowSize();
            
            // Get the UI Skin and Styles for the various Editors
            TMP_UIStyleManager.GetUIStyles();

        }

        public void OnGUI()
        {
            DrawEditorPanel();
        }


        void DrawEditorPanel()
        {
            GUILayout.BeginVertical();
            {
                GUILayout.Label("<b>TMP Sprite Importer</b>", TMP_UIStyleManager.Section_Label);

                // Label
                GUILayout.Label("Import Settings", TMP_UIStyleManager.Section_Label, GUILayout.Width(150));

                GUILayout.BeginVertical(TMP_UIStyleManager.TextureAreaBox);
                {
                    EditorGUI.BeginChangeCheck();

                    // Sprite Texture Selection
                    m_JsonFile = EditorGUILayout.ObjectField("Sprite Data Source", m_JsonFile, typeof(TextAsset), false) as TextAsset;

                    m_SpriteDataFormat = (SpriteAssetImportFormats)EditorGUILayout.EnumPopup("Import Format", m_SpriteDataFormat);
                    
                    // Sprite Texture Selection
                    m_SpriteAtlas = EditorGUILayout.ObjectField("Sprite Texture Atlas", m_SpriteAtlas, typeof(Texture2D), false) as Texture2D;

                    if (EditorGUI.EndChangeCheck())
                    {
                        m_CreationFeedback = string.Empty;
                    }

                    GUILayout.Space(10);

                    // Create Sprite Asset
                    if (GUILayout.Button("Create Sprite Asset"))
                    {
                        m_CreationFeedback = string.Empty;

                        // Read json data file
                        if (m_SpriteDataFormat == SpriteAssetImportFormats.TexturePacker)
                        {
                            TexturePacker.SpriteDataObject sprites = JsonUtility.FromJson<TexturePacker.SpriteDataObject>(m_JsonFile.text);

                            if (sprites != null && sprites.frames != null && sprites.frames.Count > 0)
                            {
                                int spriteCount = sprites.frames.Count;

                                // Update import results
                                m_CreationFeedback = "<b>Import Results</b>\n--------------------\n";
                                m_CreationFeedback += "<color=#C0ffff><b>" + spriteCount + "</b></color> Sprites were imported from file.";

                                // Create sprite info list
                                m_SpriteInfoList = CreateSpriteInfoList(sprites);
                            }
                        }

                    }

                    // Creation Feedback
                    GUILayout.Space(5);
                    GUILayout.BeginVertical(TMP_UIStyleManager.TextAreaBoxWindow, GUILayout.Height(60));
                    {
                        EditorGUILayout.LabelField(m_CreationFeedback, TMP_UIStyleManager.Label);
                    }
                    GUILayout.EndVertical();

                    GUILayout.Space(5);
                    GUI.enabled = m_SpriteInfoList != null ? true : false;    // Enable Save Button if font_Atlas is not Null.
                    if (GUILayout.Button("Save Sprite Asset"))
                    {
                        string filePath = string.Empty;

                        filePath = EditorUtility.SaveFilePanel("Save Sprite Asset File", new FileInfo(AssetDatabase.GetAssetPath(m_JsonFile)).DirectoryName, m_JsonFile.name, "asset");

                        if (filePath.Length == 0)
                            return;

                        SaveSpriteAsset(filePath);
                    }

                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();

        }


        /// <summary>
        /// 
        /// </summary>
        List<TMP_Sprite> CreateSpriteInfoList(TexturePacker.SpriteDataObject spriteDataObject)
        {
            List<TexturePacker.SpriteData> importedSprites = spriteDataObject.frames;

            List<TMP_Sprite> spriteInfoList = new List<TMP_Sprite>();

            for (int i = 0; i < importedSprites.Count; i++)
            {
                TMP_Sprite sprite = new TMP_Sprite();

                sprite.id = i;
                sprite.name = Path.GetFileNameWithoutExtension(importedSprites[i].filename);
                sprite.hashCode = TMP_TextUtilities.GetSimpleHashCode(sprite.name);

                // Attempt to extract Unicode value from name
                int unicode = 0;
                int indexOfSeperator = sprite.name.IndexOf('-');
                if (indexOfSeperator != -1)
                    unicode = TMP_TextUtilities.StringToInt(sprite.name.Substring(indexOfSeperator + 1));
                else
                    unicode = TMP_TextUtilities.StringToInt(sprite.name);

                sprite.unicode = unicode;

                sprite.x = importedSprites[i].frame.x;
                sprite.y = m_SpriteAtlas.height - (importedSprites[i].frame.y + importedSprites[i].frame.h);
                sprite.width = importedSprites[i].frame.w;
                sprite.height = importedSprites[i].frame.h;

                //Calculate sprite pivot position
                sprite.pivot = importedSprites[i].pivot;

                // Properties the can be modified
                sprite.xAdvance = sprite.width;
                sprite.scale = 1.0f;
                sprite.xOffset = 0 - (sprite.width * sprite.pivot.x);
                sprite.yOffset = sprite.height - (sprite.height * sprite.pivot.y);

                spriteInfoList.Add(sprite);
            }

            return spriteInfoList;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        void SaveSpriteAsset(string filePath)
        {
            filePath = filePath.Substring(0, filePath.Length - 6); // Trim file extension from filePath.

            string dataPath = Application.dataPath;

            if (filePath.IndexOf(dataPath, System.StringComparison.InvariantCultureIgnoreCase) == -1)
            {
                Debug.LogError("You're saving the font asset in a directory outside of this project folder. This is not supported. Please select a directory under \"" + dataPath + "\"");
                return;
            }

            string relativeAssetPath = filePath.Substring(dataPath.Length - 6);
            string dirName = Path.GetDirectoryName(relativeAssetPath);
            string fileName = Path.GetFileNameWithoutExtension(relativeAssetPath);
            string path_NoExt = dirName + "/" + fileName;


            // Create new Sprite Asset using this texture
            m_SpriteAsset = CreateInstance<TMP_SpriteAsset>();
            AssetDatabase.CreateAsset(m_SpriteAsset, path_NoExt + ".asset");

            // Compute the hash code for the sprite asset.
            m_SpriteAsset.hashCode = TMP_TextUtilities.GetSimpleHashCode(m_SpriteAsset.name);

            // Assign new Sprite Sheet texture to the Sprite Asset.
            m_SpriteAsset.spriteSheet = m_SpriteAtlas;
            m_SpriteAsset.spriteInfoList = m_SpriteInfoList;

            // Add new default material for sprite asset.
            AddDefaultMaterial(m_SpriteAsset);
        }


        /// <summary>
        /// Create and add new default material to sprite asset.
        /// </summary>
        /// <param name="spriteAsset"></param>
        private static void AddDefaultMaterial(TMP_SpriteAsset spriteAsset)
        {
            Shader shader = Shader.Find("TextMeshPro/Sprite");
            Material material = new Material(shader);
            material.SetTexture(ShaderUtilities.ID_MainTex, spriteAsset.spriteSheet);

            spriteAsset.material = material;
            material.hideFlags = HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(material, spriteAsset);
        }


        /// <summary>
        /// Limits the minimum size of the editor window.
        /// </summary>
        void SetEditorWindowSize()
        {
            EditorWindow editorWindow = this;

            Vector2 currentWindowSize = editorWindow.minSize;

            editorWindow.minSize = new Vector2(Mathf.Max(230, currentWindowSize.x), Mathf.Max(300, currentWindowSize.y));
        }

    }
#endif
}