using System;
using UnityEditor;
using UnityEngine;

namespace EnhancedHierarchy.Icons {
    [Serializable]
    internal sealed class PrefabApply : RightSideIcon {

        public override string Name { get { return "Apply Prefab"; } }

        public override void DoGUI(Rect rect) {
            var isPrefab = PrefabUtility.GetPrefabType(EnhancedHierarchy.CurrentGameObject) == PrefabType.PrefabInstance;

            using(new GUIContentColor(isPrefab ? Styles.backgroundColorEnabled : Styles.backgroundColorDisabled))
                if(GUI.Button(rect, Styles.prefabApplyContent, Styles.applyPrefabStyle)) {
                    var objs = GetSelectedObjectsAndCurrent();

                    foreach(var obj in objs)
                        Utility.ApplyPrefabModifications(obj, objs.Count <= 1);
                }
        }

    }
}