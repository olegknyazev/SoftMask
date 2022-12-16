using UnityEngine;

namespace SoftMasking {
    public interface ISoftMask {
        bool isAlive { get; }
        bool isMaskingEnabled { get; }
        // May return null.
        Material GetReplacement(Material original);
        void ReleaseReplacement(Material replacement);
        void UpdateTransformChildren(Transform transform);
    }
}
