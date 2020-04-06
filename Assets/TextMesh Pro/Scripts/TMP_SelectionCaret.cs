// Copyright (C) 2014 - 2016 Stephan Bouchard - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms


using UnityEngine;
using UnityEngine.UI;


namespace TMPro
{
    /// <summary>
    /// A simple component that can be added to a newly created object where inheriting from MaskableGraphic is needed.
    /// </summary>
    public class TMP_SelectionCaret : MaskableGraphic
    {

        /// <summary>
        /// Override to Cull function of MaskableGraphic to prevent Culling.
        /// </summary>
        /// <param name="clipRect"></param>
        /// <param name="validRect"></param>
        public override void Cull(Rect clipRect, bool validRect)
        {
            //base.Cull(clipRect, validRect);
        }
    }
}
