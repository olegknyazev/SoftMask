using System;
using UnityEditor;
using UnityEngine;

namespace EnhancedHierarchy.Icons {
    [Serializable]
    internal sealed class Active : RightSideIcon {

        public override void DoGUI(Rect rect) {
            using(new GUIBackgroundColor(EnhancedHierarchy.CurrentGameObject.activeSelf ? Styles.backgroundColorEnabled : Styles.backgroundColorDisabled)) {
                GUI.changed = false;
                GUI.Toggle(rect, EnhancedHierarchy.CurrentGameObject.activeSelf, Styles.activeContent, Styles.activeToggleStyle);

                if(!GUI.changed)
                    return;

                var objs = GetSelectedObjectsAndCurrent();
                var active = !EnhancedHierarchy.CurrentGameObject.activeSelf;

                Undo.RecordObjects(objs.ToArray(), EnhancedHierarchy.CurrentGameObject.activeSelf ? "Disabled GameObject" : "Enabled Gameobject");

                foreach(var obj in objs)
                    obj.SetActive(active);
            }
        }

    }
}