using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SoftMask.Extensions;

namespace SoftMask {
    [ExecuteInEditMode]
    [AddComponentMenu("")]
    [RequireComponent(typeof(Graphic))]
    public class SoftMaskable : UIBehaviour, IMaterialModifier {
        SoftMask _mask;
        Graphic _graphic;
        Material _replacement;
        bool _warned;

        public Material GetModifiedMaterial(Material baseMaterial) {
            if (_mask && _mask.isActiveAndEnabled) {
                // We should find new replacement first and only then Release() the previous 
                // one. It allows us to not delete the old material if it may be reused.
                var newMat = _mask.GetReplacement(baseMaterial);
                replacement = newMat;
                if (replacement) {
                    _warned = false;
                    return replacement;
                }
                // Warn only if material has non-default UI shader. Otherwise, it seems that
                // replacement is null because SoftMask.defaultShader isn't set. If so, it's
                // SoftMask's business.
                if (!baseMaterial.HasDefaultUIShader())
                    WarnMaskingWillNotWork(baseMaterial);
            } else {
                replacement = null;
            }   
            return baseMaterial;
        }
        
        public void Invalidate() {
            graphic.SetMaterialDirty();
        }

        protected override void Awake() {
            base.Awake();
            hideFlags = HideFlags.HideInInspector;
        }

        protected virtual void Update() {
            if (!mask)
                DestroyImmediate(this);
        }

        protected override void OnEnable() {
            base.OnEnable();
            FindMask();
        }

        protected override void OnDisable() {
            base.OnDisable();
            mask = null;
        }

        protected override void OnTransformParentChanged() {
            base.OnTransformParentChanged();
            FindMask();
        }

        Graphic graphic { get { return _graphic ?? (_graphic = GetComponent<Graphic>()); } }

        Material replacement {
            get { return _replacement; }
            set {
                if (_replacement != value) {
                    if (_replacement && _mask)
                        _mask.ReleaseReplacement(_replacement);
                    _replacement = value;
                }
            }
        }

        SoftMask mask {
            get { return _mask; }
            set {
                if (_mask != value) {
                    if (_mask)
                        replacement = null;
                    _mask = value;
                    Invalidate();
                }
            }
        }

        void FindMask() {
            mask = GetComponentInParent<SoftMask>();
            
        }

        void WarnMaskingWillNotWork(Material material) {
            if (!_warned) {
                Debug.LogWarningFormat(
                    gameObject,
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