// Copyright (C) 2014 - 2016 Stephan Bouchard - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms


using UnityEngine;
using System.Collections;


namespace TMPro
{
    /// <summary>
    /// Custom text input validator where user can implement their own custom character validation.
    /// </summary>
    [System.Serializable]
    public abstract class TMP_InputValidator : ScriptableObject
    {
        public abstract char Validate(ref string text, ref int pos, char ch);
    }
}
