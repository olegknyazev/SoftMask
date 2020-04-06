using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EnhancedHierarchy {
    internal static class ReflectionHelper {

        private const BindingFlags FULL_BINDING = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;

        static ReflectionHelper() {
            editorAssembly = typeof(Editor).Assembly;
            engineAssembly = typeof(Application).Assembly;

            hierarchyWindowType = editorAssembly.GetType("UnityEditor.SceneHierarchyWindow");
            iconSelectorType = editorAssembly.GetType("UnityEditor.IconSelector");

            lastHierarchyField = hierarchyWindowType.GetField("s_LastInteractedHierarchy", FULL_BINDING);
            iconSelectorInitMethod = iconSelectorType.GetMethod("Init", FULL_BINDING);

            imageConversionType = engineAssembly.GetType("UnityEngine.ImageConversion", false);

            if(imageConversionType == null)
                textureLoadMethod = typeof(Texture2D).GetMethod("LoadImage", new Type[] { typeof(byte[]) });
            else
                textureLoadMethod = imageConversionType.GetMethod("LoadImage", new Type[] { typeof(Texture2D), typeof(byte[]) });

            setIconMethod = typeof(EditorGUIUtility).GetMethod("SetIconForObject", FULL_BINDING);
            getIconMethod = typeof(EditorGUIUtility).GetMethod("GetIconForObject", FULL_BINDING);

            treeviewProp = hierarchyWindowType.GetProperty("treeView", FULL_BINDING);
            treeviewDataSourceProp = treeviewProp.PropertyType.GetProperty("data", FULL_BINDING);
            treeviewExpandedMethod = treeviewDataSourceProp.PropertyType.GetMethod("IsExpanded", FULL_BINDING, null, new[] { typeof(int) }, null);

            syncNeededProp = hierarchyWindowType.GetProperty("selectionSyncNeeded", FULL_BINDING);
        }

        private static readonly Assembly editorAssembly;
        private static readonly Assembly engineAssembly;
        private static readonly Type hierarchyWindowType;
        private static readonly Type iconSelectorType;
        private static readonly Type imageConversionType;
        private static readonly Type treeViewType;

        private static readonly FieldInfo lastHierarchyField;
        private static readonly PropertyInfo treeviewProp;
        private static readonly PropertyInfo treeviewDataSourceProp;
        private static readonly PropertyInfo syncNeededProp;
        private static readonly MethodInfo treeviewExpandedMethod;
        private static readonly MethodInfo textureLoadMethod;
        private static readonly MethodInfo iconSelectorInitMethod;
        private static readonly MethodInfo setIconMethod;
        private static readonly MethodInfo getIconMethod;

        private static object treeviewInstance;
        private static object treeviewDataSourceInstance;
        private static EditorWindow hierarchyWindow;

        public delegate void OnObjectIconChange(Texture2D icon);

        private static object playModeColor = typeof(Editor).Assembly.GetType("UnityEditor.HostView").GetField("kPlayModeDarken", FULL_BINDING).GetValue(null);
        private static PropertyInfo playModeColorProp = typeof(Editor).Assembly.GetType("UnityEditor.PrefColor").GetProperty("Color", FULL_BINDING);

        public static Color PlaymodeTint {
            get {
                try {
                    if(!EditorApplication.isPlayingOrWillChangePlaymode)
                        return Color.white;
                    return (Color)playModeColorProp.GetValue(playModeColor, null);
                }
                catch {
                    return Color.white;
                }
            }
        }
        public static EditorWindow HierarchyWindowInstance {
            get {
                if(hierarchyWindow)
                    return hierarchyWindow;

                if(lastHierarchyField != null)
                    return hierarchyWindow = (EditorWindow)lastHierarchyField.GetValue(null);

                return hierarchyWindow = (EditorWindow)Resources.FindObjectsOfTypeAll(hierarchyWindowType).FirstOrDefault();
            }
        }
        public static bool HierarchyFocused { get { return EditorWindow.focusedWindow && EditorWindow.focusedWindow.GetType() == hierarchyWindowType; } }
        public static BindingFlags FullBinding { get { return FULL_BINDING; } }

        public static PropertyInfo GetHierarchyTitleProperty() {
            return hierarchyWindowType.GetProperty("titleContent", FULL_BINDING) ?? hierarchyWindowType.GetProperty("title", FULL_BINDING);
        }

        public static void ShowIconSelector(Object targetObj, Rect activatorRect, bool showLabelIcons) {
            ShowIconSelector(targetObj, activatorRect, showLabelIcons, icon => { });
        }

        public static void ShowIconSelector(Object targetObj, Rect activatorRect, bool showLabelIcons, OnObjectIconChange onObjectChange) {
            using(ProfilerSample.Get())
                try {
                    var instance = ScriptableObject.CreateInstance(iconSelectorType);
                    var update = new EditorApplication.CallbackFunction(() => { });

                    iconSelectorInitMethod.Invoke(instance, new object[] { targetObj, activatorRect, showLabelIcons });
                    update += () => {
                        if(!instance) {
                            onObjectChange(GetObjectIcon(targetObj));
                            EditorApplication.update -= update;
                        }
                    };

                    EditorApplication.update += update;
                }
                catch(Exception e) {
                    Debug.LogWarning("Failed to open icon selector\n" + e);
                }
        }

        public static void SetObjectIcon(Object obj, Texture2D texture) {
            using(ProfilerSample.Get())
                setIconMethod.Invoke(null, new object[] { obj, texture });
            EditorUtility.SetDirty(obj);
        }

        public static Texture2D GetObjectIcon(Object obj) {
            using(ProfilerSample.Get())
                return (Texture2D)getIconMethod.Invoke(null, new object[] { obj });
        }

        public static void LoadTexture(Texture2D texture, byte[] data) {
            //Texture2D.LoadImage changed to an extension method in Unity 2017
            //Compiling it with the old method will make the module stop working on 2017
            //Compiling it with the extension method will make the module stop working with older versions
            if(imageConversionType == null)
                textureLoadMethod.Invoke(texture, new object[] { data });
            else
                textureLoadMethod.Invoke(null, new object[] { texture, data });
        }

        public static bool GetTransformIsExpanded(GameObject go) {
            using(ProfilerSample.Get())
                try {
                    if(treeviewInstance == null) {
                        treeviewInstance = treeviewProp.GetValue(HierarchyWindowInstance, null);
                        treeviewDataSourceInstance = treeviewDataSourceProp.GetValue(treeviewInstance, null);
                    }
                    else if(treeviewDataSourceInstance == null)
                        treeviewDataSourceInstance = treeviewDataSourceProp.GetValue(treeviewInstance, null);

                    return (bool)treeviewExpandedMethod.Invoke(treeviewDataSourceInstance, new object[] { go.GetInstanceID() });
                }
                catch(Exception e) {
                    Preferences.NumericChildExpand.Value = false;
                    Utility.LogException(e);
                    Debug.LogWarning(string.Format("Disabled \"{0}\" because it failed to get hierarchy info", Preferences.NumericChildExpand.Content.text));
                    return false;
                }
        }

        public static void SetHierarchySelectionNeedSync() {
            using(ProfilerSample.Get())
                try {
                    if(HierarchyWindowInstance)
                        syncNeededProp.SetValue(HierarchyWindowInstance, true, null);
                }
                catch(Exception e) {
                    Utility.LogException(e);
                }
        }

    }
}