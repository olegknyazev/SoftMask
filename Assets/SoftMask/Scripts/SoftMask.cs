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
        [SerializeField] MaskSource _maskSource = MaskSource.Graphic;
        [SerializeField] Sprite _maskSprite = null;
        [SerializeField] BorderMode _maskBorderMode = BorderMode.Simple;

        MaskParameters _maskParameters;
        bool _dirty;

        public enum MaskSource { Graphic, Sprite }
        public enum BorderMode { Simple, Sliced, Tiled }

        public Shader defaultMaskShader {
            get { return _defaultMaskShader; }
            set {
                if (_defaultMaskShader != value) {
                    _defaultMaskShader = value;
                    if (!_defaultMaskShader)
                        Debug.LogWarningFormat(this, "Mask may be not work because it's defaultMaskShader is set to null");
                    DestroyAllOverrides();
                    InvalidateChildren();
                }
            }
        }

        public MaskSource maskSource {
            get { return _maskSource; }
            set {
                if (_maskSource != value) {
                    _maskSource = value;
                    _dirty = true;
                }
            } 
        }

        public Sprite maskSprite {
            get { return _maskSprite; }
            set {
                if (_maskSprite != value) {
                    _maskSprite = value;
                    _dirty = true;
                }
            }
        }

        public bool maskingEnabled {
            get { return isActiveAndEnabled; }
        }

        // May return null.
        public Material GetReplacement(Material original) {
            Assert.IsTrue(isActiveAndEnabled);
            for (int i = 0; i < _overrides.Count; ++i) {
                var entry = _overrides[i];
                if (ReferenceEquals(entry.original, original))
                    return entry.Get();
            }
            var replacement = Replace(original);
            if (replacement) {
                replacement.hideFlags = HideFlags.HideAndDontSave;
                _maskParameters.Apply(replacement);
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
                        _overrides.RemoveAt(i);
                        return;
                    }
            }
        }

        protected virtual void LateUpdate() {
            SpawnMaskablesInChildren();
            if (!_graphic) {
                _graphic = GetComponent<Graphic>();
                if (_graphic) {
                    _graphic.RegisterDirtyVerticesCallback(OnGraphicDirty);
                    _graphic.RegisterDirtyMaterialCallback(OnGraphicDirty);
                }
            }

            if (transform.hasChanged || _dirty)
                UpdateMask();
        }

        protected override void OnEnable() {
            base.OnEnable();
            UpdateMask();
            InvalidateChildren();
        }

        protected override void OnDisable() {
            base.OnDisable();
            if (_graphic) {
                _graphic.UnregisterDirtyVerticesCallback(OnGraphicDirty);
                _graphic.UnregisterDirtyMaterialCallback(OnGraphicDirty);
                _graphic = null;
            }
            InvalidateChildren();
            DestroyAllOverrides();
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            DestroyMaskablesInChildren();
        }

        protected override void OnRectTransformDimensionsChange() {
            base.OnRectTransformDimensionsChange();
            _dirty = true;
        }

        protected override void OnDidApplyAnimationProperties() {
            base.OnDidApplyAnimationProperties();
            _dirty = true;
        }

        protected override void OnValidate() {
            base.OnValidate();
            _dirty = true;
        }
        
        RectTransform _rectTransform;
        RectTransform rectTransform { get { return _rectTransform ?? (_rectTransform = GetComponent<RectTransform>()); } }

        Graphic _graphic;
        Graphic graphic { get { return _graphic ?? (_graphic = GetComponent<Graphic>()); } }

        Canvas _canvas;
        Canvas canvas { get { return _canvas ?? (_canvas = _graphic ? _graphic.canvas : null); } } // TODO implement directly!
        
        bool isBasedOnGraphic { get { return _maskSource == MaskSource.Graphic; } }

        void OnGraphicDirty() {
            if (isBasedOnGraphic)
                _dirty = true;
        }

        void UpdateMask() {
            CalculateMaskParameters();
            ApplyToAllReplacements();
            transform.hasChanged = false;
            _dirty = false;
        }

        void SpawnMaskablesInChildren() {
            foreach (var g in transform.GetComponentsInChildren<Graphic>())
                if (g.gameObject != gameObject)
                    if (!g.GetComponent<SoftMaskable>())
                        g.gameObject.AddComponent<SoftMaskable>();
        }

        void DestroyMaskablesInChildren() {
            foreach (var m in transform.GetComponentsInChildren<SoftMaskable>())
                DestroyImmediate(m);
        }

        void InvalidateChildren() {
            foreach (var maskable in transform.GetComponentsInChildren<SoftMaskable>()) {
                if (maskable)
                    maskable.Invalidate();
            }
        }
        
        void DestroyAllOverrides() {
            for (int i = 0; i < _overrides.Count; ++i)
                DestroyImmediate(_overrides[i].replacement);
            _overrides.Clear();
        }

        Material Replace(Material original) {
            if (original == null || original.shader == Canvas.GetDefaultCanvasMaterial().shader) {
                var replacement = _defaultMaskShader ? new Material(_defaultMaskShader) : null;
                replacement.CopyPropertiesFromMaterial(original);
                return replacement;
            } else if (original.shader == Canvas.GetDefaultCanvasTextMaterial().shader)
                throw new NotSupportedException();
            else if (original.HasProperty("_SoftMask"))
                return new Material(original);
            else
                return null;
        }

        void CalculateMaskParameters() {
            switch (_maskSource) {
                case MaskSource.Graphic:
                    if (graphic is Image)
                        CalculateImageBased((Image)graphic);
                    else
                        CalculateEmpty();
                    break;
                case MaskSource.Sprite:
                    CalculateSpriteBased(_maskSprite, _maskBorderMode);
                    break;
                default:
                    CalculateEmpty();
                    break;
            }
        }

        BorderMode ToBorderMode(Image.Type imageType) {
            switch (imageType) {
                case Image.Type.Simple: return BorderMode.Simple;
                case Image.Type.Sliced: return BorderMode.Sliced;
                case Image.Type.Tiled: return BorderMode.Tiled;
                default:
                    Debug.LogWarningFormat("FUCK THE HELL!!!"); // TODO
                    return BorderMode.Simple;
            }
        }

        void CalculateImageBased(Image image) {
            CalculateSpriteBased(image.sprite, ToBorderMode(image.type));
        }
        
        void CalculateSpriteBased(Sprite sprite, BorderMode spriteMode) {
            var textureRect = ToVector(sprite.textureRect);
            var textureSize = new Vector2(sprite.texture.width, sprite.texture.height);
            _maskParameters.maskRect = CanvasSpaceRect(Vector4.zero);
            _maskParameters.maskBorder = CanvasSpaceRect(sprite.border * GraphicToCanvas(sprite));
            _maskParameters.maskRectUV = Div(textureRect, textureSize);
            _maskParameters.maskBorderUV = Div(Inset(ToVector(sprite.rect), sprite.border), textureSize);
            _maskParameters.texture = sprite.texture;
            _maskParameters.textureMode = spriteMode;
        }

        void CalculateEmpty() {
            _maskParameters.texture = null;
        }

        float GraphicToCanvas(Sprite sprite) {
            var canvasPPU = canvas ? canvas.referencePixelsPerUnit : 100;
            var maskPPU = sprite ? sprite.pixelsPerUnit : 100;
            return canvasPPU / maskPPU;
        }

        void ApplyToAllReplacements() {
            for (int i = 0; i < _overrides.Count; ++i) {
                var mat = _overrides[i].replacement;
                if (mat)
                    _maskParameters.Apply(mat);
            }
        }

        static Vector3[] _corners = new Vector3[4];

        Vector4 CanvasSpaceRect(Vector4 border) { return CanvasSpaceRect(rectTransform, border); }
        Vector4 CanvasSpaceRect(RectTransform transform, Vector4 border) {
            transform.GetLocalCorners(_corners);
            _corners[0] = GetComponentInParent<Canvas>().transform.InverseTransformPoint(transform.TransformPoint(_corners[0] + new Vector3(border.x, border.y)));
            _corners[2] = GetComponentInParent<Canvas>().transform.InverseTransformPoint(transform.TransformPoint(_corners[2] - new Vector3(border.z, border.w)));
            return new Vector4(_corners[0].x, _corners[0].y, _corners[2].x, _corners[2].y);
        }

        static Vector4 ToVector(Rect r) { return new Vector4(r.xMin, r.yMin, r.xMax, r.yMax); }
        static Vector4 Div(Vector4 v, Vector2 s) { return new Vector4(v.x / s.x, v.y / s.y, v.z / s.x, v.w / s.y); }
        static Vector4 Inset(Vector4 v, Vector4 b) { return new Vector4(v.x + b.x, v.y + b.y, v.z - b.z, v.w - b.w); }

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
        
        struct MaskParameters {
            public Vector4 maskRect;
            public Vector4 maskBorder;
            public Vector4 maskRectUV;
            public Vector4 maskBorderUV;
            public Texture texture;
            public BorderMode textureMode;

            public void Apply(Material mat) {
                if (texture) {
                    mat.SetTexture("_SoftMask", texture);
                    mat.SetVector("_SoftMask_Rect", maskRect);
                    mat.SetVector("_SoftMask_UVRect", maskRectUV);
                    if (textureMode == BorderMode.Sliced) {
                        mat.EnableKeyword("SOFTMASK_USE_BORDER");
                        mat.SetVector("_SoftMask_BorderRect", maskBorder);
                        mat.SetVector("_SoftMask_UVBorderRect", maskBorderUV);
                    } else {
                        mat.DisableKeyword("SOFTMASK_USE_BORDER");
                    }
                } else {
                    mat.SetTexture("_SoftMask", null);
                }
            }
        }
    }
}
