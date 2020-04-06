// Copyright (C) 2014 - 2016 Stephan Bouchard - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms


using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


namespace TMPro
{
    /// <summary>
    /// Class that contains the basic information about the font.
    /// </summary>
    [Serializable]
    public class FaceInfo
    {
        public string Name;
        public float PointSize;
        public float Scale;

        public int CharacterCount;

        public float LineHeight;
        public float Baseline;
        public float Ascender;
        public float CapHeight;
        public float Descender;
        public float CenterLine;

        public float SuperscriptOffset;
        public float SubscriptOffset;
        public float SubSize;

        public float Underline;
        public float UnderlineThickness;

        public float strikethrough;
        public float strikethroughThickness;

        public float TabWidth;

        public float Padding;
        public float AtlasWidth;
        public float AtlasHeight;
    }


    // Class which contains the Glyph Info / Character definition for each character contained in the font asset.
    [Serializable]
    public class TMP_Glyph : TMP_TextElement
    {
        /// <summary>
        /// Function to create a deep copy of a GlyphInfo.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static TMP_Glyph Clone(TMP_Glyph source)
        {
            TMP_Glyph copy = new TMP_Glyph();

            copy.id = source.id;
            copy.x = source.x;
            copy.y = source.y;
            copy.width = source.width;
            copy.height = source.height;
            copy.xOffset = source.xOffset;
            copy.yOffset = source.yOffset;
            copy.xAdvance = source.xAdvance;
            copy.scale = source.scale;

            return copy;
        }
    }


    // Structure which holds the font creation settings
    [Serializable]
    public struct FontCreationSetting
    {
        public string fontSourcePath;
        public int fontSizingMode;
        public int fontSize;
        public int fontPadding;
        public int fontPackingMode;
        public int fontAtlasWidth;
        public int fontAtlasHeight;
        public int fontCharacterSet;
        public int fontStyle;
        public float fontStlyeModifier;
        public int fontRenderMode;
        public bool fontKerning;
    }


    // Class which contains pre-defined mesh information for each character. This is not used at this time.
    [Serializable]
    public class Glyph2D
    {
        // Vertices aligned with pivot located at Midline / Baseline.
        public Vector3 bottomLeft;
        public Vector3 topLeft;
        public Vector3 bottomRight;
        public Vector3 topRight;

        public Vector2 uv0;
        public Vector2 uv1;
        public Vector2 uv2;
        public Vector2 uv3;
    }


    public struct KerningPairKey
    {
        public int ascii_Left;
        public int ascii_Right;
        public int key;

        public KerningPairKey(int ascii_left, int ascii_right)
        {
            ascii_Left = ascii_left;
            ascii_Right = ascii_right;
            key = (ascii_right << 16) + ascii_left;
        }
    }


    [Serializable]
    public class KerningPair
    {
        public int AscII_Left;
        public int AscII_Right;
        public float XadvanceOffset;

        public KerningPair(int left, int right, float offset)
        {
            AscII_Left = left;
            AscII_Right = right;
            XadvanceOffset = offset;
        }
    }


    [Serializable]
    public class KerningTable
    {
        public List<KerningPair> kerningPairs;


        public KerningTable()
        {
            kerningPairs = new List<KerningPair>();
        }


        public void AddKerningPair()
        {
            if (kerningPairs.Count == 0)
            {
                kerningPairs.Add(new KerningPair(0, 0, 0));
            }
            else
            {
                int left = kerningPairs.Last().AscII_Left;
                int right = kerningPairs.Last().AscII_Right;
                float xoffset = kerningPairs.Last().XadvanceOffset;

                kerningPairs.Add(new KerningPair(left, right, xoffset));
            }

        }


        public int AddKerningPair(int left, int right, float offset)
        {
            int index = kerningPairs.FindIndex(item => item.AscII_Left == left && item.AscII_Right == right);

            if (index == -1)
            {
                kerningPairs.Add(new KerningPair(left, right, offset));
                return 0;
            }

            // Return -1 if Kerning Pair already exists.
            return -1;
        }


        public void RemoveKerningPair(int left, int right)
        {
            int index = kerningPairs.FindIndex(item => item.AscII_Left == left && item.AscII_Right == right);

            if (index != -1)
                kerningPairs.RemoveAt(index);
        }


        public void RemoveKerningPair(int index)
        {
            kerningPairs.RemoveAt(index);
        }


        public void SortKerningPairs()
        {
            // Sort List of Kerning Info
            if (kerningPairs.Count > 0)
                kerningPairs = kerningPairs.OrderBy(s => s.AscII_Left).ThenBy(s => s.AscII_Right).ToList();
        }
    }


    public static class TMP_FontUtilities
    {
        private static List<int> k_searchedFontAssets;

        /// <summary>
        /// Search through the given font and its fallbacks for the specified character.
        /// </summary>
        /// <param name="font">The font asset to search for the given character.</param>
        /// <param name="character">The character to find.</param>
        /// <param name="glyph">out parameter containing the glyph for the specified character (if found).</param>
        /// <returns></returns>
        public static TMP_FontAsset SearchForGlyph(TMP_FontAsset font, int character, out TMP_Glyph glyph)
        {
            if (k_searchedFontAssets == null)
                k_searchedFontAssets = new List<int>();

            k_searchedFontAssets.Clear();

            return SearchForGlyphInternal(font, character, out glyph);
        }


        /// <summary>
        /// Search through the given list of fonts and their possible fallbacks for the specified character.
        /// </summary>
        /// <param name="fonts"></param>
        /// <param name="character"></param>
        /// <param name="glyph"></param>
        /// <returns></returns>
        public static TMP_FontAsset SearchForGlyph(List<TMP_FontAsset> fonts, int character, out TMP_Glyph glyph)
        {
            return SearchForGlyphInternal(fonts, character, out glyph);
        }


        private static TMP_FontAsset SearchForGlyphInternal (TMP_FontAsset font, int character, out TMP_Glyph glyph)
        {
            glyph = null;

            if (font == null) return null;

            if (font.characterDictionary.TryGetValue(character, out glyph))
            {
                return font;
            }
            else if (font.fallbackFontAssets != null && font.fallbackFontAssets.Count > 0)
            {
                for (int i = 0; i < font.fallbackFontAssets.Count && glyph == null; i++)
                {
                    TMP_FontAsset temp = font.fallbackFontAssets[i];
                    if (temp == null) continue;

                    int id = temp.GetInstanceID();

                    // Skip over the fallback font asset in the event it is null or if already searched.
                    if (k_searchedFontAssets.Contains(id)) continue;

                    // Add to list of font assets already searched.
                    k_searchedFontAssets.Add(id);

                    temp = SearchForGlyphInternal(temp, character, out glyph);

                    if (temp != null)
                        return temp;
                }
            }

            return null;
        }


        private static TMP_FontAsset SearchForGlyphInternal(List<TMP_FontAsset> fonts, int character, out TMP_Glyph glyph)
        {
            glyph = null;

            if (fonts != null && fonts.Count > 0)
            {
                for (int i = 0; i < fonts.Count; i++)
                {
                    TMP_FontAsset fontAsset = SearchForGlyphInternal(fonts[i], character, out glyph);

                    if (fontAsset != null)
                        return fontAsset;
                }
            }

            return null;
        }
    }



}