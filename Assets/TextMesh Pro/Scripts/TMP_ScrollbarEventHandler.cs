// Copyright (C) 2014 - 2016 Stephan Bouchard - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms


using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System;


namespace TMPro
{

    public class TMP_ScrollbarEventHandler : MonoBehaviour, IPointerClickHandler, ISelectHandler, IDeselectHandler
    {
        public bool isSelected;

        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log("Scrollbar click...");
        }

        public void OnSelect(BaseEventData eventData)
        {
            Debug.Log("Scrollbar selected");
            isSelected = true;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            Debug.Log("Scrollbar De-Selected");
            isSelected = false;
        }
    }
}
