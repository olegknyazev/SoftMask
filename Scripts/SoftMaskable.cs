using System.Collections.Generic;
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
        bool _affectedByMask;
        bool _destroyed;

        public bool shaderIsNotSupported { get; private set; }

        public bool isMaskingEnabled {
            get {
                return mask != null 
                    && mask.isAlive 
                    && mask.isMaskingEnabled 
                    && _affectedByMask;
            }
        }

        public ISoftMask mask {
            get { return _mask; }
            private set {
                if (_mask != value) {
                    if (_mask != null)
                        replacement = null;
                    _mask = (value != null && value.isAlive) ? value : null;
                    Invalidate();
                }
            }
        }

        public Material GetModifiedMaterial(Material baseMaterial) {
            if (isMaskingEnabled) {
                // First, get a new material then release the old one. It allows us to reuse 
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
        
        // Called when replacement material might be changed, so, the material should be refreshed.
        public void Invalidate() {
            if (graphic)
                graphic.SetMaterialDirty();
        }

        // Called when an active mask might be changed, so, the mask should be searched again.
        public void MaskMightChanged() {
            if (FindMaskOrDie())
                Invalidate();
        }

        protected override void Awake() {
            base.Awake();
            hideFlags = HideFlags.HideInInspector;
        }

        protected override void OnEnable() {
            base.OnEnable();
            if (FindMaskOrDie())
                RequestChildTransformUpdate();
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

        protected override void OnCanvasHierarchyChanged() {
            base.OnCanvasHierarchyChanged();
            // A change of override sorting might change the mask instance that's masking us
            FindMaskOrDie();
        }

        void OnTransformChildrenChanged() {
            RequestChildTransformUpdate();
        }

        void RequestChildTransformUpdate() {
            if (mask != null)
                mask.UpdateTransformChildren(transform);
        }

        Graphic graphic { get { return _graphic ? _graphic : (_graphic = GetComponent<Graphic>()); } }

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

        bool FindMaskOrDie() {
            if (_destroyed)
                return false;
            mask = NearestMask(transform, out _affectedByMask)
                ?? NearestMask(transform, out _affectedByMask, enabledOnly: false);
            if (mask == null) {
                _destroyed = true;
                DestroyImmediate(this);
                return false;
            }
            return true;
        }

        static ISoftMask NearestMask(Transform transform, out bool processedByThisMask, bool enabledOnly = true) {
            processedByThisMask = true;
            var current = transform;
            while (true) {
                if (!current)
                    return null;
                if (current != transform) { // Masks do not mask themselves
                    var mask = GetISoftMask(current, shouldBeEnabled: enabledOnly);
                    if (mask != null)
                        return mask;
                }
                if (IsOverridingSortingCanvas(current))
                    processedByThisMask = false;
                current = current.parent;
            }
        }

        static List<ISoftMask> s_softMasks = new List<ISoftMask>();
        static List<Canvas> s_canvases = new List<Canvas>();

        static ISoftMask GetISoftMask(Transform current, bool shouldBeEnabled = true) {
            var mask = GetComponent(current, s_softMasks);
            if (mask != null && mask.isAlive && (!shouldBeEnabled || mask.isMaskingEnabled))
                return mask;
            return null;
        }

        static bool IsOverridingSortingCanvas(Transform transform) {
            var canvas = GetComponent(transform, s_canvases);
            if (canvas && canvas.overrideSorting)
                return true;
            return false;
        }
        
        static T GetComponent<T>(Component component, List<T> cachedList) where T : class {
            component.GetComponents(cachedList);
            using (new ClearListAtExit<T>(cachedList))
                return cachedList.Count > 0 ? cachedList[0] : null;
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