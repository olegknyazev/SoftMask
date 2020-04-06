using System;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace EnhancedHierarchy.Icons {
    [Serializable]
    internal sealed class MonoBehaviourIcon : LeftSideIcon {

        [NonSerialized]
        private static StringBuilder goComponents = new StringBuilder(500);
        [NonSerialized]
        private static GUIContent tempTooltipContent = new GUIContent();

        public override string Name { get { return "MonoBehaviour Icon"; } }
        public override float Width { get { return goComponents.Length > 0 ? 15f : 0f; } }

        public override void Init() {
            if(!EnhancedHierarchy.IsRepaintEvent || !EnhancedHierarchy.IsGameObject)
                return;

            goComponents.Length = 0;
            var components = EnhancedHierarchy.MonoBehaviours;

            for(var i = 0; i < components.Length; i++)
                if(components[i])
                    goComponents.AppendLine(components[i].GetType().ToString());
        }

        public override void DoGUI(Rect rect) {
            if(!EnhancedHierarchy.IsRepaintEvent || !EnhancedHierarchy.IsGameObject)
                return;

            if(goComponents.Length > 0) {
                if(rect.Contains(Event.current.mousePosition) && Preferences.Tooltips)
                    tempTooltipContent.tooltip = goComponents.ToString().TrimEnd('\n', '\r');
                else
                    tempTooltipContent.tooltip = string.Empty;

                rect.yMin += 1f;
                rect.yMax -= 1f;
                rect.xMin += 1f;

                GUI.DrawTexture(rect, Styles.monobehaviourIconTexture, ScaleMode.ScaleToFit);
                EditorGUI.LabelField(rect, tempTooltipContent);
            }
        }
    }
}