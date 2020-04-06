using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace EnhancedHierarchy.Icons {
    [Serializable]
    internal sealed class Warnings : LeftSideIcon {

        private const int MAX_STRING_LEN = 750;
        private const float ICONS_WIDTH = 16f;

        [NonSerialized]
        public static StringBuilder goLogs = new StringBuilder(MAX_STRING_LEN);
        [NonSerialized]
        public static StringBuilder goWarnings = new StringBuilder(MAX_STRING_LEN);
        [NonSerialized]
        public static StringBuilder goErrors = new StringBuilder(MAX_STRING_LEN);
        [NonSerialized]
        private static GUIContent tempTooltipContent = new GUIContent();

        public override string Name { get { return "Logs, Warnings and Errors"; } }
        public override float Width {
            get {
                var result = 0f;

                if(goLogs.Length > 0)
                    result += ICONS_WIDTH;
                if(goWarnings.Length > 0)
                    result += ICONS_WIDTH;
                if(goErrors.Length > 0)
                    result += ICONS_WIDTH;

                return result;
            }
        }

        public override void Init() {
            if(!EnhancedHierarchy.IsRepaintEvent || !EnhancedHierarchy.IsGameObject)
                return;

            goLogs.Length = 0;
            goWarnings.Length = 0;
            goErrors.Length = 0;

            var contextEntries = (List<LogEntry>)null;
            var components = EnhancedHierarchy.MonoBehaviours;

            for(var i = 0; i < components.Length; i++)
                if(!components[i])
                    goWarnings.AppendLine("Missing MonoBehaviour\n");

            if(LogEntry.ReferencedObjects.TryGetValue(EnhancedHierarchy.CurrentGameObject, out contextEntries))
                for(var i = 0; i < contextEntries.Count; i++)
                    if(goLogs.Length < MAX_STRING_LEN && contextEntries[i].HasMode(EntryMode.ScriptingLog))
                        goLogs.AppendLine(contextEntries[i].ToString());

                    else if(goWarnings.Length < MAX_STRING_LEN && contextEntries[i].HasMode(EntryMode.ScriptingWarning))
                        goWarnings.AppendLine(contextEntries[i].ToString());

                    else if(goErrors.Length < MAX_STRING_LEN && contextEntries[i].HasMode(EntryMode.ScriptingError))
                        goErrors.AppendLine(contextEntries[i].ToString());
        }

        public override void DoGUI(Rect rect) {
            if(!EnhancedHierarchy.IsRepaintEvent || !EnhancedHierarchy.IsGameObject)
                return;

            rect.xMax = rect.xMin + 17f;
            rect.yMax += 1f;

            if(goLogs.Length > 0) {
                if(rect.Contains(Event.current.mousePosition))
                    if(Preferences.Tooltips)
                        tempTooltipContent.tooltip = goLogs.ToString().TrimEnd('\n', '\r');
                    else
                        tempTooltipContent.tooltip = string.Empty;

                GUI.DrawTexture(rect, Styles.infoIcon, ScaleMode.ScaleToFit);
                EditorGUI.LabelField(rect, tempTooltipContent);
                rect.x += ICONS_WIDTH;
            }

            if(goWarnings.Length > 0) {
                if(rect.Contains(Event.current.mousePosition))
                    if(Preferences.Tooltips)
                        tempTooltipContent.tooltip = goWarnings.ToString().TrimEnd('\n', '\r');
                    else
                        tempTooltipContent.tooltip = string.Empty;

                GUI.DrawTexture(rect, Styles.warningIcon, ScaleMode.ScaleToFit);
                EditorGUI.LabelField(rect, tempTooltipContent);
                rect.x += ICONS_WIDTH;
            }

            if(goErrors.Length > 0) {
                if(rect.Contains(Event.current.mousePosition))
                    if(Preferences.Tooltips)
                        tempTooltipContent.tooltip = goErrors.ToString().TrimEnd('\n', '\r');
                    else
                        tempTooltipContent.tooltip = string.Empty;

                GUI.DrawTexture(rect, Styles.errorIcon, ScaleMode.ScaleToFit);
                EditorGUI.LabelField(rect, tempTooltipContent);
                rect.x += ICONS_WIDTH;
            }
        }
    }
}