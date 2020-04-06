using UnityEditor;
using UnityEngine;

namespace SoftMasking.Editor {
    static class AutomatedTestStyles {
        public static readonly GUIStyle inProgress = MakeTestResultStyle(new Color(0.4f, 0.4f, 0f));
        public static readonly GUIStyle passed = MakeTestResultStyle(new Color(0f, 0.4f, 0f));
        public static readonly GUIStyle failed = MakeTestResultStyle(new Color(0.4f, 0f, 0f));
        public static readonly GUIStyle validationRect = MakeRectStyle();
        public static readonly GUIStyle errorMessage;

        static AutomatedTestStyles() {
            errorMessage = EditorStyles.label;
            errorMessage.wordWrap = true;
        }

        static GUIStyle MakeTestResultStyle(Color color) {
            var style = new GUIStyle();
            style.normal.textColor = color;
            style.fontStyle = FontStyle.Bold;
            return style;
        }

        static GUIStyle MakeRectStyle() {
            var style = new GUIStyle();
            style.border = new RectOffset(2, 2, 2, 2);
            style.normal.background = Resources.Load<Texture2D>("ValidationRectBorder");
            return style;
        }
    }
}
