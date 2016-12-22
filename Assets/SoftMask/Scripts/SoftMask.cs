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
        
        [SerializeField] public Shader defaultMaskShader = null;
        [SerializeField] MaskSource _maskSource;
        
        IImpl _activeImpl;
        SpriteImpl _spriteImpl;
        RectTransform _rectTransform;
        Graphic _graphic;
        Canvas _canvas;

        Shader _appliedDefaultMaskShader;
        Vector3 _appliedPosition;
        Quaternion _appliedRotation;
        Vector3 _appliedScale;
        IImpl _appliedImpl;
        bool _rectTransformDimensionsChanged;

        public enum MaskSource { Graphic }

        public MaskSource maskSource {
            get { return _maskSource; }
            set {
                if (_maskSource != value) {
                    _maskSource = value;
                    ActualizeImpl();
                }
            } 
        }

        public bool isReady { get { return _activeImpl != null; } }

        protected virtual void Update() {
            SpawnMaskablesInChildren();
            if (_appliedDefaultMaskShader != defaultMaskShader) {
                if (!defaultMaskShader)
                    Debug.LogWarningFormat(this, "Mask may be not work because it's defaultMaskShader is set to null");
                DestroyAllOverrides();
                InvalidateChildren();
                _appliedDefaultMaskShader = defaultMaskShader;
            }
            if (_appliedPosition != transform.position
                    || _appliedRotation != transform.rotation
                    || _appliedScale != transform.lossyScale
                    || _appliedImpl != _activeImpl
                    || _rectTransformDimensionsChanged) {
                ApplyMaterialProperties();
                _appliedPosition = transform.position;
                _appliedRotation = transform.rotation;
                _appliedScale = transform.lossyScale;
                _appliedImpl = _activeImpl;
                _rectTransformDimensionsChanged = false;
            }
        }

        protected override void OnEnable() {
            base.OnEnable();
            ActualizeImpl();
        }

        protected override void OnDisable() {
            base.OnDisable();
            DestroyAllOverrides();
        }

        protected override void OnRectTransformDimensionsChange() {
            base.OnRectTransformDimensionsChange();
            _rectTransformDimensionsChanged = true;
        }

        RectTransform rectTransform { get { return _rectTransform ?? (_rectTransform = GetComponent<RectTransform>()); } }
        Graphic graphic { get { return _graphic ?? (_graphic = GetComponent<Graphic>()); } }
        Canvas canvas { get { return _canvas ?? (_canvas = _graphic ? _graphic.canvas : null); } } // TODO implement directly!
        SpriteImpl spriteImpl { get { return _spriteImpl ?? (_spriteImpl = new SpriteImpl(this)); } }

        void ActualizeImpl() {
            _activeImpl = null;
            switch (_maskSource) {
                case MaskSource.Graphic: {
                        var graphic = GetComponent<Graphic>();
                        if (graphic is Image) {
                            spriteImpl.explicitSprite = null;
                            _activeImpl = spriteImpl;
                        }
                    }
                    break;
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
                return defaultMaskShader ? new Material(defaultMaskShader) : null;
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
            if (_activeImpl != null)
                _activeImpl.ApplyMaterialProperties(mat);
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

        public interface IImpl {
            bool MaterialPropertiesChanged();
            void ApplyMaterialProperties(Material mat);
        }

        [Serializable]
        public class SpriteImpl : IImpl {
            public Sprite explicitSprite;

            SoftMask _owner;
            Sprite _appliedSprite;

            Vector4 _maskRect;
            Vector4 _maskBorder;
            Vector4 _maskRectUV;
            Vector4 _maskBorderUV;

            public SpriteImpl(SoftMask owner) {
                _owner = owner;
            }

            public bool MaterialPropertiesChanged() {
                return _appliedSprite != sprite;
            }

            public void ApplyMaterialProperties(Material mat) {
                if (sprite) {
                    CalculateMaskRect();
                    mat.SetTexture("_SoftMask", sprite.texture);
                    mat.SetVector("_SoftMask_Rect", _maskRect);
                    mat.SetVector("_SoftMask_BorderRect", _maskBorder);
                    mat.SetVector("_SoftMask_UVRect", _maskRectUV);
                    mat.SetVector("_SoftMask_UVBorderRect", _maskBorderUV);
                } else {
                    mat.SetTexture("_SoftMask", null);
                }
                
                _appliedSprite = sprite;
            }
            
            Sprite sprite { get { return !ReferenceEquals(explicitSprite, null) ? explicitSprite : OwnerSprite(); } }

            Sprite OwnerSprite() {
                var image = _owner.graphic as Image;
                if (image)
                    return image.overrideSprite ? image.overrideSprite : image.sprite;
                return null;
            }

            float graphicToCanvas {
                get {
                    var canvasPPU = _owner.canvas ? _owner.canvas.referencePixelsPerUnit : 100;
                    var maskPPU = sprite ? sprite.pixelsPerUnit : 100;
                    return canvasPPU / maskPPU;
                }
            }

            void CalculateMaskRect() {
                var textureRect = ToVector(sprite.textureRect);
                var textureSize = sprite.rect.size;
                _maskRect = _owner.CanvasSpaceRect(Vector4.zero);
                _maskBorder = _owner.CanvasSpaceRect(sprite.border * graphicToCanvas);
                _maskRectUV = Div(textureRect, textureSize);
                _maskBorderUV = Div(Inset(ToVector(sprite.rect), sprite.border), textureSize);
            }
        }
    }
}
