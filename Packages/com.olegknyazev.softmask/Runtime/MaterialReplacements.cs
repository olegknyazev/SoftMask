using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace SoftMasking {
    class MaterialReplacements {
        readonly IMaterialReplacer _replacer;
        readonly Action<Material> _applyParameters;

        readonly List<MaterialOverride> _overrides = new List<MaterialOverride>();

        public MaterialReplacements(IMaterialReplacer replacer, Action<Material> applyParameters) {
            _replacer = replacer;
            _applyParameters = applyParameters;
        }

        public Material Get(Material original) {
            for (int i = 0; i < _overrides.Count; ++i) {
                var entry = _overrides[i];
                if (ReferenceEquals(entry.original, original)) {
                    var existing = entry.Get();
                    if (existing) { // null may be stored in _overrides
                        existing.CopyPropertiesFromMaterial(original);
                        _applyParameters(existing);
                    }
                    return existing;
                }   
            }
            var replacement = _replacer.Replace(original);
            if (replacement) {
                Assert.AreNotEqual(original, replacement, "IMaterialReplacer should not return the original material");
                replacement.hideFlags = HideFlags.HideAndDontSave;
                _applyParameters(replacement);
            }
            _overrides.Add(new MaterialOverride(original, replacement));
            return replacement;
        }

        public void Release(Material replacement) {
            for (int i = 0; i < _overrides.Count; ++i) {
                var entry = _overrides[i];
                if (entry.replacement == replacement)
                    if (entry.Release()) {
                        UnityEngine.Object.DestroyImmediate(replacement);
                        _overrides.RemoveAt(i);
                        return;
                    }
            }
        }

        public void ApplyAll() {
            for (int i = 0; i < _overrides.Count; ++i) {
                var mat = _overrides[i].replacement;
                if (mat)
                    _applyParameters(mat);
            }
        }

        public void DestroyAllAndClear() {
            for (int i = 0; i < _overrides.Count; ++i)
                UnityEngine.Object.DestroyImmediate(_overrides[i].replacement);
            _overrides.Clear();
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
