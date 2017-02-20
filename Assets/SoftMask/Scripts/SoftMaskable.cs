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
        bool _destroyed;

        public bool shaderIsNotSupported { get; private set; }
        public bool isMaskingEnabled { get { return mask != null && mask.isAlive && mask.isMaskingEnabled; } }

        public Material GetModifiedMaterial(Material baseMaterial) {
            if (isMaskingEnabled) {
                // First get a new material, then release the old one. It allows us to reuse 
                // the old material if it's still actual.
                var newMat = mask.GetReplacement(baseMaterial);
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
        public void MaskMightChanged() {
            var prevMask = mask;
            var prevEnabled = isMaskingEnabled;
            if (FindMaskOrDie())
                if (!ReferenceEquals(prevMask, mask) || prevEnabled != isMaskingEnabled)
                    Invalidate();
        }

        protected override void Awake() {
            base.Awake();
            hideFlags = HideFlags.HideInInspector;
        }

        protected override void OnEnable() {
            base.OnEnable();
            if (FindMaskOrDie())
                NotifyChildrenChanged();
        }

        protected override void OnDisable() {
            base.OnDisable();
            mask = null; // To invalidate the Graphic and free the material
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            _destroyed = true;
        }

        protected override void OnTransformParentChanged() {
            base.OnTransformParentChanged();
            FindMaskOrDie();
        }

        void OnTransformChildrenChanged() {
            NotifyChildrenChanged();
        }

        void NotifyChildrenChanged() {
            if (mask != null)
                mask.UpdateTransformChildren(transform);
        }

        Graphic graphic { get { return _graphic ?? (_graphic = GetComponent<Graphic>()); } }

        Material replacement {
            get { return _replacement; }
            set {
                if (_replacement != value) {
                    if (_replacement != null && mask != null)
                        mask.ReleaseReplacement(_replacement);
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
                    _mask = (value != null && value.isAlive) ? value : null;
                    Invalidate();
                }
            }
        }

        bool FindMaskOrDie() {
            if (_destroyed)
                return false;
            mask = NearestMask(transform);
            if (mask == null)
                mask = NearestMask(transform, enabledOnly: false);
            if (mask == null) {
                _destroyed = true;
                DestroyImmediate(this);
                return false;
            }
            return true;
        }

        static ISoftMask NearestMask(Transform transform, bool enabledOnly = true) {
            if (!transform)
                return null;
            var mask = transform.GetComponent<ISoftMask>();
            if (mask != null && mask.isAlive && (!enabledOnly || mask.isMaskingEnabled))
                return mask;
            return NearestMask(transform.parent, enabledOnly);
        }

        void SetShaderNotSupported(Material material) {
            if (!shaderIsNotSupported) {
                Debug.LogWarningFormat(
                    gameObject,
                    "SoftMask will not work on {0} because material {1} doesn't support masking. " +
                    "Add masking support to your material or set Graphic's material to None to use " +
                    "a default one.",
                    graphic,
                    material);
                shaderIsNotSupported = true;
            }
        }
    }
}