using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SoftMasking.Extensions;

namespace SoftMasking {
    /// <summary>
    /// Contains some predefined combinations of mask channel weights.
    /// </summary>
    public static class MaskChannel {
        public static Color alpha   = new Color(0, 0, 0, 1);
        public static Color red     = new Color(1, 0, 0, 0);
        public static Color green   = new Color(0, 1, 0, 0);
        public static Color blue    = new Color(0, 0, 1, 0);
        public static Color gray    = new Color(1, 1, 1, 0) / 3.0f;
    }

    /// <summary>
    /// SoftMask is a component that can be added to UI elements for masking the children. It works
    /// like a standard Unity's <see cref="Mask"/> but supports alpha.
    /// </summary>
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/Soft Mask", 14)]
    [RequireComponent(typeof(RectTransform))]
    [HelpURL("https://docs.google.com/document/d/1XhJFNFHNyKXwWsErLkd1FBw0YgOCeo4qkjrMW9_H-hc")]
    public class SoftMask : UIBehaviour, ISoftMask, ICanvasRaycastFilter {
        //
        // How it works:
        //
        // SoftMask overrides Shader used by child elements. To do it, SoftMask spawns invisible 
        // SoftMaskable components on them on the fly. SoftMaskable implements IMaterialOverride,
        // which allows it to override the shader that performs actual rendering. Use of
        // IMaterialOverride is transparent to the user: a material assigned to Graphic in the 
        // inspector is left untouched.
        //
        // Management of SoftMaskables is fully automated. SoftMaskables are kept on the child
        // objects while any SoftMask parent present. When something changes and SoftMask parent
        // no longer exists, SoftMaskable is destroyed automatically. So, a user of SoftMask
        // doesn't have to worry about any component changes in the hierarchy.
        //
        // The replacement shader samples the mask texture and multiply the resulted color 
        // accordingly. SoftMask has the predefined replacement for Unity's default UI shader 
        // (and its ETC1-version in Unity 5.4+). So, when SoftMask 'sees' a material that uses a
        // known shader, it overrides shader by the predefined one. If SoftMask encounters a
        // material with an unknown shader, it can't do anything reasonable (because it doesn't know
        // what that shader should do). In such a case, SoftMask will not work and a warning will
		// be displayed in Console. If you want SoftMask to work with a custom shader, you can
		// manually add support to this shader. For reference how to do it, see
		// CustomWithSoftMask.shader from included samples.
        //
        // All replacements are cached in SoftMask instances. By default Unity draws UI with a
        // very small amount of material instances (they are spawned one per masking/clipping layer),
        // so, SoftMask creates a relatively small amount of overrides.
        //

        [SerializeField] Shader _defaultShader = null;
        [SerializeField] Shader _defaultETC1Shader = null;
        [SerializeField] MaskSource _source = MaskSource.Graphic;
        [SerializeField] RectTransform _separateMask = null;
        [SerializeField] Sprite _sprite = null;
        [SerializeField] BorderMode _spriteBorderMode = BorderMode.Simple;
        [SerializeField] Texture2D _texture = null;
        [SerializeField] Rect _textureUVRect = DefaultUVRect;
        [SerializeField] Color _channelWeights = MaskChannel.alpha;
        [SerializeField] float _raycastThreshold = 0.0f;

        MaterialReplacements _materials;
        MaterialParameters _parameters;
        Sprite _lastUsedSprite;
        Rect _lastMaskRect;
        bool _maskingWasEnabled;
        bool _destroyed;
        bool _dirty;

        // Cached components
        RectTransform _maskTransform;
        Graphic _graphic;
        Canvas _canvas;

        public SoftMask() {
            var materialReplacer = 
                new MaterialReplacerChain(
                    MaterialReplacer.globalReplacers,
                    new MaterialReplacerImpl(this));
            _materials = new MaterialReplacements(materialReplacer, m => _parameters.Apply(m));
        }

        /// <summary>
        /// Source of the mask's image.
        /// </summary>
        [Serializable]
        public enum MaskSource {
            /// <summary>
            /// The mask image should be taken from the Graphic component of the containing 
            /// GameObject. Only Image and RawImage components are supported. If there is no
            /// appropriate Graphic on the GameObject, a solid rectangle of the RectTransform
            /// dimensions will be used.
            /// </summary>
            Graphic,
            /// <summary>
            /// The mask image should be taken from an explicitly specified Sprite. When this mode
            /// is used, spriteBorderMode can also be set to determine how to process Sprite's
            /// borders. If the sprite isn't set, a solid rectangle of the RectTransform dimensions 
            /// will be used. This mode is analogous to using an Image with according sprite and 
            /// type set.
            /// </summary>
            Sprite,
            /// <summary>
            /// The mask image should be taken from an explicitly specified Texture2D. When this
            /// mode is used, textureUVRect can also be set to determine what part of the texture
            /// should be used. If the texture isn't set, a solid rectangle of the RectTransform
            /// dimensions will be used. This mode is analogous to using a RawImage with according 
            /// texture and uvRect set.
            /// </summary>
            Texture
        }

        /// <summary>
        /// How Sprite's borders should be processed. It is a reduced set of Image.Type values.
        /// </summary>
        [Serializable]
        public enum BorderMode {
            /// <summary>
            /// Sprite should be drawn as a whole, ignoring any borders set. It works the
            /// same way as Unity's Image.Type.Simple.
            /// </summary>
            Simple,
            /// <summary>
            /// Sprite borders should be stretched when the drawn image is larger that the
            /// source. It works the same way as Unity's Image.Type.Sliced.
            /// </summary>
            Sliced,
            /// <summary>
            /// The same as Sliced, but border fragments will be repeated instead of
            /// stretched. It works the same way as Unity's Image.Type.Tiled.
            /// </summary>
            Tiled
        }
        
        /// <summary>
        /// Errors encountered during SoftMask diagnostics. Mostly intended to use in Unity Editor.
        /// </summary>
        [Flags]
        [Serializable]
        public enum Errors {
            NoError                 = 0,
            UnsupportedShaders      = 1 << 0,
            NestedMasks             = 1 << 1,
            TightPackedSprite       = 1 << 2,
            AlphaSplitSprite        = 1 << 3,
            UnsupportedImageType    = 1 << 4,
            UnreadableTexture       = 1 << 5
        }

        /// <summary>
        /// Specifies a Shader that should be used as a replacement of the Unity's default UI
        /// shader. If you add SoftMask in play-time by AddComponent(), you should set 
        /// this property manually.
        /// </summary>
        public Shader defaultShader {
            get { return _defaultShader; }
            set { SetShader(ref _defaultShader, value); }
        }

        /// <summary>
        /// Specifies a Shader that should be used as a replacement of the Unity's default UI
        /// shader with ETC1 (alpha-split) support. If you use ETC1 textures in UI and
        /// add SoftMask in play-time by AddComponent(), you should set this property manually.
        /// </summary>
        public Shader defaultETC1Shader {
            get { return _defaultETC1Shader; }
            set { SetShader(ref _defaultETC1Shader, value, warnIfNotSet: false); }
        }

        /// <summary>
        /// Determines from where the mask image should be taken.
        /// </summary>
        public MaskSource source {
            get { return _source; }
            set { if (_source != value) Set(ref _source, value); }
        }

        /// <summary>
        /// Specifies a RectTransform that should be used as a mask. It allows to separate 
        /// a mask from a masking hierarchy root, which simplifies creation of moving or 
        /// sliding masks. When null, the RectTransform of the current object will be used.
        /// Default value is null.
        /// </summary>
        public RectTransform separateMask {
            get { return _separateMask; }
            set {
                if (_separateMask != value) {
                    Set(ref _separateMask, value);
                    // We should search them again
                    _graphic = null;
                    _maskTransform = null;
                }
            }
        }

        /// <summary>
        /// Specifies a Sprite that should be used as the mask image. This property takes
        /// effect only when the source is MaskSource.Sprite.
        /// </summary>
        public Sprite sprite {
            get { return _sprite; }
            set { if (_sprite != value) Set(ref _sprite, value); }
        }

        /// <summary>
        /// Specifies the draw mode of sprite borders. This property takes effect only when the
        /// source is MaskSource.Sprite.
        /// </summary>
        public BorderMode spriteBorderMode {
            get { return _spriteBorderMode; }
            set { if (_spriteBorderMode != value) Set(ref _spriteBorderMode, value); }
        }

        /// <summary>
        /// Specifies a Texture2D that should be used as the mask image. This property takes
        /// effect only when the source is MaskSource.Texture.
        /// </summary>
        public Texture2D texture {
            get { return _texture; }
            set { if (_texture != value) Set(ref _texture, value); }
        }

        /// <summary>
        /// Specifies an UV rectangle defining the image part, that should be used as 
        /// the mask image. This property takes effect only when the source is MaskSource.Texture.
        /// A value is set in normalized coordinates. The default value is (0, 0, 1, 1), which means
        /// that the whole texture is used.
        /// </summary>
        public Rect textureUVRect {
            get { return _textureUVRect; }
            set { if (_textureUVRect != value) Set(ref _textureUVRect, value); }
        }

        /// <summary>
        /// Specifies weights of the color channels of the mask. The color sampled from the mask 
        /// texture is multiplied by this value, after what all components are summed up together.
        /// That is, the final mask value is calculated as:
        ///     color = `pixel-from-mask` * channelWeights
        ///     value = color.r + color.g + color.b + color.a
        /// The `value` is a number by which the resulting pixel's alpha is multiplied. As you
        /// can see, the result value isn't normalized, so, you should account it while defining
        /// custom values for this property.
        /// Static class MaskChannel contains some useful predefined values. You can use they
        /// as example of how mask calculation works.
        /// The default value is MaskChannel.alpha.
        /// </summary>
        public Color channelWeights {
            get { return _channelWeights; }
            set { if (_channelWeights != value) Set(ref _channelWeights, value); }
        }

        /// <summary>
        /// Specifies the minimum mask value that the point should have for an input event to pass.
        /// If the value sampled from the mask is greater or equal this value, the input event
        /// is considered 'hit'. The mask value is compared with raycastThreshold after
        /// channelWeights applied.
        /// The default value is 0, which means that any pixel belonging to RectTransform is
        /// considered in input events. If you specify the value greater than 0, the mask's 
        /// texture should be readable.
        /// Accepts values in range [0..1].
        /// </summary>
        public float raycastThreshold {
            get { return _raycastThreshold; }
            set { _raycastThreshold = value; }
        }

        /// <summary>
        /// Returns true if Soft Mask does raycast filtering, that is if the masked areas are
        /// transparent to input.
        /// </summary>
        public bool isUsingRaycastFiltering {
            get { return _raycastThreshold > 0f; }
        }

        /// <summary>
        /// Returns true if masking is currently active.
        /// </summary>
        public bool isMaskingEnabled {
            get { return isActiveAndEnabled && canvas; }
        }

        /// <summary>
        /// Checks for errors and returns them as flags. It is used in the editor to determine
        /// which warnings should be displayed.
        /// </summary>
        public Errors PollErrors() { return new Diagnostics(this).PollErrors(); }

        // ICanvasRaycastFilter
        public bool IsRaycastLocationValid(Vector2 sp, Camera cam) {
            Vector2 localPos;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(maskTransform, sp, cam, out localPos)) return false;
            if (!Mathr.Inside(localPos, LocalMaskRect(Vector4.zero))) return false;
            if (!_parameters.texture) return true;
            if (!isUsingRaycastFiltering) return true;
            float mask;
            if (!_parameters.SampleMask(localPos, out mask)) {
                Debug.LogErrorFormat(this,
                    "Raycast Threshold greater than 0 can't be used on Soft Mask with texture '{0}' because "
                    + "it's not readable. You can make the texture readable in the Texture Import Settings.",
                    _parameters.activeTexture.name);
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
            SubscribeOnWillRenderCanvases();
            SpawnMaskablesInChildren(transform);
            FindGraphic();
            if (isMaskingEnabled)
                UpdateMaskParameters();
            NotifyChildrenThatMaskMightChanged();
        }

        protected override void OnDisable() {
            base.OnDisable();
            UnsubscribeFromWillRenderCanvases();
            if (_graphic) {
                _graphic.UnregisterDirtyVerticesCallback(OnGraphicDirty);
                _graphic.UnregisterDirtyMaterialCallback(OnGraphicDirty);
                _graphic = null;
            }
            NotifyChildrenThatMaskMightChanged();
            DestroyMaterials();
        }
       
        protected override void OnDestroy() {
            base.OnDestroy();
            _destroyed = true;
            NotifyChildrenThatMaskMightChanged();
        }
        
        protected virtual void LateUpdate() {
            var maskingEnabled = isMaskingEnabled;
            if (maskingEnabled) {
                if (_maskingWasEnabled != maskingEnabled)
                    SpawnMaskablesInChildren(transform);
                var prevGraphic = _graphic;
                FindGraphic();
                if (_lastMaskRect != maskTransform.rect
                        || !ReferenceEquals(_graphic, prevGraphic))
                    _dirty = true;
            }
            _maskingWasEnabled = maskingEnabled;
        }

        protected override void OnRectTransformDimensionsChange() {
            base.OnRectTransformDimensionsChange();
            _dirty = true;
        }

        protected override void OnDidApplyAnimationProperties() {
            base.OnDidApplyAnimationProperties();
            _dirty = true;
        }

    #if UNITY_EDITOR
        protected override void OnValidate() {
            base.OnValidate();
            _dirty = true;
            _maskTransform = null;
            _graphic = null;
        }
    #endif

        protected override void OnTransformParentChanged() {
            base.OnTransformParentChanged();
            _canvas = null;
            _dirty = true;
        }

        protected override void OnCanvasHierarchyChanged() {
            base.OnCanvasHierarchyChanged();
            _canvas = null;
            _dirty = true;
            NotifyChildrenThatMaskMightChanged();
        }

        void OnTransformChildrenChanged() {
            SpawnMaskablesInChildren(transform);
        }
         
        void SubscribeOnWillRenderCanvases() {
            // To get called when layout and graphics update is finished we should
            // subscribe after CanvasUpdateRegistry. CanvasUpdateRegistry subscribes
            // in his constructor, so we force its execution.
            Touch(CanvasUpdateRegistry.instance);
            Canvas.willRenderCanvases += OnWillRenderCanvases;
        }

        void UnsubscribeFromWillRenderCanvases() {
            Canvas.willRenderCanvases -= OnWillRenderCanvases;
        }

        void OnWillRenderCanvases() {
            // To be sure that mask will match the state of another drawn UI elements,
            // we update material parameters when layout and graphic update is done,
            // just before actual rendering.
            if (isMaskingEnabled)
                UpdateMaskParameters();
        }
        
        static T Touch<T>(T obj) { return obj; }

        static readonly Rect DefaultUVRect = new Rect(0, 0, 1, 1);

        RectTransform maskTransform {
            get {
                return
                    _maskTransform
                        ? _maskTransform
                        : (_maskTransform = _separateMask ? _separateMask : GetComponent<RectTransform>());
            }
        }

        Canvas canvas {
            get { return _canvas ? _canvas : (_canvas = NearestEnabledCanvas()); }
        }

        bool isBasedOnGraphic { get { return _source == MaskSource.Graphic; } }

        bool ISoftMask.isAlive { get { return this && !_destroyed; } }

        Material ISoftMask.GetReplacement(Material original) {
            Assert.IsTrue(isActiveAndEnabled);
            return _materials.Get(original);
        }

        void ISoftMask.ReleaseReplacement(Material replacement) {
            _materials.Release(replacement);
        }

        void ISoftMask.UpdateTransformChildren(Transform transform) {
            SpawnMaskablesInChildren(transform);
        }

        void OnGraphicDirty() {
            if (isBasedOnGraphic)
                _dirty = true;
        }

        void FindGraphic() {
            if (!_graphic && isBasedOnGraphic) {
                _graphic = maskTransform.GetComponent<Graphic>();
                if (_graphic) {
                    _graphic.RegisterDirtyVerticesCallback(OnGraphicDirty);
                    _graphic.RegisterDirtyMaterialCallback(OnGraphicDirty);
                }
            }
        }

        Canvas NearestEnabledCanvas() {
            // It's a rare operation, so I do not optimize it with static lists
            var canvases = GetComponentsInParent<Canvas>(false);
            for (int i = 0; i < canvases.Length; ++i)
                if (canvases[i].isActiveAndEnabled)
                    return canvases[i];
            return null;
        }

        void UpdateMaskParameters() {
            Assert.IsTrue(isMaskingEnabled);
            if (_dirty || maskTransform.hasChanged) {
                CalculateMaskParameters();
                maskTransform.hasChanged = false;
                _lastMaskRect = maskTransform.rect;
                _dirty = false;
            }
            _materials.ApplyAll();
        }

        void SpawnMaskablesInChildren(Transform root) {
            using (new ClearListAtExit<SoftMaskable>(s_maskables))
                for (int i = 0; i < root.childCount; ++i) {
                    var child = root.GetChild(i);
                    child.GetComponents(s_maskables);
                    Assert.IsTrue(s_maskables.Count <= 1);
                    if (s_maskables.Count == 0)
                        child.gameObject.AddComponent<SoftMaskable>();
                }
        }

        void InvalidateChildren() {
            ForEachChildMaskable(x => x.Invalidate());
        }

        void NotifyChildrenThatMaskMightChanged() {
            ForEachChildMaskable(x => x.MaskMightChanged());
        }

        void ForEachChildMaskable(Action<SoftMaskable> f) {
            transform.GetComponentsInChildren(s_maskables);
            using (new ClearListAtExit<SoftMaskable>(s_maskables))
                for (int i = 0; i < s_maskables.Count; ++i) {
                    var maskable = s_maskables[i];
                    if (maskable && maskable.gameObject != gameObject)
                        f(maskable);
                }
        }

        void DestroyMaterials() {
            _materials.DestroyAllAndClear();
        }

        struct SourceParameters {
            public Image image;
            public Sprite sprite;
            public BorderMode spriteBorderMode;
            public Texture2D texture;
            public Rect textureUVRect;
        }

        SourceParameters DeduceSourceParameters() {
            var result = new SourceParameters();
            switch (_source) {
                case MaskSource.Graphic:
                    if (_graphic is Image) {
                        result.image = (Image)_graphic;
                        result.sprite = result.image.sprite;
                        result.spriteBorderMode = ToBorderMode(result.image.type);
                        result.texture = result.sprite ? result.sprite.texture : null;
                    } else if (_graphic is RawImage) {
                        var rawImage = (RawImage)_graphic;
                        result.texture = rawImage.texture as Texture2D;
                        result.textureUVRect = rawImage.uvRect;
                    }
                    break;
                case MaskSource.Sprite:
                    result.sprite = _sprite;
                    result.spriteBorderMode = _spriteBorderMode;
                    result.texture = result.sprite ? result.sprite.texture : null; // TODO make SourceParameters immutable and expose specific ctors?
                    break;
                case MaskSource.Texture:
                    result.texture = _texture;
                    result.textureUVRect = _textureUVRect;
                    break;
                default:
                    Debug.LogErrorFormat(this, "Unknown MaskSource: {0}", _source);
                    break;
            }
            return result;
        }

        BorderMode ToBorderMode(Image.Type imageType) {
            switch (imageType) {
                case Image.Type.Simple: return BorderMode.Simple;
                case Image.Type.Sliced: return BorderMode.Sliced;
                case Image.Type.Tiled: return BorderMode.Tiled;
                default:
                    Debug.LogErrorFormat(
                        this,
                        "SoftMask doesn't support image type {0}. Image type Simple will be used.",
                        imageType);
                    return BorderMode.Simple;
            }
        }

        void CalculateMaskParameters() {
            var sourceParams = DeduceSourceParameters();
            if (sourceParams.sprite)
                CalculateSpriteBased(sourceParams.sprite, sourceParams.spriteBorderMode);
            else if (sourceParams.texture)
                CalculateTextureBased(sourceParams.texture, sourceParams.textureUVRect);
            else
                CalculateSolidFill();
        }

        void CalculateSpriteBased(Sprite sprite, BorderMode borderMode) {
            var lastSprite = _lastUsedSprite;
            _lastUsedSprite = sprite;
            var spriteErrors = Diagnostics.CheckSprite(sprite);
            if (spriteErrors != Errors.NoError) {
                if (lastSprite != sprite)
                    WarnSpriteErrors(spriteErrors);
                CalculateSolidFill();
                return;
            }
            if (!sprite) {
                CalculateSolidFill();
                return;
            }
            FillCommonParameters();
            var spriteRect = Mathr.Move(Mathr.ToVector(sprite.rect), sprite.textureRect.position - sprite.rect.position - sprite.textureRectOffset);
            var textureRect = Mathr.ToVector(sprite.textureRect);
            var textureBorder = Mathr.BorderOf(spriteRect, textureRect);
            var textureSize = new Vector2(sprite.texture.width, sprite.texture.height);
            var fullMaskRect = LocalMaskRect(Vector4.zero);
            _parameters.maskRectUV = Mathr.Div(textureRect, textureSize);
            if (borderMode == BorderMode.Simple) {
                var textureRectInFullRect = Mathr.Div(textureBorder, Mathr.Size(spriteRect));
                _parameters.maskRect = Mathr.ApplyBorder(fullMaskRect, Mathr.Mul(textureRectInFullRect, Mathr.Size(fullMaskRect)));
            } else {
                _parameters.maskRect = Mathr.ApplyBorder(fullMaskRect, textureBorder * GraphicToCanvasScale(sprite));
                var fullMaskRectUV = Mathr.Div(spriteRect, textureSize);
                var adjustedBorder = AdjustBorders(sprite.border * GraphicToCanvasScale(sprite), fullMaskRect);
                _parameters.maskBorder = LocalMaskRect(adjustedBorder);
                _parameters.maskBorderUV = Mathr.ApplyBorder(fullMaskRectUV, Mathr.Div(sprite.border, textureSize));
            }
            _parameters.texture = sprite.texture;
            _parameters.borderMode = borderMode;
            if (borderMode == BorderMode.Tiled)
                _parameters.tileRepeat = MaskRepeat(sprite, _parameters.maskBorder);
        }

        static Vector4 AdjustBorders(Vector4 border, Vector4 rect) {
            // Copied from Unity's Image.
            var size = Mathr.Size(rect);
            for (int axis = 0; axis <= 1; axis++) {
                // If the rect is smaller than the combined borders, then there's not room for
                // the borders at their normal size. In order to avoid artefacts with overlapping
                // borders, we scale the borders down to fit.
                float combinedBorders = border[axis] + border[axis + 2];
                if (size[axis] < combinedBorders && combinedBorders != 0) {
                    float borderScaleRatio = size[axis] / combinedBorders;
                    border[axis] *= borderScaleRatio;
                    border[axis + 2] *= borderScaleRatio;
                }
            }
            return border;
        }

        void CalculateTextureBased(Texture2D texture, Rect uvRect) {
            FillCommonParameters();
            _parameters.maskRect = LocalMaskRect(Vector4.zero);
            _parameters.maskRectUV = Mathr.ToVector(uvRect);
            _parameters.texture = texture;
            _parameters.borderMode = BorderMode.Simple;
        }

        void CalculateSolidFill() {
            CalculateTextureBased(null, DefaultUVRect);
        }

        void FillCommonParameters() {
            _parameters.worldToMask = WorldToMask();
            _parameters.maskChannelWeights = _channelWeights;
        }

        float GraphicToCanvasScale(Sprite sprite) {
            var canvasPPU = canvas ? canvas.referencePixelsPerUnit : 100;
            var maskPPU = sprite ? sprite.pixelsPerUnit : 100;
            return canvasPPU / maskPPU;
        }

        Matrix4x4 WorldToMask() {
            return maskTransform.worldToLocalMatrix * canvas.rootCanvas.transform.localToWorldMatrix;
        }

        Vector4 LocalMaskRect(Vector4 border) {
            return Mathr.ApplyBorder(Mathr.ToVector(maskTransform.rect), border);
        }

        Vector2 MaskRepeat(Sprite sprite, Vector4 centralPart) {
            var textureCenter = Mathr.ApplyBorder(Mathr.ToVector(sprite.textureRect), sprite.border);
            return Mathr.Div(Mathr.Size(centralPart) * GraphicToCanvasScale(sprite), Mathr.Size(textureCenter));
        }

        void WarnIfDefaultShaderIsNotSet() {
            if (!_defaultShader)
                Debug.LogWarning("SoftMask may not work because its defaultShader is not set", this);
        }

        void WarnSpriteErrors(Errors errors) {
            if ((errors & Errors.TightPackedSprite) != 0)
                Debug.LogError("SoftMask doesn't support tight packed sprites", this);
            if ((errors & Errors.AlphaSplitSprite) != 0)
                Debug.LogError("SoftMask doesn't support sprites with an alpha split texture", this);
        }

        void Set<T>(ref T field, T value) {
            field = value;
            _dirty = true;
        }

        void SetShader(ref Shader field, Shader value, bool warnIfNotSet = true) {
            if (field != value) {
                field = value;
                if (warnIfNotSet)
                    WarnIfDefaultShaderIsNotSet();
                DestroyMaterials();
                InvalidateChildren();
            }
        }

        static readonly List<SoftMask> s_masks = new List<SoftMask>();
        static readonly List<SoftMaskable> s_maskables = new List<SoftMaskable>();

        class MaterialReplacerImpl : IMaterialReplacer {
            readonly SoftMask _owner;

            public MaterialReplacerImpl(SoftMask owner) {
                // Pass whole owner instead of just shaders because they can be changed dynamically.
                _owner = owner;
            }

            public int order { get { return 0; } }

            public Material Replace(Material original) {
                if (original == null || original.HasDefaultUIShader())
                    return Replace(original, _owner._defaultShader);
            #if UNITY_5_4_OR_NEWER
                else if (original.HasDefaultETC1UIShader())
                    return Replace(original, _owner._defaultETC1Shader);
            #endif
                else if (original.SupportsSoftMask())
                    return new Material(original);
                else
                    return null;
            }

            static Material Replace(Material original, Shader defaultReplacementShader) {
                var replacement = defaultReplacementShader
                    ? new Material(defaultReplacementShader)
                    : null;
                if (replacement && original)
                    replacement.CopyPropertiesFromMaterial(original);
                return replacement;
            }
        }

        // Various operations on a Rect represented as Vector4. 
        // In Vector4 Rect is stored as (xMin, yMin, xMax, yMax).
        static class Mathr {
            public static Vector4 ToVector(Rect r) { return new Vector4(r.xMin, r.yMin, r.xMax, r.yMax); }
            public static Vector4 Div(Vector4 v, Vector2 s) { return new Vector4(v.x / s.x, v.y / s.y, v.z / s.x, v.w / s.y); }
            public static Vector2 Div(Vector2 v, Vector2 s) { return new Vector2(v.x / s.x, v.y / s.y); }
            public static Vector4 Mul(Vector4 v, Vector2 s) { return new Vector4(v.x * s.x, v.y * s.y, v.z * s.x, v.w * s.y); }
            public static Vector2 Size(Vector4 r) { return new Vector2(r.z - r.x, r.w - r.y); }
            public static Vector4 Move(Vector4 v, Vector2 o) { return new Vector4(v.x + o.x, v.y + o.y, v.z + o.x, v.w + o.y); }

            public static Vector4 BorderOf(Vector4 outer, Vector4 inner) {
                return new Vector4(inner.x - outer.x, inner.y - outer.y, outer.z - inner.z, outer.w - inner.w);
            }

            public static Vector4 ApplyBorder(Vector4 v, Vector4 b) {
                return new Vector4(v.x + b.x, v.y + b.y, v.z - b.z, v.w - b.w);
            }

            public static Vector2 Min(Vector4 r) { return new Vector2(r.x, r.y); }
            public static Vector2 Max(Vector4 r) { return new Vector2(r.z, r.w); }

            public static Vector2 Remap(Vector2 c, Vector4 from, Vector4 to) {
                var fromSize = Max(from) - Min(from);
                var toSize = Max(to) - Min(to);
                return Vector2.Scale(Div((c - Min(from)), fromSize), toSize) + Min(to);
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
                mat.SetTexture(Ids.SoftMask, activeTexture);
                mat.SetVector(Ids.SoftMask_Rect, maskRect);
                mat.SetVector(Ids.SoftMask_UVRect, maskRectUV);
                mat.SetColor(Ids.SoftMask_ChannelWeights, maskChannelWeights);
                mat.SetMatrix(Ids.SoftMask_WorldToMask, worldToMask);
                mat.EnableKeyword("SOFTMASK_SIMPLE", borderMode == BorderMode.Simple);
                mat.EnableKeyword("SOFTMASK_SLICED", borderMode == BorderMode.Sliced);
                mat.EnableKeyword("SOFTMASK_TILED", borderMode == BorderMode.Tiled);
                if (borderMode != BorderMode.Simple) {
                    mat.SetVector(Ids.SoftMask_BorderRect, maskBorder);
                    mat.SetVector(Ids.SoftMask_UVBorderRect, maskBorderUV);
                    if (borderMode == BorderMode.Tiled)
                        mat.SetVector(Ids.SoftMask_TileRepeat, tileRepeat);
                }
            }

            // The following functions performs the same logic as functions from SoftMask.cginc. 
            // They implemented it a bit different way, because there is no such convenient
            // vector operations in Unity/C# and conditions are much cheaper here.

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
                var w = (x2 - x1);
                return Mathf.Lerp(u1, u2, w != 0.0f ? Frac((v - x1) / w * repeat) : 0.0f);
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

            static class Ids {
                public static readonly int SoftMask = Shader.PropertyToID("_SoftMask");
                public static readonly int SoftMask_Rect = Shader.PropertyToID("_SoftMask_Rect");
                public static readonly int SoftMask_UVRect = Shader.PropertyToID("_SoftMask_UVRect");
                public static readonly int SoftMask_ChannelWeights = Shader.PropertyToID("_SoftMask_ChannelWeights");
                public static readonly int SoftMask_WorldToMask = Shader.PropertyToID("_SoftMask_WorldToMask");
                public static readonly int SoftMask_BorderRect = Shader.PropertyToID("_SoftMask_BorderRect");
                public static readonly int SoftMask_UVBorderRect = Shader.PropertyToID("_SoftMask_UVBorderRect");
                public static readonly int SoftMask_TileRepeat = Shader.PropertyToID("_SoftMask_TileRepeat");
            }
        }

        struct Diagnostics {
            SoftMask _softMask;

            public Diagnostics(SoftMask softMask) { _softMask = softMask; }
            
            public Errors PollErrors() {
                var softMask = _softMask; // for use in lambda
                var result = Errors.NoError;
                softMask.GetComponentsInChildren(s_maskables);
                using (new ClearListAtExit<SoftMaskable>(s_maskables))
                    if (s_maskables.Any(m => ReferenceEquals(m.mask, softMask) && m.shaderIsNotSupported))
                        result |= Errors.UnsupportedShaders;
                if (ThereAreNestedMasks())
                    result |= Errors.NestedMasks;
                result |= CheckSprite(sprite);
                result |= CheckImage();
                result |= CheckTexture();
                return result;
            }

            public static Errors CheckSprite(Sprite sprite) {
                var result = Errors.NoError;
                if (!sprite) return result;
                if (sprite.packed && sprite.packingMode == SpritePackingMode.Tight)
                    result |= Errors.TightPackedSprite;
                if (sprite.associatedAlphaSplitTexture)
                    result |= Errors.AlphaSplitSprite;
                return result;
            }

            Image image { get { return _softMask.DeduceSourceParameters().image; } }
            Sprite sprite { get { return _softMask.DeduceSourceParameters().sprite; } }
            Texture2D texture { get { return _softMask.DeduceSourceParameters().texture; } }

            bool ThereAreNestedMasks() {
                var softMask = _softMask; // for use in lambda
                var result = false;
                using (new ClearListAtExit<SoftMask>(s_masks)) {
                    softMask.GetComponentsInParent(false, s_masks);
                    result |= s_masks.Any(x => AreCompeting(softMask, x));
                    softMask.GetComponentsInChildren(false, s_masks);
                    result |= s_masks.Any(x => AreCompeting(softMask, x));
                }
                return result;
            }

            Errors CheckImage() {
                var result = Errors.NoError;
                if (!_softMask.isBasedOnGraphic) return result;
                if (image && !IsSupportedImageType(image.type))
                    result |= Errors.UnsupportedImageType;
                return result;
            }

            Errors CheckTexture() {
                var result = Errors.NoError;
                if (_softMask.isUsingRaycastFiltering && texture)
                    if (!IsReadable(texture))
                        result |= Errors.UnreadableTexture;
                return result;
            }

            static bool AreCompeting(SoftMask softMask, SoftMask other) {
                Assert.IsNotNull(other);
                return softMask.isMaskingEnabled
                    && softMask != other
                    && other.isMaskingEnabled
                    && softMask.canvas.rootCanvas == other.canvas.rootCanvas
                    && !SelectChild(softMask, other).canvas.overrideSorting;
            }

            static T SelectChild<T>(T first, T second) where T : Component {
                Assert.IsNotNull(first);
                Assert.IsNotNull(second);
                return first.transform.IsChildOf(second.transform) ? first : second;
            }

            static bool IsReadable(Texture2D texture) {
                try {
                    texture.GetPixel(0, 0);
                    return true;
                } catch (UnityException) {
                    return false;
                }
            }

            static bool IsSupportedImageType(Image.Type type) {
                return type == Image.Type.Simple
                    || type == Image.Type.Sliced
                    || type == Image.Type.Tiled;
            }
        }
    }
}
