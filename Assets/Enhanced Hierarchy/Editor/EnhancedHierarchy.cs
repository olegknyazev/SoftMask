/* Enhanced Hierarchy for Unity
 * Version 2.2.2, last change 12/06/2017
 * Samuel Schultze
 * samuelschultze@gmail.com
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EnhancedHierarchy {
    /// <summary>
    /// Main class, draws hierarchy items.
    /// </summary>
    [InitializeOnLoad]
    internal static partial class EnhancedHierarchy {

        static EnhancedHierarchy() {
            Utility.EnableFPSCounter();
            Utility.ForceUpdateHierarchyEveryFrame();

            EditorApplication.hierarchyWindowItemOnGUI += SetItemInformation;
            EditorApplication.hierarchyWindowItemOnGUI += OnItemGUI;
            EditorApplication.RepaintHierarchyWindow();
        }

        private static void OnItemGUI(int id, Rect rect) {
            if(!Preferences.Enabled)
                return;

            using(ProfilerSample.Get("Enhanced Hierarchy"))
                try {
                    if(IsGameObject) {
                        foreach(var icon in Preferences.RightIcons.Value)
                            icon.Init();

                        foreach(var icon in Preferences.LeftIcons.Value)
                            icon.Init();

                        Preferences.LeftSideButton.Value.Init();
                    }

                    SetTitle("EH 2.0");
                    CalculateIconsWidth();
                    DoSelection(RawRect);
                    IgnoreLockedSelection();
                    var trailingWidth = DoTrailing(RawRect);
                    ColorSort(RawRect);
                    DrawTree(RawRect);
                    DrawLeftSideIcons(RawRect);
                    ChildToggle();
                    DrawTooltip(RawRect, trailingWidth);

                    if(IsGameObject) {
                        rect.xMax -= Preferences.Offset;
                        rect.xMin = rect.xMax;
                        rect.y++;

                        foreach(var icon in Preferences.RightIcons.Value)
                            try {
                                using(new GUIBackgroundColor(Styles.backgroundColorEnabled)) {
                                    rect.xMin -= icon.Width;
                                    icon.DoGUI(rect);
                                    rect.xMax -= icon.Width;
                                }
                            }
                            catch(Exception e) {
                                Debug.LogException(e);
                                Preferences.ForceDisableButton(icon);
                            }

                        var leftSideRect = RawRect;

                        try {
                            if(Preferences.LeftmostButton)
                                leftSideRect.xMin = 0f;
                            else
                                leftSideRect.xMin -= 2f + CurrentGameObject.transform.childCount > 0 || Preferences.Tree ? 30f : 18f;

                            leftSideRect.xMax = leftSideRect.xMin + Preferences.LeftSideButton.Value.Width;

                            using(new GUIBackgroundColor(Styles.backgroundColorEnabled))
                                Preferences.LeftSideButton.Value.DoGUI(leftSideRect);
                        }
                        catch(Exception e) {
                            Debug.LogException(e);
                            Preferences.ForceDisableButton(Preferences.LeftSideButton.Value);
                        }
                    }

                    DrawMiniLabel(ref rect);
                    DrawHorizontalSeparator(RawRect);
                }
                catch(Exception e) {
                    Utility.LogException(e);
                }
        }

        private static void SetTitle(string title) {
            try {
                if(!IsFirstVisible || !IsRepaintEvent)
                    return;

                var titleProperty = ReflectionHelper.GetHierarchyTitleProperty();
                var isTitleContent = titleProperty.Name == "titleContent";

                if(isTitleContent) {
                    var content = (GUIContent)titleProperty.GetValue(ReflectionHelper.HierarchyWindowInstance, null);
                    content.text = title;
                    titleProperty.SetValue(ReflectionHelper.HierarchyWindowInstance, content, null);
                }
            }
            catch(Exception e) {
                Debug.LogWarning("Failed to set hieararchy title: " + e);
            }
        }

        private static void IgnoreLockedSelection() {
            if(Preferences.AllowSelectingLocked || !IsFirstVisible || !IsRepaintEvent)
                return;

            using(ProfilerSample.Get()) {
                var selection = Selection.objects;
                var changed = false;

                for(var i = 0; i < selection.Length; i++)
                    if(selection[i] is GameObject && (selection[i].hideFlags & HideFlags.NotEditable) != 0 && !EditorUtility.IsPersistent(selection[i])) {
                        selection[i] = null;
                        changed = true;
                    }

                if(changed) {
                    Selection.objects = selection;
                    ReflectionHelper.SetHierarchySelectionNeedSync();
                    EditorApplication.RepaintHierarchyWindow();
                }
            }
        }

        private static void ChildToggle() {
            using(ProfilerSample.Get()) {
                if(!Preferences.NumericChildExpand || !IsRepaintEvent || !IsGameObject || CurrentGameObject.transform.childCount <= 0)
                    return;

                var rect = RawRect;
                var childString = CurrentGameObject.transform.childCount.ToString("00");
                var expanded = ReflectionHelper.GetTransformIsExpanded(CurrentGameObject);

                rect.xMax = rect.xMin - 1f;
                rect.xMin -= 15f;

                if(childString.Length > 2)
                    rect.xMin -= 4f;

                tempChildExpandContent.text = childString;

                using(new GUIBackgroundColor(Styles.childToggleColor))
                    Styles.newToggleStyle.Draw(rect, tempChildExpandContent, false, false, expanded, false);
            }
        }

        private static void DrawHorizontalSeparator(Rect rect) {
            if(Preferences.LineSize < 1 || Preferences.LineColor.Value.a <= ALPHA_THRESHOLD || !IsRepaintEvent)
                return;

            using(ProfilerSample.Get()) {
                rect.xMin = 0f;
                rect.xMax = rect.xMax + 50f;
                rect.yMin -= Preferences.LineSize / 2;
                rect.yMax = rect.yMin + Preferences.LineSize;

                EditorGUI.DrawRect(rect, Preferences.LineColor);

                if(!IsFirstVisible)
                    return;

                rect.y = FinalRect.y - Preferences.LineSize / 2;

                var height = ReflectionHelper.HierarchyWindowInstance.position.height;
                var count = (height - FinalRect.y) / FinalRect.height;

                if(FinalRect.height <= 0f)
                    count = 100f;

                for(var i = 0; i < count; i++) {
                    rect.y += RawRect.height;
                    EditorGUI.DrawRect(rect, Preferences.LineColor);
                }
            }
        }

        private static void ColorSort(Rect rect) {
            if(!IsRepaintEvent)
                return;

            using(ProfilerSample.Get()) {
                rect.xMin = 0f;
                rect.xMax = rect.xMax + 50f;

                var color = Utility.OverlayColors(GetRowTint(), GetRowLayerTint());

                if(color.a > ALPHA_THRESHOLD)
                    EditorGUI.DrawRect(rect, color);

                if(!IsFirstVisible)
                    return;

                rect.y = FinalRect.y;

                var height = ReflectionHelper.HierarchyWindowInstance.position.height;
                var count = (height - FinalRect.y) / FinalRect.height;

                if(FinalRect.height <= 0f)
                    count = 100f;

                for(var i = 0; i < count; i++) {
                    rect.y += RawRect.height;
                    color = GetRowTint(rect);

                    if(color.a > ALPHA_THRESHOLD)
                        EditorGUI.DrawRect(rect, color);
                }
            }
        }

        private static void DrawTree(Rect rect) {
            if(!Preferences.Tree || !IsGameObject || !IsRepaintEvent)
                return;

            using(ProfilerSample.Get())
            using(new GUIColor(CurrentColor)) {
                rect.xMin -= 14f;
                rect.xMax = rect.xMin + 14f;

                if(CurrentGameObject.transform.childCount == 0 && CurrentGameObject.transform.parent)
                    if(Utility.LastInHierarchy(CurrentGameObject.transform))
                        GUI.DrawTexture(rect, Styles.treeEndTexture);
                    else
                        GUI.DrawTexture(rect, Styles.treeMiddleTexture);

                var parent = CurrentGameObject.transform.parent;

                for(rect.x -= 14f; rect.xMin > 0f && parent && parent.parent; rect.x -= 14f) {
                    if(!Utility.LastInHierarchy(parent))
                        using(new GUIColor(Utility.GetHierarchyColor(parent.parent)))
                            GUI.DrawTexture(rect, Styles.treeLineTexture);
                    parent = parent.parent;
                }
            }
        }

        private static void CalculateIconsWidth() {
            using(ProfilerSample.Get()) {
                LeftIconsWidth = 0f;
                RightIconsWidth = 0f;

                if(!IsGameObject || !IsRepaintEvent)
                    return;

                foreach(var icon in Preferences.RightIcons.Value)
                    RightIconsWidth += icon.Width;

                foreach(var icon in Preferences.LeftIcons.Value)
                    LeftIconsWidth += icon.Width;
            }
        }

        private static void DrawLeftSideIcons(Rect rect) {
            if(!IsGameObject || LeftIconsWidth == 0f)
                return;

            using(ProfilerSample.Get()) {
                rect.xMin += LabelSize;
                rect.xMin = Math.Min(rect.xMax - RightIconsWidth - LeftIconsWidth - CalcMiniLabelSize() - 5f - Preferences.Offset, rect.xMin);

                foreach(var icon in Preferences.LeftIcons.Value)
                    try {
                        rect.xMax = rect.xMin + icon.Width;
                        icon.DoGUI(rect);
                        rect.xMin = rect.xMax;
                    }
                    catch(Exception e) {
                        Debug.LogException(e);
                        Preferences.ForceDisableButton(icon);
                    }
            }
        }

        private static float DoTrailing(Rect rect) {
            if(!IsRepaintEvent || !Preferences.Trailing || !IsGameObject)
                return rect.xMax;

            using(ProfilerSample.Get()) {
                tempGameObjectNameContent.text = CurrentGameObject.name;

                var size = CurrentStyle.CalcSize(tempGameObjectNameContent);
                var iconsWidth = RightIconsWidth + LeftIconsWidth + CalcMiniLabelSize() + Preferences.Offset;

                if(size.x < rect.width - iconsWidth + 15f)
                    return rect.xMax;

                rect.yMin += 2f;
                rect.xMin = rect.xMax - iconsWidth - 18f;

                if(Selection.gameObjects.Contains(CurrentGameObject))
                    EditorGUI.DrawRect(rect, ReflectionHelper.HierarchyFocused ? Styles.selectedFocusedColor : Styles.selectedUnfocusedColor);
                else
                    EditorGUI.DrawRect(rect, Styles.normalColor);

                rect.x -= 16f;
                rect.yMin -= 1f;
                rect.yMax -= 3f;

                EditorGUI.LabelField(rect, trailingContent, CurrentStyle);

                return rect.xMin + 28f;
            }
        }

        private static void TagMiniLabel(ref Rect rect) {
            if(Event.current.type == EventType.Layout)
                return;

            using(ProfilerSample.Get())
            using(new GUIContentColor(CurrentColor * new Color(1f, 1f, 1f, CurrentGameObject.tag == UNTAGGED ? Styles.backgroundColorDisabled.a : Styles.backgroundColorEnabled.a))) {
                GUI.changed = false;
                Styles.miniLabelStyle.fontSize = Preferences.SmallerMiniLabel ? 8 : 9;

                rect.xMin -= Styles.miniLabelStyle.CalcSize(new GUIContent(CurrentGameObject.tag)).x;

                var tag = EditorGUI.TagField(rect, CurrentGameObject.tag, Styles.miniLabelStyle);

                if(GUI.changed)
                    Icons.Tag.ChangeTagAndAskForChildren(GetSelectedObjectsAndCurrent(), tag);
            }
        }

        private static void LayerMiniLabel(ref Rect rect) {
            if(Event.current.type == EventType.Layout)
                return;

            using(ProfilerSample.Get())
            using(new GUIContentColor(CurrentColor * new Color(1f, 1f, 1f, CurrentGameObject.layer == UNLAYERED ? Styles.backgroundColorDisabled.a : Styles.backgroundColorEnabled.a))) {
                GUI.changed = false;
                Styles.miniLabelStyle.fontSize = Preferences.SmallerMiniLabel ? 8 : 9;

                rect.xMin -= Styles.miniLabelStyle.CalcSize(new GUIContent(LayerMask.LayerToName(CurrentGameObject.layer))).x;

                var layer = EditorGUI.LayerField(rect, CurrentGameObject.layer, Styles.miniLabelStyle);

                if(GUI.changed)
                    Icons.Layer.ChangeLayerAndAskForChildren(GetSelectedObjectsAndCurrent(), layer);
            }
        }

        private static void DrawMiniLabel(ref Rect rect) {
            if(Preferences.MiniLabelType.Value == MiniLabelType.None || !IsGameObject)
                return;

            rect.x -= 3f;

            using(ProfilerSample.Get())
                switch(Preferences.MiniLabelType.Value) {
                    case MiniLabelType.Tag:
                        if(HasTag)
                            TagMiniLabel(ref rect);
                        break;

                    case MiniLabelType.Layer:
                        if(HasLayer)
                            LayerMiniLabel(ref rect);
                        break;

                    case MiniLabelType.TagOrLayer:
                        if(HasTag)
                            TagMiniLabel(ref rect);
                        else if(HasLayer)
                            LayerMiniLabel(ref rect);
                        break;

                    case MiniLabelType.LayerOrTag:
                        if(HasLayer)
                            LayerMiniLabel(ref rect);
                        else if(HasTag)
                            TagMiniLabel(ref rect);
                        break;

                    case MiniLabelType.TagAndLayer:
                        if(HasTag && HasLayer || !Preferences.CentralizeMiniLabelWhenPossible) {
                            var topRect = rect;
                            var bottomRect = rect;

                            topRect.yMax = RawRect.yMax - RawRect.height / 2f;
                            bottomRect.yMin = RawRect.yMin + RawRect.height / 2f;

                            if(HasTag)
                                TagMiniLabel(ref topRect);
                            if(HasLayer)
                                LayerMiniLabel(ref bottomRect);

                            rect.xMin = Mathf.Min(topRect.xMin, bottomRect.xMin);
                        }
                        else if(HasLayer)
                            LayerMiniLabel(ref rect);
                        else if(HasTag)
                            TagMiniLabel(ref rect);

                        break;

                    case MiniLabelType.LayerAndTag:
                        if(HasTag && HasLayer || !Preferences.CentralizeMiniLabelWhenPossible) {
                            var topRect = rect;
                            var bottomRect = rect;

                            topRect.yMax = RawRect.yMax - RawRect.height / 2f;
                            bottomRect.yMin = RawRect.yMin + RawRect.height / 2f;

                            if(HasLayer)
                                LayerMiniLabel(ref topRect);
                            if(HasTag)
                                TagMiniLabel(ref bottomRect);

                            rect.xMin = Mathf.Min(topRect.xMin, bottomRect.xMin);
                        }
                        else if(HasLayer)
                            LayerMiniLabel(ref rect);
                        else if(HasTag)
                            TagMiniLabel(ref rect);

                        break;
                }
        }

        private static float CalcMiniLabelSize() {
            Styles.miniLabelStyle.fontSize = Preferences.SmallerMiniLabel ? 8 : 9;

            using(ProfilerSample.Get())
                switch(Preferences.MiniLabelType.Value) {
                    case MiniLabelType.Tag:
                        if(HasTag)
                            return Styles.miniLabelStyle.CalcSize(new GUIContent(CurrentGameObject.tag)).x;
                        else
                            return 0f;

                    case MiniLabelType.Layer:
                        if(HasLayer)
                            return Styles.miniLabelStyle.CalcSize(new GUIContent(LayerMask.LayerToName(CurrentGameObject.layer))).x;
                        else
                            return 0f;

                    case MiniLabelType.TagOrLayer:
                        if(HasTag)
                            return Styles.miniLabelStyle.CalcSize(new GUIContent(CurrentGameObject.tag)).x;
                        else if(HasLayer)
                            return Styles.miniLabelStyle.CalcSize(new GUIContent(LayerMask.LayerToName(CurrentGameObject.layer))).x;
                        else
                            return 0f;

                    case MiniLabelType.LayerOrTag:
                        if(HasLayer)
                            return Styles.miniLabelStyle.CalcSize(new GUIContent(LayerMask.LayerToName(CurrentGameObject.layer))).x;
                        else if(HasTag)
                            return Styles.miniLabelStyle.CalcSize(new GUIContent(CurrentGameObject.tag)).x;
                        else
                            return 0f;

                    case MiniLabelType.TagAndLayer:
                    case MiniLabelType.LayerAndTag:
                        var tagSize = 0f;
                        var layerSize = 0f;

                        if(HasTag)
                            tagSize = Styles.miniLabelStyle.CalcSize(new GUIContent(CurrentGameObject.tag)).x;
                        if(HasLayer)
                            layerSize = Styles.miniLabelStyle.CalcSize(new GUIContent(LayerMask.LayerToName(CurrentGameObject.layer))).x;

                        return Mathf.Max(tagSize, layerSize);

                    default:
                        return 0f;
                }
        }

        private static void DrawTooltip(Rect rect, float fullTrailingWidth) {
            if(!Preferences.Tooltips || !IsGameObject || !IsRepaintEvent)
                return;

            using(ProfilerSample.Get()) {
                if(DragSelection != null)
                    return;

                rect.xMax = Mathf.Min(fullTrailingWidth, rect.xMin + LabelSize);

                if(!rect.Contains(Event.current.mousePosition))
                    return;

                var tooltip = new StringBuilder(100);

                tooltip.AppendLine(CurrentGameObject.name);
                tooltip.AppendFormat("\nTag: {0}", CurrentGameObject.tag);
                tooltip.AppendFormat("\nLayer: {0}", LayerMask.LayerToName(CurrentGameObject.layer));

                if(GameObjectUtility.GetStaticEditorFlags(CurrentGameObject) != 0)
                    tooltip.AppendFormat("\nStatic: {0}", Utility.EnumFlagsToString(GameObjectUtility.GetStaticEditorFlags(CurrentGameObject)));

                tooltip.AppendLine();
                tooltip.AppendLine();

                var components = CurrentGameObject.GetComponents<Component>();

                foreach(var component in components)
                    if(component is Transform)
                        continue;
                    else if(component)
                        tooltip.AppendLine(ObjectNames.GetInspectorTitle(component));
                    else
                        tooltip.AppendLine("Missing Component");

                tempTooltipContent.tooltip = tooltip.ToString().TrimEnd('\n', '\r');
                EditorGUI.LabelField(rect, tempTooltipContent);
            }
        }

        private static void DoSelection(Rect rect) {
            if(!Preferences.EnhancedSelection || Event.current.button != 1) {
                DragSelection = null;
                return;
            }

            using(ProfilerSample.Get()) {
                rect.xMin = 0f;

                switch(Event.current.type) {
                    case EventType.MouseDrag:
                        if(!IsFirstVisible)
                            return;

                        if(DragSelection == null) {
                            DragSelection = new List<Object>();
                            SelectionStart = Event.current.mousePosition;
                            SelectionRect = new Rect();
                        }

                        SelectionRect = new Rect() {
                            xMin = Mathf.Min(Event.current.mousePosition.x, SelectionStart.x),
                            yMin = Mathf.Min(Event.current.mousePosition.y, SelectionStart.y),
                            xMax = Mathf.Max(Event.current.mousePosition.x, SelectionStart.x),
                            yMax = Mathf.Max(Event.current.mousePosition.y, SelectionStart.y)
                        };

                        if(Event.current.control || Event.current.command)
                            DragSelection.AddRange(Selection.objects);

                        Selection.objects = DragSelection.ToArray();
                        Event.current.Use();
                        break;

                    case EventType.MouseUp:
                        if(DragSelection != null)
                            Event.current.Use();
                        DragSelection = null;
                        break;

                    case EventType.Repaint:
                        if(DragSelection == null || !IsFirstVisible)
                            break;

                        var scrollRect = new Rect();

                        if(Event.current.mousePosition.y > FinalRect.y) {
                            scrollRect = FinalRect;
                            scrollRect.y += scrollRect.height;
                        }
                        else if(Event.current.mousePosition.y < RawRect.y) {
                            scrollRect = RawRect;
                            scrollRect.y -= scrollRect.height;
                        }
                        else
                            break;

                        SelectionRect = new Rect() {
                            xMin = Mathf.Min(scrollRect.xMax, SelectionStart.x),
                            yMin = Mathf.Min(scrollRect.yMax, SelectionStart.y),
                            xMax = Mathf.Max(scrollRect.xMax, SelectionStart.x),
                            yMax = Mathf.Max(scrollRect.yMax, SelectionStart.y)
                        };

                        if(Event.current.control || Event.current.command)
                            DragSelection.AddRange(Selection.objects);

                        Selection.objects = DragSelection.ToArray();

                        GUI.ScrollTowards(scrollRect, 9f);
                        EditorApplication.RepaintHierarchyWindow();
                        break;

                    case EventType.Layout:
                        if(DragSelection != null && IsGameObject)
                            if(!SelectionRect.Overlaps(rect) && DragSelection.Contains(CurrentGameObject))
                                DragSelection.Remove(CurrentGameObject);
                            else if(SelectionRect.Overlaps(rect) && !DragSelection.Contains(CurrentGameObject))
                                DragSelection.Add(CurrentGameObject);
                        break;
                }
            }
        }

        public static Color GetRowTint() {
            return GetRowTint(RawRect);
        }

        public static Color GetRowTint(Rect rect) {
            using(ProfilerSample.Get())
                if(rect.y / RawRect.height % 2 >= 0.5f)
                    return Preferences.OddRowColor;
                else
                    return Preferences.EvenRowColor;
        }

        public static Color GetRowLayerTint() {
            return GetRowLayerTint(RawRect, CurrentGameObject);
        }

        public static Color GetRowLayerTint(Rect rect, GameObject go) {
            using(ProfilerSample.Get())
                if(go)
                    return Array.Find<LayerColor>(Preferences.PerLayerRowColors, layer => { return layer == go.layer; }).color;
                else
                    return Color.clear;
        }

        private static List<GameObject> GetSelectedObjectsAndCurrent() {
            if(!Preferences.ChangeAllSelected || Selection.gameObjects.Length <= 1)
                return new List<GameObject> { CurrentGameObject };

            var selection = new List<GameObject>(Selection.gameObjects);

            for(var i = 0; i < selection.Count; i++)
                if(EditorUtility.IsPersistent(selection[i]))
                    selection.RemoveAt(i);

            if(!selection.Contains(CurrentGameObject))
                selection.Add(CurrentGameObject);

            selection.Remove(null);
            return selection;
        }
    }
}