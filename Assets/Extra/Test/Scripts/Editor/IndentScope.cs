using System;
using UnityEditor;

namespace SoftMasking.Editor {
    class IndentScope : IDisposable {
        public IndentScope() {
            ++EditorGUI.indentLevel;
        }
        public void Dispose() {
            --EditorGUI.indentLevel;
        }
    }
}
