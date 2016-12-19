using UnityEngine;
using UnityEngine.EventSystems;

namespace SoftMask {
    [ExecuteInEditMode]
    [AddComponentMenu("")]
    public class SoftMaskable : UIBehaviour {
        SoftMask _mask;

        protected override void Awake() {
            base.Awake();
            hideFlags = HideFlags.HideAndDontSave;
            FindMask();
            print("SoftMaskable.Awake " + GetInstanceID());
        }
        
        protected override void OnDestroy() {
            base.OnDestroy();
            print("SoftMaskable.OnDestroy " + GetInstanceID());
        }

        protected override void OnTransformParentChanged() {
            base.OnTransformParentChanged();
            FindMask();
            if (!_mask)
                DestroyImmediate(this);
        }

        void FindMask() {
            _mask = GetComponentInParent<SoftMask>();
        }
    }
}