using System;
using UnityEngine;

namespace EnhancedHierarchy.Icons {
    [Serializable]
    internal sealed class LeftNone : LeftSideIcon {
        public override float Width { get { return 0f; } }
        public override string Name { get { return "None"; } }
        public override void DoGUI(Rect rect) { }
    }

    [Serializable]
    internal sealed class RightNone : RightSideIcon {
        public override float Width { get { return 0f; } }
        public override string Name { get { return "None"; } }
        public override void DoGUI(Rect rect) { }
    }
}