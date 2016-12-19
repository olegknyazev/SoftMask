using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoftMask {
    [ExecuteInEditMode]
    [AddComponentMenu("")]
    public class SoftMaskable : UIBehaviour, IMaterialModifier {
        SoftMask _mask;
        Graphic _graphic;
        bool _warned;
        Material _material;

        public Material GetModifiedMaterial(Material baseMaterial) {
            if (_mask) {
                // We should find replacemnt first and only then Release a previous one.
                // It allows to not delete the old material if it may be reused.
                var replacement = _mask.GetReplacement(baseMaterial);
                material = replacement;
                if (material) {
                    _warned = false;
                    return material;
                }   
                WarnMaskingWillNotWork(baseMaterial);
            }
            return baseMaterial;
        }

        protected override void Awake() {
            base.Awake();
            hideFlags = HideFlags.HideAndDontSave;
            mask = FindMask();
        }
        
        protected override void OnDestroy() {
            base.OnDestroy();
            mask = null;
        }

        protected override void OnTransformParentChanged() {
            base.OnTransformParentChanged();
            mask = FindMask();
            if (!mask)
                DestroyImmediate(this);
        }

        Graphic graphic { get { return _graphic ?? (_graphic = GetComponent<Graphic>()); } }

        Material material {
            get { return _material; }
            set {
                if (_material != value) {
                    if (_material && _mask)
                        _mask.ReleaseReplacement(_material);
                    _material = value;
                }
            }
        }

        SoftMask mask {
            get { return _mask; }
            set {
                if (_mask != value) {
                    if (_mask)
                        material = null;
                    _mask = value;
                }
            }
        }

        SoftMask FindMask() { return GetComponentInParent<SoftMask>(); }

        void WarnMaskingWillNotWork(Material material) {
            if (!_warned) {
                Debug.LogWarningFormat(
                    "Soft Mask will not work on {0} because material {1} doesn't support masking. " +
                    "Add masking support to your material or set Graphic's material to None to use " +
                    "a default one.",
                    graphic,
                    material);
                _warned = true;
            }
        }
    }
}