using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoftMask {
    [ExecuteInEditMode]
    [AddComponentMenu("UI/Soft Mask", 14)]
    public class SoftMask : UIBehaviour {
        readonly List<MaterialOverride> _overrides = new List<MaterialOverride>();
        [SerializeField] Shader _defaultMaskShader;
        Shader _defaultMaskShader_actual;

        protected virtual void Update() {
            SpawnMaskablesInChildren();
            if (_defaultMaskShader_actual != _defaultMaskShader) {
                if (!_defaultMaskShader)
                    Debug.LogWarningFormat(this, "Mask may be not work because it's defaultMaskShader is set to null");
                DestroyAllOverrides();
                InvalidateChildren();
                _defaultMaskShader_actual = _defaultMaskShader;
            }
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            DestroyAllOverrides();
        }

        void SpawnMaskablesInChildren() {
            foreach (var g in transform.GetComponentsInChildren<Graphic>())
                if (!g.GetComponent<SoftMaskable>())
                    g.gameObject.AddComponent<SoftMaskable>();
        }

        void InvalidateChildren() {
            foreach (var g in transform.GetComponentsInChildren<Graphic>()) {
                var maskable = g.GetComponent<SoftMaskable>();
                if (maskable)
                    maskable.Invalidate();
            }
        }

        // May return null.
        public Material GetReplacement(Material original) {
            for (int i = 0; i < _overrides.Count; ++i) {
                var entry = _overrides[i];
                if (entry.original == original)
                    return entry.Get();
            }
            var replacement = Replace(original);
            if (replacement)
                replacement.hideFlags = HideFlags.HideAndDontSave;
            _overrides.Add(new MaterialOverride(original, replacement));
            return replacement;
        }

        public void ReleaseReplacement(Material replacement) {
            for (int i = 0; i < _overrides.Count; ++i) {
                var entry = _overrides[i];
                if (entry.replacement == replacement)
                    if (entry.Release()) {
                        DestroyImmediate(replacement);
                        return;
                    }   
            }
        }

        void DestroyAllOverrides() {
            for (int i = 0; i < _overrides.Count; ++i)
                DestroyImmediate(_overrides[i].replacement);
            _overrides.Clear();
        }

        Material Replace(Material original) {
            if (original == null || original == Canvas.GetDefaultCanvasMaterial())
                return _defaultMaskShader ? new Material(_defaultMaskShader) : null;
            else if (original == Canvas.GetDefaultCanvasTextMaterial())
                throw new NotSupportedException();
            else if (original.HasProperty("_SoftMask"))
                return new Material(original);
            else
                return null;
        }

        class MaterialOverride {
            int _useCount;

            public MaterialOverride(Material original, Material replacement) {
                this.original = original;
                this.replacement = replacement;
                _useCount = 1;
            }

            public Material original { get; private set; }
            public Material replacement { get; private set; }

            public Material Get() {
                ++_useCount;
                return replacement;
            }

            public bool Release() {
                Assert.IsTrue(_useCount > 0);
                return --_useCount == 0;
            }
        }
    }
}
