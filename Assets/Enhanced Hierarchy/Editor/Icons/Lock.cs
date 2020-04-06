using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace EnhancedHierarchy.Icons {
    [Serializable]
    internal sealed class Lock : RightSideIcon {

        public override void DoGUI(Rect rect) {
            var locked = (EnhancedHierarchy.CurrentGameObject.hideFlags & HideFlags.NotEditable) != 0;

            using(new GUIBackgroundColor(locked ? Styles.backgroundColorEnabled : Styles.backgroundColorDisabled)) {
                GUI.changed = false;
                GUI.Toggle(rect, locked, Styles.lockContent, Styles.lockToggleStyle);

                if(!GUI.changed)
                    return;

                var selectedObjects = GetSelectedObjectsAndCurrent();
                var changeMode = AskChangeModeIfNecessary(selectedObjects, Preferences.LockAskMode.Value, "Lock Object",
                    "Do you want to " + (!locked ? "lock" : "unlock") + " the children objects as well?");

                switch(changeMode) {
                    case ChildrenChangeMode.ObjectOnly:
                        foreach(var obj in selectedObjects)
                            Undo.RegisterCompleteObjectUndo(obj, locked ? "Unlock Object" : "Lock Object");

                        foreach(var obj in selectedObjects)
                            if(!locked)
                                Utility.LockObject(obj);
                            else
                                Utility.UnlockObject(obj);
                        break;

                    case ChildrenChangeMode.ObjectAndChildren:
                        foreach(var obj in selectedObjects)
                            Undo.RegisterFullObjectHierarchyUndo(obj, locked ? "Unlock Object" : "Lock Object");

                        foreach(var obj in selectedObjects)
                            foreach(var transform in obj.GetComponentsInChildren<Transform>(true))
                                if(!locked)
                                    Utility.LockObject(transform.gameObject);
                                else
                                    Utility.UnlockObject(transform.gameObject);
                        break;
                }

                InternalEditorUtility.RepaintAllViews();
            }
        }

    }
}