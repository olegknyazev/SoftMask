// Copyright (C) 2014 - 2016 Stephan Bouchard - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms


using UnityEngine;

namespace TMPro
{

    // Base class inherited by the various TextMeshPro Assets.
    [System.Serializable]
    public class TMP_Asset : ScriptableObject
    {
        /// <summary>
        /// HashCode based on the name of the asset.
        /// </summary>
        public int hashCode;

        /// <summary>
        /// The material used by this asset.
        /// </summary>
        public Material material;

        /// <summary>
        /// HashCode based on the name of the material assigned to this asset.
        /// </summary>
        public int materialHashCode;

    }
}
