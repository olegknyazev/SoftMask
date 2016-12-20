using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoftMask {
    [ExecuteInEditMode]
    [AddComponentMenu("UI/Soft Mask", 14)]
    [RequireComponent(typeof(RectTransform))]
    public class SoftMask : UIBehaviour {
        readonly List<MaterialOverride> _overrides = new List<MaterialOverride>();

        [SerializeField] Shader _defaultMaskShader = null;
        Shader _defaultMaskShader_actual;

        RectTransform _rectTransform;
        Sprite _mask = null;
        Sprite _mask_actual;
        Vector3 _position_actual;
        Quaternion _rotation_actual;
        Vector3 _scale_actual;
        bool _rectTransformDimensionsChanged;

        Vector4 _maskRect;
        Vector4 _maskBorder;
        Vector4 _maskRectUV;
        Vector4 _maskBorderUV;

        Graphic _graphic;
        Image _image;

        protected virtual void Update() {
            SpawnMaskablesInChildren();
            if (_defaultMaskShader_actual != _defaultMaskShader) {
                if (!_defaultMaskShader)
                    Debug.LogWarningFormat(this, "Mask may be not work because it's defaultMaskShader is set to null");
                DestroyAllOverrides();
                InvalidateChildren();
                _defaultMaskShader_actual = _defaultMaskShader;
            }
            if (image)
                _mask = image.overrideSprite ? image.overrideSprite : image.sprite;
            if (_mask_actual != _mask
                    || _position_actual != transform.position
                    || _rotation_actual != transform.rotation
                    || _scale_actual != transform.lossyScale
                    || _rectTransformDimensionsChanged) {
                CalculateMask();
                ApplyMaterialProperties();
                _mask_actual = _mask;
                _position_actual = transform.position;
                _rotation_actual = transform.rotation;
                _scale_actual = transform.lossyScale;
                _rectTransformDimensionsChanged = false;
            }
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            DestroyAllOverrides();
        }

        protected override void OnRectTransformDimensionsChange() {
            base.OnRectTransformDimensionsChange();
            _rectTransformDimensionsChanged = true;
        }

        RectTransform rectTransform { get { return _rectTransform ?? (_rectTransform = GetComponent<RectTransform>()); } }

        Image image { get { return _image ?? (_image = GetComponent<Image>()); } }

        Graphic graphic { get { return _graphic ?? (_graphic = GetComponent<Graphic>()); } }

        float graphicToCanvas {
            get {
                var canvasPPU = graphic ? graphic.canvas.referencePixelsPerUnit : 100;
                var maskPPU = (_mask ? _mask.pixelsPerUnit : 100);
                return canvasPPU / maskPPU;
            }
        }

        void SpawnMaskablesInChildren() {
            foreach (var g in transform.GetComponentsInChildren<Graphic>())
                if (g.gameObject != gameObject)
                    if (!g.GetComponent<SoftMaskable>())
                        g.gameObject.AddComponent<SoftMaskable>();
        }

        void InvalidateChildren() {
            foreach (var maskable in transform.GetComponentsInChildren<SoftMaskable>()) {
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
            if (replacement) {
                replacement.hideFlags = HideFlags.HideAndDontSave;
                ApplyMaterialProperties(replacement);
            }   
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

        void ApplyMaterialProperties() {
            for (int i = 0; i < _overrides.Count; ++i) {
                var mat = _overrides[i].replacement;
                if (mat)
                    ApplyMaterialProperties(mat);
            }
        }

        void ApplyMaterialProperties(Material mat) {
            if (_mask)
                mat.SetTexture("_SoftMask", _mask.texture);
            mat.SetVector("_SoftMask_Rect", _maskRect);
            mat.SetVector("_SoftMask_BorderRect", _maskBorder);
            mat.SetVector("_SoftMask_UVRect", _maskRectUV);
            mat.SetVector("_SoftMask_UVBorderRect", _maskBorderUV);
        }

        void CalculateMask() {
            if (!_mask)
                return;
            var textureRect = ToVector(_mask.textureRect);
            var textureSize = _mask.rect.size;
            _maskRect = CanvasSpaceRect(rectTransform, Vector4.zero);
            _maskBorder = CanvasSpaceRect(rectTransform, _mask.border * graphicToCanvas);
            _maskRectUV = Div(textureRect, textureSize);
            _maskBorderUV = Div(Inset(ToVector(_mask.rect), _mask.border), textureSize);
        }

        static Vector4 ToVector(Rect r) { return new Vector4(r.xMin, r.yMin, r.xMax, r.yMax); }
        static Vector4 Div(Vector4 v, Vector2 s) { return new Vector4(v.x / s.x, v.y / s.y, v.z / s.x, v.w / s.y); }
        static Vector4 Inset(Vector4 v, Vector4 b) { return new Vector4(v.x + b.x, v.y + b.y, v.z - b.z, v.w - b.w); }

        static Vector3[] _corners = new Vector3[4];

        Vector4 CanvasSpaceRect(RectTransform transform, Vector4 border) {
            transform.GetLocalCorners(_corners);
            _corners[0] = GetComponentInParent<Canvas>().transform.InverseTransformPoint(transform.TransformPoint(_corners[0] + new Vector3(border.x, border.y)));
            _corners[2] = GetComponentInParent<Canvas>().transform.InverseTransformPoint(transform.TransformPoint(_corners[2] - new Vector3(border.z, border.w)));
            return new Vector4(_corners[0].x, _corners[0].y, _corners[2].x, _corners[2].y);
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
