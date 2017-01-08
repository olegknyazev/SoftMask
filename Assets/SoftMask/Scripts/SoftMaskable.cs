using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SoftMasking.Extensions;

namespace SoftMasking {
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public class SoftMaskable : UIBehaviour, IMaterialModifier {
        ISoftMask _mask;
        Graphic _graphic;
        Material _replacement;

        public bool shaderIsNotSupported { get; private set; }

        public Material GetModifiedMaterial(Material baseMaterial) {
            if (_mask != null && _mask.isMaskingEnabled) {
                // We should find new replacement first and only then Release() the previous 
                // one. It allows us to not delete the old material if it may be reused.
                var newMat = _mask.GetReplacement(baseMaterial);
                replacement = newMat;
                if (replacement) {
                    shaderIsNotSupported = false;
                    return replacement;
                }
                // Warn only if material has non-default UI shader. Otherwise, it seems that
                // replacement is null because SoftMask.defaultShader isn't set. If so, it's
                // SoftMask's business.
                if (!baseMaterial.HasDefaultUIShader())
                    SetShaderNotSupported(baseMaterial);
            } else {
                shaderIsNotSupported = false;
                replacement = null;
            }   
            return baseMaterial;
        }
        
        // Called when replacement material might changed, so, material should be reevaluated.
        public void Invalidate() {
            if (graphic)
                graphic.SetMaterialDirty();
        }

        // Called when active mask might changed, so, mask should be searched again.
        public void MaskMightChange() {
            FindMask();
        }

        protected override void Awake() {
            base.Awake();
            hideFlags = HideFlags.HideInInspector;
        }

        protected virtual void Update() {
            if (mask == null || graphic == null)
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
                    if (_replacement != null && _mask != null)
                        _mask.ReleaseReplacement(_replacement);
                    _replacement = value;
                }
            }
        }

        ISoftMask mask {
            get { return _mask; }
            set {
                if (_mask != value) {
                    if (_mask != null)
                        replacement = null;
                    _mask = value;
                    Invalidate();
                }
            }
        }

        void FindMask() { mask = NearestEnabledMask(transform); }

        SoftMask NearestEnabledMask(Transform root) {
            if (!root)
                return null;
            var mask = root.GetComponent<SoftMask>();
            if (mask && mask.isMaskingEnabled)
                return mask;
            return NearestEnabledMask(root.parent);
        }

        void SetShaderNotSupported(Material material) {
            if (!shaderIsNotSupported) {
                Debug.LogWarningFormat(
                    gameObject,
                    "Soft Mask will not work on {0} because material {1} doesn't support masking. " +
                    "Add masking support to your material or set Graphic's material to None to use " +
                    "a default one.",
                    graphic,
                    material);
                shaderIsNotSupported = true;
            }
        }
    }
}