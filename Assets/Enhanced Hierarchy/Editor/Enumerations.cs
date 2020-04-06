using System;

namespace EnhancedHierarchy {

    [Flags]
    internal enum EntryMode {
        ScriptingError = 256,
        ScriptingWarning = 512,
        ScriptingLog = 1024
    }

    internal enum MiniLabelType {
        None = 0,
        Tag = 1,
        Layer = 2,
        TagOrLayer = 3,
        LayerOrTag = 4,
        TagAndLayer = 5,
        LayerAndTag = 6
    }

    internal enum ChildrenChangeMode {
        ObjectAndChildren = 0,
        ObjectOnly = 1,
        Ask = 2,
    }

}