﻿using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SoftMask.Extensions;

namespace SoftMask {
    public static class MaskChannel {
        public static Color alpha   = new Color(0, 0, 0, 1);
        public static Color red     = new Color(1, 0, 0, 0);
        public static Color green   = new Color(0, 1, 0, 0);
        public static Color blue    = new Color(0, 0, 1, 0);
        public static Color gray    = new Color(1, 1, 1, 0) / 3.0f;
    }

    [ExecuteInEditMode]
    [AddComponentMenu("UI/Soft Mask", 14)]
    [RequireComponent(typeof(RectTransform))]
    public class SoftMask : UIBehaviour {
        //
        // How it works:
        //
        // Soft Mask works by using special Shader when rendering child elements. That Shader
        // samples Mask texture and multiplies the resulted color accordingly. To override
        // Shader in child elements, SoftMask spawns invisible SoftMaskable components on them,
        // on the fly. SoftMaskable is kept on the children elements while there is a SoftMask
        // parent. When the parent is gone, SoftMaskable components are destroyed. When a child
        // element is moved to another place in the hierarchy where is no SoftMask parent 
        // present, it's SoftMaskable destroys itself.
        //

        [SerializeField] Shader _defaultMaskShader = null;
        [SerializeField] MaskSource _maskSource = MaskSource.Graphic;
        [SerializeField] Sprite _maskSprite = null;
        [SerializeField] BorderMode _maskBorderMode = BorderMode.Simple;
        [SerializeField] Texture2D _maskTexture = null;
        [SerializeField] Color _maskChannelWeights = MaskChannel.alpha;

        MaterialReplacements _materials;
        MaskParameters _maskParameters;
        bool _dirty;

        public SoftMask() {
            _materials = new MaterialReplacements(Replace, m => _maskParameters.Apply(m));
        }

        [Serializable] public enum MaskSource { Graphic, Sprite, Texture }
        [Serializable] public enum BorderMode { Simple, Sliced, Tiled }

        public Shader defaultMaskShader {
            get { return _defaultMaskShader; }
            set {
                if (_defaultMaskShader != value) {
                    _defaultMaskShader = value;
                    if (!_defaultMaskShader)
                        Debug.LogWarningFormat(this, "Mask may be not work because it's defaultMaskShader is set to null");
                    DestroyMaterials();
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

        public Texture2D maskTexture {
            get { return _maskTexture; }
            set {
                if (_maskTexture != value) {
                    _maskTexture = value;
                    _dirty = true;
                }
            }
        }

        public Color maskChannelWeights {
            get { return _maskChannelWeights; }
            set {
                if (_maskChannelWeights != value) {
                    _maskChannelWeights = value;
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
            return _materials.Get(original);
        }

        public void ReleaseReplacement(Material replacement) {
            _materials.Release(replacement);
        }

        protected virtual void LateUpdate() {
            SpawnMaskablesInChildren();
            FindGraphic();
            if (transform.hasChanged || _dirty)
                UpdateMask();
        }

        protected override void OnEnable() {
            base.OnEnable();
            FindGraphic();
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
            DestroyMaterials();
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

        Canvas _canvas;
        Canvas canvas { get { return _canvas ?? (_canvas = _graphic ? _graphic.canvas : null); } } // TODO implement directly!
        
        bool isBasedOnGraphic { get { return _maskSource == MaskSource.Graphic; } }
        
        void OnGraphicDirty() {
            if (isBasedOnGraphic)
                _dirty = true;
        }

        void FindGraphic() {
            if (!_graphic) {
                _graphic = GetComponent<Graphic>();
                if (_graphic) {
                    _graphic.RegisterDirtyVerticesCallback(OnGraphicDirty);
                    _graphic.RegisterDirtyMaterialCallback(OnGraphicDirty);
                }
            }
        }

        void UpdateMask() {
            CalculateMaskParameters();
            _materials.ApplyAll();
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
            foreach (var maskable in transform.GetComponentsInChildren<SoftMaskable>())
                if (maskable)
                    maskable.Invalidate();
        }
        
        void DestroyMaterials() {
            _materials.DestroyAllAndClear();
        }

        Material Replace(Material original) {
            if (original == null || original.HasDefaultUIShader()) {
                var replacement = _defaultMaskShader ? new Material(_defaultMaskShader) : null;
                if (replacement && original)
                    replacement.CopyPropertiesFromMaterial(original);
                return replacement;
            } else if (original.SupportsSoftMask())
                return new Material(original);
            else
                return null;
        }

        void CalculateMaskParameters() {
            switch (_maskSource) {
                case MaskSource.Graphic:
                    if (_graphic is Image)
                        CalculateImageBased((Image)_graphic);
                    else if (_graphic is RawImage)
                        CalculateRawImageBased((RawImage)_graphic);
                    else
                        CalculateSolidFill();
                    break;
                case MaskSource.Sprite:
                    CalculateSpriteBased(_maskSprite, _maskBorderMode);
                    break;
                case MaskSource.Texture:
                    CalculateTextureBased(_maskTexture);
                    break;
                default:
                    Debug.LogWarningFormat("Unknown MaskSource: {0}", _maskSource);
                    CalculateSolidFill();
                    break;
            }
        }

        BorderMode ToBorderMode(Image.Type imageType) {
            switch (imageType) {
                case Image.Type.Simple: return BorderMode.Simple;
                case Image.Type.Sliced: return BorderMode.Sliced;
                case Image.Type.Tiled: return BorderMode.Tiled;
                default:
                    Debug.LogWarningFormat(
                        "Image.Type {0} isn't supported by SoftMask. Image.Type.Simple will be used.",
                        imageType);
                    return BorderMode.Simple;
            }
        }

        void CalculateImageBased(Image image) {
            if (image.sprite)
                CalculateSpriteBased(image.sprite, ToBorderMode(image.type));
            else
                CalculateSolidFill();
        }

        void CalculateRawImageBased(RawImage image) {
            CalculateTextureBased(image.texture as Texture2D);
        }

        void CalculateSpriteBased(Sprite sprite, BorderMode spriteMode) {
            CalculateCommonParameters();
            var textureRectInFullRect = Div(BorderOf(sprite.rect, sprite.textureRect), sprite.rect.size);
            var textureRect = ToVector(sprite.textureRect);
            var textureSize = new Vector2(sprite.texture.width, sprite.texture.height);
            var fullMaskRect = LocalRect(Vector4.zero);
            _maskParameters.maskRect = ApplyBorder(fullMaskRect, Mul(textureRectInFullRect, Size(fullMaskRect)));
            _maskParameters.maskBorder = LocalRect(sprite.border * GraphicToCanvas(sprite));
            _maskParameters.maskRectUV = Div(textureRect, textureSize);
            _maskParameters.maskBorderUV = ApplyBorder(_maskParameters.maskRectUV, Div(sprite.border, textureSize));
            _maskParameters.texture = sprite.texture;
            _maskParameters.textureMode = spriteMode;
            if (spriteMode == BorderMode.Tiled)
                _maskParameters.tileRepeat = MaskRepeat(sprite, _maskParameters.maskBorder);
        }

        static readonly Vector4 DefaultRectUV = new Vector4(0, 0, 1, 1);

        void CalculateTextureBased(Texture2D texture) {
            CalculateCommonParameters();
            _maskParameters.maskRect = LocalRect(Vector4.zero);
            _maskParameters.maskRectUV = DefaultRectUV;
            _maskParameters.texture = texture;
            _maskParameters.textureMode = BorderMode.Simple;
        }

        void CalculateSolidFill() {
            CalculateTextureBased(null);
        }

        void CalculateCommonParameters() {
            _maskParameters.worldToMask = WorldToMask();
            _maskParameters.maskChannelWeights = _maskChannelWeights;
        }

        float GraphicToCanvas(Sprite sprite) {
            var canvasPPU = canvas ? canvas.referencePixelsPerUnit : 100;
            var maskPPU = sprite ? sprite.pixelsPerUnit : 100;
            return canvasPPU / maskPPU;
        }

        Matrix4x4 WorldToMask() {
            return transform.worldToLocalMatrix * canvas.transform.localToWorldMatrix;
        }

        Vector4 LocalRect(Vector4 border) {
            return ApplyBorder(ToVector(rectTransform.rect), border);
        }

        Vector2 MaskRepeat(Sprite sprite, Vector4 centralPart) {
            var textureCenter = ApplyBorder(ToVector(sprite.textureRect), sprite.border);
            return Div(Size(centralPart) * GraphicToCanvas(sprite), Size(textureCenter));
        }

        static Vector4 ToVector(Rect r) { return new Vector4(r.xMin, r.yMin, r.xMax, r.yMax); }
        static Vector4 Div(Vector4 v, Vector2 s) { return new Vector4(v.x / s.x, v.y / s.y, v.z / s.x, v.w / s.y); }
        static Vector2 Div(Vector2 v, Vector2 s) { return new Vector2(v.x / s.x, v.y / s.y); }
        static Vector4 Mul(Vector4 v, Vector2 s) { return new Vector4(v.x * s.x, v.y * s.y, v.z * s.x, v.w * s.y); }
        static Vector2 Size(Vector4 r) { return new Vector2(r.z - r.x, r.w - r.y); }

        static Vector4 BorderOf(Rect outer, Rect inner) {
            return new Vector4(inner.xMin - outer.xMin, inner.yMin - outer.yMin, outer.xMax - inner.xMax, outer.yMax - inner.yMax);
        }

        static Vector4 ApplyBorder(Vector4 v, Vector4 b) {
            return new Vector4(v.x + b.x, v.y + b.y, v.z - b.z, v.w - b.w);
        }

        struct MaskParameters {
            public Vector4 maskRect;
            public Vector4 maskBorder;
            public Vector4 maskRectUV;
            public Vector4 maskBorderUV;
            public Vector2 tileRepeat;
            public Color maskChannelWeights;
            public Matrix4x4 worldToMask;
            public Texture texture;
            public BorderMode textureMode;

            public Texture activeTexture { get { return texture ? texture : Texture2D.whiteTexture; } }

            public void Apply(Material mat) {
                mat.SetTexture("_SoftMask", activeTexture);
                mat.SetVector("_SoftMask_Rect", maskRect);
                mat.SetVector("_SoftMask_UVRect", maskRectUV);
                mat.SetColor("_SoftMask_ChannelWeights", maskChannelWeights);
                mat.SetMatrix("_SoftMask_WorldToMask", worldToMask);
                mat.EnableKeyword("SOFTMASK_SLICED", textureMode == BorderMode.Sliced);
                mat.EnableKeyword("SOFTMASK_TILED", textureMode == BorderMode.Tiled);
                if (textureMode != BorderMode.Simple) {
                    mat.SetVector("_SoftMask_BorderRect", maskBorder);
                    mat.SetVector("_SoftMask_UVBorderRect", maskBorderUV);
                    if (textureMode == BorderMode.Tiled)
                        mat.SetVector("_SoftMask_TileRepeat", tileRepeat);
                }
            }
        }
    }
}
