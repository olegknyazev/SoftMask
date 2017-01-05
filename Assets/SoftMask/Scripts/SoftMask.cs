using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SoftMasking.Extensions;

namespace SoftMasking {
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
    public class SoftMask : UIBehaviour, ICanvasRaycastFilter, ISoftMask {
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

        [SerializeField] Shader _defaultShader = null;
        [SerializeField] MaskSource _source = MaskSource.Graphic;
        [SerializeField] Sprite _sprite = null;
        [SerializeField] BorderMode _spriteBorderMode = BorderMode.Simple;
        [SerializeField] Texture2D _texture = null;
        [SerializeField] Rect _textureRect = DefaultRectUV;
        [SerializeField] Color _channelWeights = MaskChannel.alpha;
        [SerializeField] float _raycastThreshold = 0.0f;

        MaterialReplacements _materials;
        MaterialParameters _parameters;
        bool _dirty;

        public SoftMask() {
            _materials = new MaterialReplacements(Replace, m => _parameters.Apply(m));
        }

        [Serializable] public enum MaskSource { Graphic, Sprite, Texture }
        [Serializable] public enum BorderMode { Simple, Sliced, Tiled }

        public Shader defaultShader {
            get { return _defaultShader; }
            set {
                if (_defaultShader != value) {
                    _defaultShader = value;
                    WarnIfDefaultShaderIsNotSet();
                    DestroyMaterials();
                    InvalidateChildren();
                }
            }
        }

        public MaskSource source {
            get { return _source; }
            set {
                if (_source != value) {
                    _source = value;
                    _dirty = true;
                }
            } 
        }

        public Sprite sprite {
            get { return _sprite; }
            set {
                if (_sprite != value) {
                    _sprite = value;
                    _dirty = true;
                }
            }
        }

        public Texture2D texture {
            get { return _texture; }
            set {
                if (_texture != value) {
                    _texture = value;
                    _dirty = true;
                }
            }
        }

        public Rect textureUVRect {
            get { return _textureRect; }
            set {
                if (_textureRect != value) {
                    _textureRect = value;
                    _dirty = true;
                }
            }
        }

        public Color channelWeights {
            get { return _channelWeights; }
            set {
                if (_channelWeights != value) {
                    _channelWeights = value;
                    _dirty = true;
                }
            }
        }

        public float raycastThreshold {
            get { return _raycastThreshold; }
            set { _raycastThreshold = value; }
        }

        public bool isMaskingEnabled {
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

        public bool IsRaycastLocationValid(Vector2 sp, Camera cam) {
            Vector2 localPos;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, sp, cam, out localPos)) return false;
            if (!_parameters.texture) return true;
            if (!Mathr.Inside(localPos, _parameters.maskRect)) return false;
            if (_raycastThreshold <= 0.0f) return true;
            float mask;
            if (!_parameters.SampleMask(localPos, out mask)) {
                Debug.LogError("raycastThreshold greater than 0 can't be used on SoftMask whose texture cannot be read.", this);
                return true;
            }   
            return mask >= _raycastThreshold;
        }

        protected override void Start() {
            base.Start();
            WarnIfDefaultShaderIsNotSet();
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

        protected virtual void LateUpdate() {
            SpawnMaskablesInChildren();
            var prevGraphic = _graphic;
            FindGraphic();
            if (transform.hasChanged || _dirty || !ReferenceEquals(_graphic, prevGraphic))
                UpdateMask();
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

        static readonly Rect DefaultRectUV = new Rect(0, 0, 1, 1);

        RectTransform _rectTransform;
        RectTransform rectTransform { get { return _rectTransform ?? (_rectTransform = GetComponent<RectTransform>()); } }

        Graphic _graphic;

        Canvas _canvas;
        Canvas canvas { get { return _canvas ?? (_canvas = _graphic ? _graphic.canvas : null); } } // TODO implement directly!
        
        bool isBasedOnGraphic { get { return _source == MaskSource.Graphic; } }
        
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
                var replacement = _defaultShader ? new Material(_defaultShader) : null;
                if (replacement && original)
                    replacement.CopyPropertiesFromMaterial(original);
                return replacement;
            } else if (original.SupportsSoftMask())
                return new Material(original);
            else
                return null;
        }

        void CalculateMaskParameters() {
            switch (_source) {
                case MaskSource.Graphic:
                    if (_graphic is Image)
                        CalculateImageBased((Image)_graphic);
                    else if (_graphic is RawImage)
                        CalculateRawImageBased((RawImage)_graphic);
                    else
                        CalculateSolidFill();
                    break;
                case MaskSource.Sprite:
                    CalculateSpriteBased(_sprite, _spriteBorderMode);
                    break;
                case MaskSource.Texture:
                    CalculateTextureBased(_texture, _textureRect);
                    break;
                default:
                    Debug.LogWarningFormat("Unknown MaskSource: {0}", _source);
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
            Assert.IsNotNull(image);
            CalculateSpriteBased(image.sprite, ToBorderMode(image.type));
        }

        void CalculateRawImageBased(RawImage image) {
            Assert.IsNotNull(image);
            CalculateTextureBased(image.texture as Texture2D, image.uvRect);
        }

        void CalculateSpriteBased(Sprite sprite, BorderMode spriteMode) {
            if (!sprite) {
                CalculateSolidFill();
                return;
            }   
            FillCommonParameters();
            var textureRectInFullRect = Mathr.Div(Mathr.BorderOf(sprite.rect, sprite.textureRect), sprite.rect.size);
            var textureRect = Mathr.ToVector(sprite.textureRect);
            var textureSize = new Vector2(sprite.texture.width, sprite.texture.height);
            var fullMaskRect = LocalRect(Vector4.zero);
            _parameters.maskRect = Mathr.ApplyBorder(fullMaskRect, Mathr.Mul(textureRectInFullRect, Mathr.Size(fullMaskRect)));
            _parameters.maskBorder = LocalRect(sprite.border * GraphicToCanvas(sprite));
            _parameters.maskRectUV = Mathr.Div(textureRect, textureSize);
            _parameters.maskBorderUV = Mathr.ApplyBorder(_parameters.maskRectUV, Mathr.Div(sprite.border, textureSize));
            _parameters.texture = sprite.texture;
            _parameters.borderMode = spriteMode;
            if (spriteMode == BorderMode.Tiled)
                _parameters.tileRepeat = MaskRepeat(sprite, _parameters.maskBorder);
        }

        void CalculateTextureBased(Texture2D texture, Rect uvRect) {
            FillCommonParameters();
            _parameters.maskRect = LocalRect(Vector4.zero);
            _parameters.maskRectUV = Mathr.ToVector(uvRect);
            _parameters.texture = texture;
            _parameters.borderMode = BorderMode.Simple;
        }

        void CalculateSolidFill() {
            CalculateTextureBased(null, DefaultRectUV);
        }

        void FillCommonParameters() {
            _parameters.worldToMask = WorldToMask();
            _parameters.maskChannelWeights = _channelWeights;
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
            return Mathr.ApplyBorder(Mathr.ToVector(rectTransform.rect), border);
        }

        Vector2 MaskRepeat(Sprite sprite, Vector4 centralPart) {
            var textureCenter = Mathr.ApplyBorder(Mathr.ToVector(sprite.textureRect), sprite.border);
            return Mathr.Div(Mathr.Size(centralPart) * GraphicToCanvas(sprite), Mathr.Size(textureCenter));
        }

        float MaskValue(Color mask) {
            var value = mask * _parameters.maskChannelWeights;
            return value.a + value.r + value.g + value.b;
        }

        void WarnIfDefaultShaderIsNotSet() {
            if (!_defaultShader)
                Debug.LogWarningFormat(this, "Mask may be not work because it's defaultShader is not set");
        }

        // Various operations on Rect represented as Vector4. 
        // In Vector4 Rect is stored as (xMin, yMin, xMax, yMax).
        static class Mathr {
            public static Vector4 ToVector(Rect r) { return new Vector4(r.xMin, r.yMin, r.xMax, r.yMax); }
            public static Vector4 Div(Vector4 v, Vector2 s) { return new Vector4(v.x / s.x, v.y / s.y, v.z / s.x, v.w / s.y); }
            public static Vector2 Div(Vector2 v, Vector2 s) { return new Vector2(v.x / s.x, v.y / s.y); }
            public static Vector4 Mul(Vector4 v, Vector2 s) { return new Vector4(v.x * s.x, v.y * s.y, v.z * s.x, v.w * s.y); }
            public static Vector2 Size(Vector4 r) { return new Vector2(r.z - r.x, r.w - r.y); }

            public static Vector4 BorderOf(Rect outer, Rect inner) {
                return new Vector4(inner.xMin - outer.xMin, inner.yMin - outer.yMin, outer.xMax - inner.xMax, outer.yMax - inner.yMax);
            }

            public static Vector4 ApplyBorder(Vector4 v, Vector4 b) {
                return new Vector4(v.x + b.x, v.y + b.y, v.z - b.z, v.w - b.w);
            }

            public static Vector2 Min(Vector4 r) { return new Vector2(r.x, r.y); }
            public static Vector2 Max(Vector4 r) { return new Vector2(r.z, r.w); }

            public static Vector2 Remap(Vector2 c, Vector4 r1, Vector4 r2) {
                var r1size = Max(r1) - Min(r1);
                var r2size = Max(r2) - Min(r2);
                return Vector2.Scale(Div((c - Min(r1)), r1size), r2size) + Min(r2);
            }

            public static bool Inside(Vector2 v, Vector4 r) {
                return v.x >= r.x && v.y >= r.y && v.x <= r.z && v.y <= r.w;
            }
        }

        struct MaterialParameters {
            public Vector4 maskRect;
            public Vector4 maskBorder;
            public Vector4 maskRectUV;
            public Vector4 maskBorderUV;
            public Vector2 tileRepeat;
            public Color maskChannelWeights;
            public Matrix4x4 worldToMask;
            public Texture2D texture;
            public BorderMode borderMode;

            public Texture2D activeTexture { get { return texture ? texture : Texture2D.whiteTexture; } }

            public bool SampleMask(Vector2 localPos, out float mask) {
                var uv = XY2UV(localPos);
                try {
                    mask = MaskValue(texture.GetPixelBilinear(uv.x, uv.y));
                    return true;
                } catch (UnityException) {
                    mask = 0;
                    return false;
                }
            }

            public void Apply(Material mat) {
                mat.SetTexture("_SoftMask", activeTexture);
                mat.SetVector("_SoftMask_Rect", maskRect);
                mat.SetVector("_SoftMask_UVRect", maskRectUV);
                mat.SetColor("_SoftMask_ChannelWeights", maskChannelWeights);
                mat.SetMatrix("_SoftMask_WorldToMask", worldToMask);
                mat.EnableKeyword("SOFTMASK_SLICED", borderMode == BorderMode.Sliced);
                mat.EnableKeyword("SOFTMASK_TILED", borderMode == BorderMode.Tiled);
                if (borderMode != BorderMode.Simple) {
                    mat.SetVector("_SoftMask_BorderRect", maskBorder);
                    mat.SetVector("_SoftMask_UVBorderRect", maskBorderUV);
                    if (borderMode == BorderMode.Tiled)
                        mat.SetVector("_SoftMask_TileRepeat", tileRepeat);
                }
            }

            // Next functions performs the same logic as functions from SoftMask.cginc. 
            // They implemented it a bit different way, because there is no such convenient
            // vector operations in Unity/C# and there is a much little penalties for conditions.

            Vector2 XY2UV(Vector2 localPos) {
                switch (borderMode) {
                    case BorderMode.Simple: return MapSimple(localPos);
                    case BorderMode.Sliced: return MapBorder(localPos, repeat: false);
                    case BorderMode.Tiled: return MapBorder(localPos, repeat: true);
                    default:
                        Debug.LogError("Unknown BorderMode");
                        return MapSimple(localPos);
                }
            }

            Vector2 MapSimple(Vector2 localPos) {
                return Mathr.Remap(localPos, maskRect, maskRectUV);
            }

            Vector2 MapBorder(Vector2 localPos, bool repeat) {
                return
                    new Vector2(
                        Inset(
                            localPos.x, 
                            maskRect.x, maskBorder.x, maskBorder.z, maskRect.z, 
                            maskRectUV.x, maskBorderUV.x, maskBorderUV.z, maskRectUV.z, 
                            repeat ? tileRepeat.x : 1),
                        Inset(
                            localPos.y,
                            maskRect.y, maskBorder.y, maskBorder.w, maskRect.w,
                            maskRectUV.y, maskBorderUV.y, maskBorderUV.w, maskRectUV.w,
                            repeat ? tileRepeat.y : 1));
            }
            
            float Inset(float v, float x1, float x2, float u1, float u2, float repeat = 1) {
                return Frac((v - x1) / (x2 - x1) * repeat) * (u2 - u1) + u1;
            }

            float Inset(float v, float x1, float x2, float x3, float x4, float u1, float u2, float u3, float u4, float repeat = 1) {
                if (v < x2)
                    return Inset(v, x1, x2, u1, u2);
                else if (v < x3)
                    return Inset(v, x2, x3, u2, u3, repeat);
                else
                    return Inset(v, x3, x4, u3, u4);
            }

            float Frac(float v) { return v - Mathf.Floor(v); } 

            float MaskValue(Color mask) {
                var value = mask * maskChannelWeights;
                return value.a + value.r + value.g + value.b;
            }
        }
    }
}
