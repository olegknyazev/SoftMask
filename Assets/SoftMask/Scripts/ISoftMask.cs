using UnityEngine;

namespace SoftMasking {
    public interface ISoftMask {
        bool isMaskingEnabled { get; }
        // May return null.
        Material GetReplacement(Material original);
        void ReleaseReplacement(Material replacement);
    }
}
