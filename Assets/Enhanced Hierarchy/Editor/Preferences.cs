using System;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace EnhancedHierarchy {

    /// <summary>
    /// Serializable Vector to save Color, Vector2, Vector3 and Vector4.
    /// </summary>
    [Serializable]
    internal struct SerializableVector {

        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float W { get; set; }

        public SerializableVector(float x) { X = x; Y = 0f; Z = 0f; W = 0f; }
        public SerializableVector(float x, float y) { X = x; Y = y; Z = 0f; W = 0f; }
        public SerializableVector(float x, float y, float z) { X = x; Y = y; Z = z; W = 0f; }
        public SerializableVector(float x, float y, float z, float w) { X = x; Y = y; Z = z; W = w; }

        public static SerializableVector GetVectorFromObject(object obj) {
            if(obj is Color)
                return (Color)obj;
            if(obj is Vector2)
                return (Vector2)obj;
            if(obj is Vector3)
                return (Vector3)obj;
            if(obj is Vector4)
                return (Vector4)obj;
            throw new InvalidCastException();
        }

        public static implicit operator Color(SerializableVector v) { return new Color(v.X, v.Y, v.Z, v.W); }
        public static implicit operator Vector2(SerializableVector v) { return new Vector2(v.X, v.Y); }
        public static implicit operator Vector3(SerializableVector v) { return new Vector3(v.X, v.Y, v.Z); }
        public static implicit operator Vector4(SerializableVector v) { return new Vector4(v.X, v.Y, v.Z, v.W); }

        public static implicit operator SerializableVector(Color c) { return new SerializableVector(c.r, c.g, c.b, c.a); }
        public static implicit operator SerializableVector(Vector2 v) { return new SerializableVector(v.x, v.y); }
        public static implicit operator SerializableVector(Vector3 v) { return new SerializableVector(v.x, v.y, v.z); }
        public static implicit operator SerializableVector(Vector4 v) { return new SerializableVector(v.x, v.y, v.z, v.w); }
    }

    /// <summary>
    /// Per layer color setting.
    /// </summary>
    [Serializable]
    internal struct LayerColor {
        public int layer;
        public SerializableVector color;

        public LayerColor(int layer) : this(layer, Color.clear) { }

        public LayerColor(int layer, Color color) {
            this.layer = layer;
            this.color = color;
        }

        public static implicit operator LayerColor(int layer) {
            return new LayerColor(layer);
        }

        public static bool operator ==(LayerColor left, LayerColor right) {
            return left.layer == right.layer;
        }

        public static bool operator !=(LayerColor left, LayerColor right) {
            return left.layer != right.layer;
        }

        public override bool Equals(object obj) {
            if(obj == null || !(obj is LayerColor))
                return false;

            return ((LayerColor)obj).layer == layer;
        }

        public override int GetHashCode() {
            return layer.GetHashCode();
        }
    }

    /// <summary>
    /// Save and load hierarchy preferences.
    /// </summary>
    internal static class Preferences {
        public static int currentKeyIndex;
        public static string[] allKeys = new string[128];

        private static Color DefaultOddSortColor { get { return EditorGUIUtility.isProSkin ? new Color(0f, 0f, 0f, 0.10f) : new Color(1f, 1f, 1f, 0.20f); } }
        private static Color DefaultEvenSortColor { get { return EditorGUIUtility.isProSkin ? new Color(0f, 0f, 0f, 0f) : new Color(1f, 1f, 1f, 0f); } }
        private static Color DefaultLineColor { get { return new Color(0f, 0f, 0f, 0.2f); } }

        private static readonly GUIContent resetSettingsContent = new GUIContent("Use Defaults", "Reset all settings to default ones");
        private static readonly GUIContent unlockAllContent = new GUIContent("Unlock All Objects", "Unlock all objects in the current scene, it's highly recommended to do this when disabling or removing the extension to prevent data loss\nThis might take a few seconds on large scenes");

        public static PrefItem<int> Offset { get; private set; }
        public static PrefItem<int> LineSize { get; private set; }
        public static PrefItem<bool> Enabled { get; private set; }
        public static PrefItem<bool> Tree { get; private set; }
        public static PrefItem<bool> Tooltips { get; private set; }
        public static PrefItem<bool> RelevantTooltipsOnly { get; private set; }
        public static PrefItem<bool> EnhancedSelection { get; private set; }
        public static PrefItem<bool> Trailing { get; private set; }
        public static PrefItem<bool> AllowSelectingLocked { get; private set; }
        public static PrefItem<bool> AllowSelectingLockedSceneView { get; private set; }
        public static PrefItem<bool> ChangeAllSelected { get; private set; }
        public static PrefItem<bool> LeftmostButton { get; private set; }
        public static PrefItem<bool> NumericChildExpand { get; private set; }
        public static PrefItem<bool> SmallerMiniLabel { get; private set; }
        public static PrefItem<bool> CentralizeMiniLabelWhenPossible { get; private set; }
        public static PrefItem<bool> HideDefaultTag { get; private set; }
        public static PrefItem<bool> HideDefaultLayer { get; private set; }
        public static PrefItem<bool> HideDefaultIcon { get; private set; }
        public static PrefItem<Color> OddRowColor { get; private set; }
        public static PrefItem<Color> EvenRowColor { get; private set; }
        public static PrefItem<Color> LineColor { get; private set; }
        public static PrefItem<RightSideIcon> LeftSideButton { get; private set; }
        public static PrefItem<MiniLabelType> MiniLabelType { get; private set; }
        public static PrefItem<ChildrenChangeMode> LockAskMode { get; private set; }
        public static PrefItem<ChildrenChangeMode> LayerAskMode { get; private set; }
        public static PrefItem<ChildrenChangeMode> TagAskMode { get; private set; }
        public static PrefItem<ChildrenChangeMode> StaticAskMode { get; private set; }
        public static PrefItem<ChildrenChangeMode> IconAskMode { get; private set; }
        public static PrefItem<LeftSideIcon[]> LeftIcons { get; private set; }
        public static PrefItem<RightSideIcon[]> RightIcons { get; private set; }
        public static PrefItem<LayerColor[]> PerLayerRowColors { get; private set; }

        public static bool MiniLabelTagEnabled {
            get {
                switch(MiniLabelType.Value) {
                    case global::EnhancedHierarchy.MiniLabelType.Tag:
                    case global::EnhancedHierarchy.MiniLabelType.TagOrLayer:
                    case global::EnhancedHierarchy.MiniLabelType.LayerOrTag:
                    case global::EnhancedHierarchy.MiniLabelType.LayerAndTag:
                    case global::EnhancedHierarchy.MiniLabelType.TagAndLayer:
                        return true;

                    default:
                        return false;
                }
            }
        }
        public static bool MiniLabelLayerEnabled {
            get {
                switch(MiniLabelType.Value) {
                    case global::EnhancedHierarchy.MiniLabelType.Layer:
                    case global::EnhancedHierarchy.MiniLabelType.LayerOrTag:
                    case global::EnhancedHierarchy.MiniLabelType.TagOrLayer:
                    case global::EnhancedHierarchy.MiniLabelType.LayerAndTag:
                    case global::EnhancedHierarchy.MiniLabelType.TagAndLayer:
                        return true;

                    default:
                        return false;
                }
            }
        }

        //Just to serialize the scroll on assembly reloads
        private static PrefItem<Vector2> scroll = new PrefItem<Vector2>("scroll", Vector2.zero, string.Empty, string.Empty);
        private static ReorderableList leftIconsList, rightIconsList, rowColorsList;

        private static GenericMenu LeftIconsMenu { get { return GetGenericMenuForList(leftIconsList, LeftSideIcon.AllIcons); } }
        private static GenericMenu RightIconsMenu { get { return GetGenericMenuForList(rightIconsList, RightSideIcon.AllIcons); } }

        private static GenericMenu GetGenericMenuForList(ReorderableList list, IconBase[] icons) {
            var menu = new GenericMenu();

            foreach(var i in icons) {
                var icon = i;
                if(!list.list.Contains(icon) && icon != new Icons.LeftNone() && icon != new Icons.RightNone())
                    menu.AddItem(new GUIContent(icon.Name), false, () => list.list.Add(icon));
            }

            return menu;
        }

        private static ReorderableList GenerateReordableListForIcons<T>(PrefItem<T[]> preferenceItem) {
            var result = new ReorderableList(preferenceItem.Value.ToList(), typeof(T), true, true, true, true);

            result.elementHeight = 18f;
            result.drawHeaderCallback = rect => { rect.xMin -= EditorGUI.indentLevel * 16f; EditorGUI.LabelField(rect, preferenceItem, EditorStyles.boldLabel); };
            result.drawElementCallback = (rect, index, focused, active) => EditorGUI.LabelField(rect, result.list[index].ToString());
            result.onAddDropdownCallback = (rect, newList) => (typeof(T) == typeof(RightSideIcon) ? RightIconsMenu : LeftIconsMenu).DropDown(rect);

            return result;
        }

        public static void ReloadPrefs() {
            Enabled = new PrefItem<bool>("Enabled", true, string.Format("Enabled ({0}+H)", Utility.CtrlKey), "Enable or disable the entire plugin, it will be automatically disabled if any error occurs");
            Offset = new PrefItem<int>("Offset", 2, "Offset", "Offset for icons, useful if you have more extensions that also uses hierarchy");
            Tree = new PrefItem<bool>("Tree", true, "Hierarchy tree", "Shows lines connecting child transforms to their parent, useful if you have multiple childs inside childs");
            Tooltips = new PrefItem<bool>("Tooltip", true, "Tooltips", "Shows tooltips, like this one");
            RelevantTooltipsOnly = new PrefItem<bool>("RelevantTooltips", true, "Relevant Tooltips", "Show only tooltips with relevant informations");
            EnhancedSelection = new PrefItem<bool>("Selection", true, "Enhanced selection", "Allow selecting GameObjects by dragging over them with right mouse button");
            LeftSideButton = new PrefItem<RightSideIcon>("LeftSideButton", new Icons.GameObjectIcon(), "Left side button", "The button that will appear in the left side of the hierarchy\nLooks better with \"Hierarchy Tree\" disabled");
            LeftmostButton = new PrefItem<bool>("LeftmostSideButton", true, "Left side button at leftmost", "Put the left button to the leftmost side of the hierachy, if disabled it will be next to the game object name");
            MiniLabelType = new PrefItem<MiniLabelType>("MiniLabel", global::EnhancedHierarchy.MiniLabelType.TagAndLayer, "Mini label", "The little label next to the GameObject name");
            Trailing = new PrefItem<bool>("Trailing", true, "Trailing", "Append ... when names are bigger than the view area");
            AllowSelectingLocked = new PrefItem<bool>("SelectLocked", true, "Allow locked selection (Hierarchy)", "Allow selecting objects that are locked");
            AllowSelectingLockedSceneView = new PrefItem<bool>("SelectLockedSV", false, "Allow locked selection (Scene View)", "Allow selecting objects that are locked on scene view\nObjects locked before you change this option will have the previous behaviour, you need to unlock and lock them again to apply this setting");
            ChangeAllSelected = new PrefItem<bool>("ChangeAllLocked", true, "Change all selected", "This will make the enable, lock, layer, tag and static buttons affect all selected objects in the hierarchy");
            NumericChildExpand = new PrefItem<bool>("ReplaceToggle", false, "Replace default child toggle", "Replace the default toggle for expanding children for a new one that shows the child count");
            SmallerMiniLabel = new PrefItem<bool>("SmallerMiniLabel", true, "Smaller font", "Use a smaller font on the minilabel for narrow hierarchies");
            CentralizeMiniLabelWhenPossible = new PrefItem<bool>("CentralizeWhenPossible", true, "Centralize when possible", "Centralize minilabel when there's only tag or only layer on it");
            HideDefaultLayer = new PrefItem<bool>("HideDefaultLayer", true, "Hide \"Default\" layer", "Hide default layer on minilabel");
            HideDefaultTag = new PrefItem<bool>("HideDefaultTag", true, "Hide \"Untagged\" tag", "Hide default tag on minilabel");
            HideDefaultIcon = new PrefItem<bool>("HideDefaultIcon", false, "Hide default icon", "Hide the default game object icon");

            StaticAskMode = new PrefItem<ChildrenChangeMode>("StaticMode", ChildrenChangeMode.Ask, "Static", "Which flags will be changed when you click on the static toggle");
            IconAskMode = new PrefItem<ChildrenChangeMode>("IconAskMode", ChildrenChangeMode.ObjectOnly, "Icon", "Which objects will have their icon changed when you click on the icon button");
            LockAskMode = new PrefItem<ChildrenChangeMode>("LockMode", ChildrenChangeMode.ObjectAndChildren, "Lock", "Which objects will be locked when you click on the lock toggle");
            LayerAskMode = new PrefItem<ChildrenChangeMode>("LayerMode", ChildrenChangeMode.Ask, "Layer", "Which objects will have their layer changed when you click on the layer button or on the mini label");
            TagAskMode = new PrefItem<ChildrenChangeMode>("TagMode", ChildrenChangeMode.ObjectOnly, "Tag", "Which objects will have their tag changed when you click on the tag button or on the mini label");

            LineSize = new PrefItem<int>("LineSize", 1, "Line thickness", "Separator line thickness");
            LineColor = new PrefItem<Color>("LineColor", DefaultLineColor, "Line color", "The color used on separators line");
            OddRowColor = new PrefItem<Color>("OddRow", DefaultOddSortColor, "Odd row color", "The color used on odd rows");
            EvenRowColor = new PrefItem<Color>("EvenRow", DefaultEvenSortColor, "Even row color", "The color used on even rows");

            var defaultLeftIcons = new LeftSideIcon[] { new Icons.MonoBehaviourIcon(), new Icons.Warnings(), new Icons.SoundIcon() };
            var defaultRightIcons = new RightSideIcon[] { new Icons.Active(), new Icons.Lock(), new Icons.Static(), new Icons.PrefabApply() };
            var defaultLayerColors = new LayerColor[] { new LayerColor(5, new Color(0.8f, 0f, 1f, EditorGUIUtility.isProSkin ? 0.1f : 0.145f)) };

            LeftIcons = new PrefItem<LeftSideIcon[]>("LeftIcons", defaultLeftIcons, "Left Side Icons", "The icons that appear next to the game object name");
            RightIcons = new PrefItem<RightSideIcon[]>("RightIcons", defaultRightIcons, "Right Side Icons", "The icons that appear to the rightmost of the hierarchy");
            PerLayerRowColors = new PrefItem<LayerColor[]>("PerLayerRowColors", defaultLayerColors, "Per layer row color", "Set a row color for each different layer");

            leftIconsList = GenerateReordableListForIcons(LeftIcons);
            rightIconsList = GenerateReordableListForIcons(RightIcons);

            rowColorsList = GenerateReordableListForIcons(PerLayerRowColors);
            rowColorsList.draggable = false;
            rowColorsList.onAddDropdownCallback = null;

            rowColorsList.drawElementCallback = (rect, index, focused, active) => {
                rect.xMin -= EditorGUI.indentLevel * 16f;

                var value = (LayerColor)rowColorsList.list[index];
                var rect1 = rect;
                var rect2 = rect;
                var rect3 = rect;

                rect1.xMax = rect1.xMin + 175f;
                rect2.xMin = rect1.xMax;
                rect2.xMax = rect2.xMin + 80f;
                rect3.xMin = rect2.xMax;

                value.layer = EditorGUI.LayerField(rect1, value.layer);
                value.layer = EditorGUI.IntField(rect2, value.layer);
                value.color = EditorGUI.ColorField(rect3, value.color);

                if(value.layer > 31 || value.layer < 0)
                    value.layer = 0;

                rowColorsList.list[index] = value;
            };
        }

        public static bool IsButtonEnabled(IconBase button) {
            if(button == null)
                return false;

            if(LeftSideButton.Value == button)
                return true;

            if(button is RightSideIcon)
                return RightIcons.Value.Contains((RightSideIcon)button);
            else
                return LeftIcons.Value.Contains((LeftSideIcon)button);
        }

        public static void ForceDisableButton(IconBase button) {
            Debug.LogWarning("Disabling \"" + button.Name + "\", most likely because it threw an exception");

            if(LeftSideButton.Value == button)
                LeftSideButton.Value = new Icons.RightNone();
            else if(button is RightSideIcon)
                RightIcons.Value = (from icon in RightIcons.Value
                                    where icon != button
                                    select icon).ToArray();
            else
                LeftIcons.Value = (from icon in LeftIcons.Value
                                   where icon != button
                                   select icon).ToArray();
        }

        public static void DeleteSavedValues() {
            foreach(var key in allKeys)
                EditorPrefs.DeleteKey(key);
            currentKeyIndex = 0;
            ReloadPrefs();
        }

        [PreferenceItem("Hierarchy")]
        private static void OnPreferencesGUI() {
            scroll.Value = EditorGUILayout.BeginScrollView(scroll, false, false);

            EditorGUILayout.Separator();

            Enabled.DoGUI();
            EditorGUILayout.Separator();

            using(Enabled.GetEnabledScope()) {
                using(new GUIIndent("Misc settings")) {
                    Offset.DoGUI();
                    Tree.DoGUI();
                    Tooltips.DoGUI();
                    using(RelevantTooltipsOnly.GetFadeScope(Tooltips))
                        RelevantTooltipsOnly.DoGUI();
                    EnhancedSelection.DoGUI();
                    Trailing.DoGUI();
                    ChangeAllSelected.DoGUI();
                    NumericChildExpand.DoGUI();

                    using(HideDefaultIcon.GetFadeScope(IsButtonEnabled(new Icons.GameObjectIcon())))
                        HideDefaultIcon.DoGUI();

                    GUI.changed = false;

                    using(AllowSelectingLocked.GetFadeScope(IsButtonEnabled(new Icons.Lock())))
                        AllowSelectingLocked.DoGUI();

                    using(AllowSelectingLockedSceneView.GetFadeScope(IsButtonEnabled(new Icons.Lock()) && AllowSelectingLocked))
                        AllowSelectingLockedSceneView.DoGUI();

                    if(GUI.changed && EditorUtility.DisplayDialog("Relock all objects",
                        "Would you like to relock all objects?\n" +
                        "This is recommended when changing this setting and might take a few seconds on large scenes" +
                        "\nIt's also recommended to do this on all scenes", "Yes", "No"))
                        Utility.RelockAllObjects();
                }

                using(new GUIIndent("Row separators")) {
                    LineSize.DoSlider(0, 6);

                    using(LineColor.GetFadeScope(LineSize > 0))
                        LineColor.DoGUI();

                    OddRowColor.DoGUI();
                    EvenRowColor.DoGUI();

                    GUI.changed = false;
                    var rect = EditorGUILayout.GetControlRect(false, rowColorsList.GetHeight());
                    rect.xMin += EditorGUI.indentLevel * 16f;
                    rowColorsList.DoList(rect);

                    if(GUI.changed)
                        PerLayerRowColors.Value = rowColorsList.list.Cast<LayerColor>().ToArray();
                }

                using(new GUIIndent(MiniLabelType)) {
                    using(SmallerMiniLabel.GetFadeScope(MiniLabelType.Value != global::EnhancedHierarchy.MiniLabelType.None))
                        SmallerMiniLabel.DoGUI();
                    using(HideDefaultTag.GetFadeScope(MiniLabelTagEnabled))
                        HideDefaultTag.DoGUI();
                    using(HideDefaultLayer.GetFadeScope(MiniLabelLayerEnabled))
                        HideDefaultLayer.DoGUI();
                    using(CentralizeMiniLabelWhenPossible.GetFadeScope((HideDefaultLayer || HideDefaultTag) && (MiniLabelType.Value == global::EnhancedHierarchy.MiniLabelType.TagAndLayer || MiniLabelType.Value == global::EnhancedHierarchy.MiniLabelType.LayerAndTag)))
                        CentralizeMiniLabelWhenPossible.DoGUI();
                }

                using(new GUIIndent(LeftSideButton))
                using(LeftmostButton.GetFadeScope(LeftSideButton.Value != new Icons.RightNone()))
                    LeftmostButton.DoGUI();

                using(new GUIIndent("Children behaviour on change")) {
                    using(LockAskMode.GetFadeScope(IsButtonEnabled(new Icons.Lock())))
                        LockAskMode.DoGUI();
                    using(LayerAskMode.GetFadeScope(IsButtonEnabled(new Icons.Layer()) || MiniLabelLayerEnabled))
                        LayerAskMode.DoGUI();
                    using(TagAskMode.GetFadeScope(IsButtonEnabled(new Icons.Tag()) || MiniLabelTagEnabled))
                        TagAskMode.DoGUI();
                    using(StaticAskMode.GetFadeScope(IsButtonEnabled(new Icons.Static())))
                        StaticAskMode.DoGUI();
                    using(IconAskMode.GetFadeScope(IsButtonEnabled(new Icons.GameObjectIcon())))
                        IconAskMode.DoGUI();

                    EditorGUILayout.HelpBox(string.Format("Tip: Pressing down {0} while clicking on a button will make it temporary have the opposite children change mode", Utility.CtrlKey), MessageType.Info);
                }

                leftIconsList.displayAdd = LeftIconsMenu.GetItemCount() > 0;
                leftIconsList.DoLayoutList();
                LeftIcons.Value = leftIconsList.list.Cast<LeftSideIcon>().ToArray();

                rightIconsList.displayAdd = RightIconsMenu.GetItemCount() > 0;
                rightIconsList.DoLayoutList();
                RightIcons.Value = rightIconsList.list.Cast<RightSideIcon>().ToArray();

                if(IsButtonEnabled(new Icons.Lock()))
                    EditorGUILayout.HelpBox("Remember to always unlock your game objects when removing or disabling this extension, as you won't be able to unlock without it and may lose scene data", MessageType.Warning);

                GUI.enabled = true;
                EditorGUILayout.EndScrollView();
                EditorGUILayout.BeginHorizontal();

                if(GUILayout.Button(resetSettingsContent, GUILayout.Width(120f)))
                    DeleteSavedValues();
                if(GUILayout.Button(unlockAllContent, GUILayout.Width(120f)))
                    Utility.UnlockAllObjects();

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Separator();

                Styles.ReloadTooltips();
                EditorApplication.RepaintHierarchyWindow();
            }

        }

    }
}