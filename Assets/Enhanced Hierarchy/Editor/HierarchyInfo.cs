using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EnhancedHierarchy {
    internal static partial class EnhancedHierarchy {

        private const int UNLAYERED = 0;
        private const string UNTAGGED = "Untagged";
        private const float ALPHA_THRESHOLD = 0.01f;

        private static GUIContent trailingContent = new GUIContent("...");
        private static GUIContent tempGameObjectNameContent = new GUIContent();
        private static GUIContent tempChildExpandContent = new GUIContent();
        private static GUIContent tempTooltipContent = new GUIContent();

        public static bool IsFirstVisible { get; private set; }
        public static bool IsRepaintEvent { get; private set; }
        public static bool IsGameObject { get; private set; }
        public static bool HasTag { get; private set; }
        public static bool HasLayer { get; private set; }
        public static float LeftIconsWidth { get; private set; }
        public static float RightIconsWidth { get; private set; }
        public static float LabelSize { get; private set; }
        public static Rect RawRect { get; private set; }
        public static Rect FinalRect { get; private set; }
        public static Rect SelectionRect { get; private set; }
        public static Color CurrentColor { get; private set; }
        public static Vector2 SelectionStart { get; private set; }
        public static GUIStyle CurrentStyle { get; private set; }
        public static GameObject CurrentGameObject { get; private set; }
        public static List<Object> DragSelection { get; private set; }
        public static MonoBehaviour[] MonoBehaviours { get; private set; }
        public static EventType LastEventType { get; private set; }

        public static void SetItemInformation(int id, Rect rect) {
            if(!Preferences.Enabled)
                return;

            using(ProfilerSample.Get("Enhanced Hierarchy"))
            using(ProfilerSample.Get())
                try {
                    CurrentGameObject = EditorUtility.InstanceIDToObject(id) as GameObject;

                    IsGameObject = CurrentGameObject;
                    IsRepaintEvent = Event.current.type == EventType.Repaint;
                    IsFirstVisible = Event.current.type != LastEventType;
                    LastEventType = Event.current.type;

                    if(IsGameObject) {
                        LabelSize = EditorStyles.label.CalcSize(new GUIContent(CurrentGameObject.name)).x;
                        HasTag = CurrentGameObject.tag != UNTAGGED || !Preferences.HideDefaultTag;
                        HasLayer = CurrentGameObject.layer != UNLAYERED || !Preferences.HideDefaultLayer;
                        CurrentStyle = Utility.GetHierarchyLabelStyle(CurrentGameObject);
                        CurrentColor = CurrentStyle.normal.textColor;
                        MonoBehaviours = CurrentGameObject.GetComponents<MonoBehaviour>();
                    }

                    if(IsFirstVisible)
                        FinalRect = RawRect;

                    RawRect = rect;
                }
                catch(Exception e) {
                    Utility.LogException(e);
                }
        }

    }
}