using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Sprites;
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
    [HelpURL("https://github.com/olegknyazev/SoftMask/blob/bf89dda09b9ac46f7baa6f2ec53448eeb319c605/Packages/com.olegknyazev.softmask/Documentation%7E/Documentation.pdf")]
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
        // objects while any SoftMask parent present. When something changes and the parent
        // no longer exists, SoftMaskable is destroyed automatically. So, a user of SoftMask
        // doesn't have to worry about any component changes in the hierarchy.
        //
        // The replacement shader samples the mask texture and multiply the resulted color 
        // accordingly. SoftMask has the predefined replacement for Unity's default UI shader 
        // (and its ETC1-version). So, when SoftMask 'sees' a material that uses a
        // known shader, it overrides shader by the predefined one. If SoftMask encounters a
        // material with an unknown shader, it can't do anything reasonable (because it doesn't know
        // what that shader should do). In such a case, SoftMask will not work and a warning will
		// be displayed in Console. If you want SoftMask to work with a custom shader, you can
		// manually add support to this shader. For reference how to do it, see
		// CustomWithSoftMask.shader from the included samples.
        //
        // All replacements are cached in SoftMask instances. By default Unity draws UI with a
        // very small number of material instances (they are spawned one per masking/clipping layer),
        // so, SoftMask creates a relatively small number of overrides.
        //

        [SerializeField] MaskSource _source = MaskSource.Graphic;
        [SerializeField] RectTransform _separateMask = null;
        [SerializeField] Sprite _sprite = null;
        [SerializeField] BorderMode _spriteBorderMode = BorderMode.Simple;
        [SerializeField] float _spritePixelsPerUnitMultiplier = 1f;
        [SerializeField] Texture _texture = null;
        [SerializeField] Rect _textureUVRect = DefaultUVRect;
        [SerializeField] Color _channelWeights = MaskChannel.alpha;
        [SerializeField] float _raycastThreshold = 0f;
        [SerializeField] bool _invertMask = false;
        [SerializeField] bool _invertOutsides = false;

        readonly MaterialReplacements _materials;
        MaterialParameters _parameters;
        WarningReporter _warningReporter;
        Rect _lastMaskRect;
        bool _maskingWasEnabled;
        bool _destroyed;
        bool _dirty;
        readonly Queue<Transform> _transformsToSpawnMaskablesIn = new Queue<Transform>();

        // Cached components
        RectTransform _maskTransform;
        Graphic _graphic;
        Canvas _canvas;

        public SoftMask() {
            var materialReplacer = 
                new MaterialReplacerChain(
                    MaterialReplacer.globalReplacers,
                    new MaterialReplacerImpl());
            _materials = new MaterialReplacements(materialReplacer, m => _parameters.Apply(m));
            _warningReporter = new WarningReporter(this);
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
            /// The mask image should be taken from an explicitly specified Texture2D or
            /// RenderTexture. When this mode is used, textureUVRect can also be set to determine
            /// which part of the texture should be used. If the texture isn't set, a solid rectangle
            /// of the RectTransform dimensions will be used. This mode is analogous to using a
            /// RawImage with according texture and uvRect set.
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
        /// Errors encountered during SoftMask diagnostics. Used by SoftMaskEditor to display
        /// hints relevant to the current state.
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
            UnreadableTexture       = 1 << 5,
            UnreadableRenderTexture = 1 << 6
        }

        /// <summary>
        /// Determines from where the mask image should be taken.
        /// </summary>
        public MaskSource source {
            get => _source;
            set { if (_source != value) Set(ref _source, value); }
        }

        /// <summary>
        /// Specifies a RectTransform that defines the bounds of the mask. Use of a separate
        /// RectTransform allows moving or resizing the mask bounds without affecting children.
        /// When null, the RectTransform of this GameObject is used.
        /// Default value is null.
        /// </summary>
        public RectTransform separateMask {
            get => _separateMask;
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
        /// effect only when source is MaskSource.Sprite.
        /// </summary>
        /// <seealso cref="source"/>
        public Sprite sprite {
            get => _sprite;
            set { if (_sprite != value) Set(ref _sprite, value); }
        }

        /// <summary>
        /// Specifies how to draw sprite borders. This property takes effect only when
        /// source is MaskSource.Sprite.
        /// </summary>
        /// <seealso cref="source"/>
        /// <seealso cref="sprite"/>
        public BorderMode spriteBorderMode {
            get => _spriteBorderMode;
            set { if (_spriteBorderMode != value) Set(ref _spriteBorderMode, value); }
        }

        /// <summary>
        /// A multiplier that is applied to the pixelsPerUnit property of the selected sprite.
        /// Default value is 1. This property takes effect only when source is MaskSource.Sprite.
        /// </summary>
        /// <seealso cref="source"/>
        /// <seealso cref="sprite"/>
        public float spritePixelsPerUnitMultiplier {
            get => _spritePixelsPerUnitMultiplier;
            set { 
                if (_spritePixelsPerUnitMultiplier != value)
                    Set(ref _spritePixelsPerUnitMultiplier, ClampPixelsPerUnitMultiplier(value));
            }
        }

        /// <summary>
        /// Specifies a Texture2D that should be used as the mask image. This property takes
        /// effect only when the source is MaskSource.Texture. This and <see cref="renderTexture"/>
        /// properties are mutually exclusive.
        /// </summary>
        /// <seealso cref="renderTexture"/>
        public Texture2D texture {
            get => _texture as Texture2D;
            set { if (_texture != value) Set(ref _texture, value); }
        }
        
        /// <summary>
        /// Specifies a RenderTexture that should be used as the mask image. This property takes
        /// effect only when the source is MaskSource.Texture. This and <see cref="texture"/>
        /// properties are mutually exclusive.
        /// </summary>
        /// <seealso cref="texture"/>
        public RenderTexture renderTexture {
            get => _texture as RenderTexture;
            set { if (_texture != value) Set(ref _texture, value); }
        }

        /// <summary>
        /// Specifies a normalized UV-space rectangle defining the image part that should be used as
        /// the mask image. This property takes effect only when the source is MaskSource.Texture.
        /// A value is set in normalized coordinates. The default value is (0, 0, 1, 1), which means
        /// that the whole texture is used.
        /// </summary>
        public Rect textureUVRect {
            get => _textureUVRect;
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
            get => _channelWeights;
            set { if (_channelWeights != value) Set(ref _channelWeights, value); }
        }

        /// <summary>
        /// Specifies the minimum mask value that the point should have for an input event to pass.
        /// If the value sampled from the mask is greater or equal this value, the input event
        /// is considered 'hit'. The mask value is compared with raycastThreshold after
        /// channelWeights applied.
        /// The default value is 0, which means that any pixel belonging to RectTransform is
        /// considered in input events. If you specify the value greater than 0, the mask's 
        /// texture should be readable and it should be not a RenderTexture.
        /// Accepts values in range [0..1].
        /// </summary>
        public float raycastThreshold {
            get => _raycastThreshold;
            set => _raycastThreshold = value;
        }

        /// <summary>
        /// If set, mask values inside the mask rectangle will be inverted. In this case mask's
        /// zero value (taking <see cref="channelWeights"/> into account) will be treated as one
        /// and vice versa. The mask rectangle is the RectTransform of the GameObject this
        /// component is attached to or <see cref="separateMask"/> if it's not null.
        /// The default value is false.
        /// </summary>
        /// <seealso cref="invertOutsides"/>
        public bool invertMask {
            get => _invertMask;
            set { if (_invertMask != value) Set(ref _invertMask, value); }
        }
        
        /// <summary>
        /// If set, mask values outside the mask rectangle will be inverted. By default, everything
        /// outside the mask rectangle has zero mask value. When this property is set, the mask
        /// outsides will have value one, which means that everything outside the mask will be
        /// visible. The mask rectangle is the RectTransform of the GameObject this component
        /// is attached to or <see cref="separateMask"/> if it's not null.
        /// The default value is false.
        /// </summary>
        /// <seealso cref="invertMask"/>
        public bool invertOutsides {
            get => _invertOutsides;
            set { if (_invertOutsides != value) Set(ref _invertOutsides, value); }
        }

        /// <summary>
        /// Returns true if Soft Mask does raycast filtering, that is if the masked areas are
        /// transparent to input.
        /// </summary>
        public bool isUsingRaycastFiltering => _raycastThreshold > 0f;

        /// <summary>
        /// Returns true if masking is currently active.
        /// </summary>
        public bool isMaskingEnabled => isActiveAndEnabled && canvas;

        /// <summary>
        /// Checks for errors and returns them as flags. It is used in the editor to determine
        /// which warnings should be displayed.
        /// </summary>
        public Errors PollErrors() { return new Diagnostics(this).PollErrors(); }

        // ICanvasRaycastFilter
        public bool IsRaycastLocationValid(Vector2 sp, Camera cam) {
            Vector2 localPos;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(maskTransform, sp, cam, out localPos)) return false;
            if (!Mathr.Inside(localPos, LocalMaskRect(Vector4.zero))) return _invertOutsides;
            if (!_parameters.texture) return true;
            if (!isUsingRaycastFiltering) return true;
            float mask;
            var sampleResult = _parameters.SampleMask(localPos, out mask);
            _warningReporter.TextureRead(_parameters.texture, sampleResult);
            if (sampleResult != MaterialParameters.SampleMaskResult.Success)
                return true;
            if (_invertMask)
                mask = 1 - mask;
            return mask >= _raycastThreshold;
        }

        protected override void OnEnable() {
            base.OnEnable();
            SubscribeOnWillRenderCanvases();
            MarkTransformForMaskablesSpawn(transform);
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
                    MarkTransformForMaskablesSpawn(transform);
                SpawnMaskables();
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
            _spritePixelsPerUnitMultiplier = ClampPixelsPerUnitMultiplier(_spritePixelsPerUnitMultiplier);
            _dirty = true;
            _maskTransform = null;
            _graphic = null;
        }
    #endif

        static float ClampPixelsPerUnitMultiplier(float value) {
            return Mathf.Max(value, 0.01f);
        }

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
            MarkTransformForMaskablesSpawn(transform);
        }

        void MarkTransformForMaskablesSpawn(Transform transform) {
            // We defer SoftMaskables spawning to LateUpdate. It lets us work around
            // the problem that MaskableGraphic doesn't respect IMaterialModifiers
            // which stay before above it in the component stack in case of creation
            // a new object. Particularly, it "solves" an issue with multiline
            // TextMesh Pro text: TMPro creates SubMesh objects and then adds a
            // Graphic component in a separate step. Deferring SoftMaskable spawning
            // to LateUpdate allows us to be sure that SoftMaskable will be spawned
            // after the Graphic is spawned.
            if (!_transformsToSpawnMaskablesIn.Contains(transform))
                _transformsToSpawnMaskablesIn.Enqueue(transform);
        }

        void SpawnMaskables() {
            while (_transformsToSpawnMaskablesIn.Count > 0) {
                var transformForSpawn = _transformsToSpawnMaskablesIn.Dequeue();
                if (transformForSpawn)
                    SpawnMaskablesInChildren(transformForSpawn);
            }
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
            // Last-second chance to spawn maskables. It helps with TextMesh Pro's
            // SubMeshes that are spawned late.
            SpawnMaskables();
            // To be sure that mask will match the state of another drawn UI elements,
            // we update material parameters when layout and graphic update is done,
            // just before actual rendering.
            if (isMaskingEnabled)
                UpdateMaskParameters();
        }

        static T Touch<T>(T obj) { return obj; }

        static readonly Rect DefaultUVRect = new Rect(0, 0, 1, 1);

        RectTransform maskTransform =>
            _maskTransform
                ? _maskTransform
                : (_maskTransform = _separateMask ? _separateMask : GetComponent<RectTransform>());

        Canvas canvas => _canvas ? _canvas : (_canvas = NearestEnabledCanvas());

        bool isBasedOnGraphic => _source == MaskSource.Graphic;

        bool ISoftMask.isAlive => this && !_destroyed;

        Material ISoftMask.GetReplacement(Material original) {
            Assert.IsTrue(isActiveAndEnabled);
            return _materials.Get(original);
        }

        void ISoftMask.ReleaseReplacement(Material replacement) {
            _materials.Release(replacement);
        }

        void ISoftMask.UpdateTransformChildren(Transform transform) {
            MarkTransformForMaskablesSpawn(transform);
        }

        void OnGraphicDirty() {
            if (isBasedOnGraphic) // TODO is this check neccessary?
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
                    var hasMaskable = false;
                    for (int j = 0; j < s_maskables.Count; ++j)
                        if (!s_maskables[j].isDestroyed) {
                            hasMaskable = true;
                            break;
                        }
                    if (!hasMaskable)
                        child.gameObject.AddComponent<SoftMaskable>();
                }
        }

        void NotifyChildrenThatMaskMightChanged() {
            ForEachChildMaskable(x => x.MaskMightChanged(), includeInactive: true);
        }

        void ForEachChildMaskable(Action<SoftMaskable> action, bool includeInactive = false) {
            transform.GetComponentsInChildren(includeInactive, s_maskables);
            using (new ClearListAtExit<SoftMaskable>(s_maskables))
                for (int i = 0; i < s_maskables.Count; ++i) {
                    var maskable = s_maskables[i];
                    if (maskable && maskable.gameObject != gameObject)
                        action(maskable);
                }
        }

        void DestroyMaterials() {
            _materials.DestroyAllAndClear();
        }

        struct SourceParameters {
            public Image image;
            public Sprite sprite;
            public BorderMode spriteBorderMode;
            public float spritePixelsPerUnit;
            public Texture texture;
            public Rect textureUVRect;
        }

        const float DefaultPixelsPerUnit = 100f;

        SourceParameters DeduceSourceParameters() {
            var result = new SourceParameters();
            switch (_source) {
                case MaskSource.Graphic:
                    if (_graphic is Image image) {
                        var sprite = image.sprite;
                        result.image = image;
                        result.sprite = sprite;
                        result.spriteBorderMode = ImageTypeToBorderMode(image.type);
                        if (sprite) {
                            result.spritePixelsPerUnit = sprite.pixelsPerUnit * image.pixelsPerUnitMultiplier;
                            result.texture = sprite.texture;
                        } else
                            result.spritePixelsPerUnit = DefaultPixelsPerUnit;
                    } else if (_graphic is RawImage rawImage) {
                        result.texture = rawImage.texture;
                        result.textureUVRect = rawImage.uvRect;
                    }
                    break;
                case MaskSource.Sprite:
                    result.sprite = _sprite;
                    result.spriteBorderMode = _spriteBorderMode;
                    if (_sprite) {
                        result.spritePixelsPerUnit = _sprite.pixelsPerUnit * _spritePixelsPerUnitMultiplier;
                        result.texture = _sprite.texture;
                    } else
                        result.spritePixelsPerUnit = DefaultPixelsPerUnit;
                    break;
                case MaskSource.Texture:
                    result.texture = _texture;
                    result.textureUVRect = _textureUVRect;
                    break;
                default:
                    Debug.LogAssertionFormat(this, "Unknown MaskSource: {0}", _source);
                    break;
            }
            return result;
        }

        public static BorderMode ImageTypeToBorderMode(Image.Type type) {
            switch (type) {
                case Image.Type.Simple: return BorderMode.Simple;
                case Image.Type.Sliced: return BorderMode.Sliced;
                case Image.Type.Tiled: return BorderMode.Tiled;
                default:
                    return BorderMode.Simple;
            }
        }
        
        public static bool IsImageTypeSupported(Image.Type type) {
            return type == Image.Type.Simple
                || type == Image.Type.Sliced
                || type == Image.Type.Tiled;
        }

        void CalculateMaskParameters() {
            var sourceParams = DeduceSourceParameters();
            _warningReporter.ImageUsed(sourceParams.image);
            var spriteErrors = Diagnostics.CheckSprite(sourceParams.sprite);
            _warningReporter.SpriteUsed(sourceParams.sprite, spriteErrors);
            if (sourceParams.sprite) {
                if (spriteErrors == Errors.NoError)
                    CalculateSpriteBased(sourceParams.sprite, sourceParams.spriteBorderMode, sourceParams.spritePixelsPerUnit);
                else
                    CalculateSolidFill();
            } else if (sourceParams.texture)
                CalculateTextureBased(sourceParams.texture, sourceParams.textureUVRect);
            else
                CalculateSolidFill();
        }

        void CalculateSpriteBased(Sprite sprite, BorderMode borderMode, float spritePixelsPerUnit) {
            FillCommonParameters();
            var inner = DataUtility.GetInnerUV(sprite);
            var outer = DataUtility.GetOuterUV(sprite);
            var padding = DataUtility.GetPadding(sprite);
            var fullMaskRect = LocalMaskRect(Vector4.zero);
            _parameters.maskRectUV = outer;
            if (borderMode == BorderMode.Simple) {
                if (ShouldPreserveAspect())
                    fullMaskRect = PreserveSpriteAspectRatio(fullMaskRect, sprite.rect.size);
                var normalizedPadding = Mathr.Div(padding, sprite.rect.size);
                _parameters.maskRect = Mathr.ApplyBorder(fullMaskRect, Mathr.Mul(normalizedPadding, Mathr.Size(fullMaskRect)));
            } else {
                var spriteToCanvasScale = SpriteToCanvasScale(spritePixelsPerUnit);
                _parameters.maskRect = Mathr.ApplyBorder(fullMaskRect, padding * spriteToCanvasScale);
                var adjustedBorder = AdjustBorders(sprite.border * spriteToCanvasScale, fullMaskRect);
                _parameters.maskBorder = LocalMaskRect(adjustedBorder);
                _parameters.maskBorderUV = inner;
            }
            _parameters.texture = sprite.texture;
            _parameters.borderMode = borderMode;
            if (borderMode == BorderMode.Tiled)
                _parameters.tileRepeat = MaskRepeat(sprite, spritePixelsPerUnit, _parameters.maskBorder);
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

        bool ShouldPreserveAspect() {
            if (isBasedOnGraphic) {
                var image = _graphic as Image;
                Assert.IsNotNull(image);
                return image.preserveAspect;
            }
            return false;
        }
        
        Vector4 PreserveSpriteAspectRatio(Vector4 rect, Vector2 spriteSize) {
            var spriteRatio = spriteSize.x / spriteSize.y;
            var rectRatio = (rect.z - rect.x) / (rect.w - rect.y);
            if (spriteRatio > rectRatio) {
                var scale = rectRatio / spriteRatio;
                return new Vector4(rect.x, rect.y * scale, rect.z, rect.w * scale);
            } else {
                var scale = spriteRatio / rectRatio;
                return new Vector4(rect.x * scale, rect.y, rect.z * scale, rect.w);
            }
        }

        void CalculateTextureBased(Texture texture, Rect uvRect) {
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
            _parameters.invertMask = _invertMask;
            _parameters.invertOutsides = _invertOutsides;
        }

        float SpriteToCanvasScale(float spritePixelsPerUnit) {
            var canvasPixelsPerUnit = canvas ? canvas.referencePixelsPerUnit : 100;
            return canvasPixelsPerUnit / spritePixelsPerUnit;
        }

        Matrix4x4 WorldToMask() {
            return maskTransform.worldToLocalMatrix * canvas.rootCanvas.transform.localToWorldMatrix;
        }

        Vector4 LocalMaskRect(Vector4 border) {
            return Mathr.ApplyBorder(Mathr.ToVector(maskTransform.rect), border);
        }

        Vector2 MaskRepeat(Sprite sprite, float spritePixelsPerUnit, Vector4 centralPart) {
            var textureCenter = Mathr.ApplyBorder(Mathr.ToVector(sprite.rect), sprite.border);
            return Mathr.Div(Mathr.Size(centralPart), Mathr.Size(textureCenter) * SpriteToCanvasScale(spritePixelsPerUnit));
        }


        void Set<T>(ref T field, T value) {
            field = value;
            _dirty = true;
        }

        static readonly List<SoftMask> s_masks = new List<SoftMask>();
        static readonly List<SoftMaskable> s_maskables = new List<SoftMaskable>();

        class MaterialReplacerImpl : IMaterialReplacer {
            public int order => 0;

            public Material Replace(Material original) {
                if (original == null || original.HasDefaultUIShader())
                    return Replace(original, Resources.Load<Shader>(DefaultUIShaderReplacement));
                else if (original.HasDefaultETC1UIShader())
                    return Replace(original, Resources.Load<Shader>(DefaultUIETC1ShaderReplacement));
                else if (original.SupportsSoftMask())
                    return new Material(original);
                else
                    return null;
            }

            static string DefaultUIETC1ShaderReplacement {
                get {
                #if UNITY_2020_1_OR_NEWER
                    return "SoftMaskETC1PremultipliedAlpha";
                #else
                    return "SoftMaskETC1";
                #endif
                }
            }

            static string DefaultUIShaderReplacement {
                get {
                #if UNITY_2020_1_OR_NEWER
                    return "SoftMaskPremultipliedAlpha";
                #else
                    return "SoftMask";
                #endif
                }
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

        // Various operations on a Rect represented as Vector4 (xMin, yMin, xMax, yMax).
        static class Mathr {
            public static Vector4 ToVector(Rect r) => new Vector4(r.xMin, r.yMin, r.xMax, r.yMax);
            public static Vector4 Div(Vector4 v, Vector2 s) => new Vector4(v.x / s.x, v.y / s.y, v.z / s.x, v.w / s.y);
            public static Vector2 Div(Vector2 v, Vector2 s) => new Vector2(v.x / s.x, v.y / s.y);
            public static Vector4 Mul(Vector4 v, Vector2 s) => new Vector4(v.x * s.x, v.y * s.y, v.z * s.x, v.w * s.y);
            public static Vector2 Size(Vector4 r) => new Vector2(r.z - r.x, r.w - r.y);

            public static Vector4 ApplyBorder(Vector4 v, Vector4 b) => 
                new Vector4(v.x + b.x, v.y + b.y, v.z - b.z, v.w - b.w);

            static Vector2 Min(Vector4 r) => new Vector2(r.x, r.y);
            static Vector2 Max(Vector4 r) => new Vector2(r.z, r.w);

            public static Vector2 Remap(Vector2 c, Vector4 from, Vector4 to) {
                var fromSize = Max(from) - Min(from);
                var toSize = Max(to) - Min(to);
                return Vector2.Scale(Div((c - Min(from)), fromSize), toSize) + Min(to);
            }

            public static bool Inside(Vector2 v, Vector4 r) => 
                v.x >= r.x && v.y >= r.y && v.x <= r.z && v.y <= r.w;
        }

        struct MaterialParameters {
            public Vector4 maskRect;
            public Vector4 maskBorder;
            public Vector4 maskRectUV;
            public Vector4 maskBorderUV;
            public Vector2 tileRepeat;
            public Color maskChannelWeights;
            public Matrix4x4 worldToMask;
            public Texture texture;
            public BorderMode borderMode;
            public bool invertMask;
            public bool invertOutsides;

            Texture activeTexture => texture ? texture : Texture2D.whiteTexture;

            public enum SampleMaskResult { Success, NonReadable, NonTexture2D }

            public SampleMaskResult SampleMask(Vector2 localPos, out float mask) {
                mask = 0;
                var texture2D = texture as Texture2D;
                if (!texture2D)
                    return SampleMaskResult.NonTexture2D;
                var uv = XY2UV(localPos);
                try {
                    mask = MaskValue(texture2D.GetPixelBilinear(uv.x, uv.y));
                    return SampleMaskResult.Success;
                } catch (UnityException) {
                    return SampleMaskResult.NonReadable;
                }
            }

            public void Apply(Material mat) {
                mat.SetTexture(Ids.SoftMask, activeTexture);
                mat.SetVector(Ids.SoftMask_Rect, maskRect);
                mat.SetVector(Ids.SoftMask_UVRect, maskRectUV);
                mat.SetColor(Ids.SoftMask_ChannelWeights, maskChannelWeights);
                mat.SetMatrix(Ids.SoftMask_WorldToMask, worldToMask);
                mat.SetFloat(Ids.SoftMask_InvertMask, invertMask ? 1 : 0);
                mat.SetFloat(Ids.SoftMask_InvertOutsides, invertOutsides ? 1 : 0);
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
                        Debug.LogAssertion("Unknown BorderMode");
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

            float Frac(float v) => v - Mathf.Floor(v);

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
                public static readonly int SoftMask_InvertMask = Shader.PropertyToID("_SoftMask_InvertMask");
                public static readonly int SoftMask_InvertOutsides = Shader.PropertyToID("_SoftMask_InvertOutsides");
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

            Image image => _softMask.DeduceSourceParameters().image;
            Sprite sprite => _softMask.DeduceSourceParameters().sprite;
            Texture texture => _softMask.DeduceSourceParameters().texture;

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
                if (image && !IsImageTypeSupported(image.type))
                    result |= Errors.UnsupportedImageType;
                return result;
            }

            Errors CheckTexture() {
                var result = Errors.NoError;
                if (_softMask.isUsingRaycastFiltering && texture) {
                    var texture2D = texture as Texture2D;
                    if (!texture2D)
                        result |= Errors.UnreadableRenderTexture;
                    else if (!IsReadable(texture2D))
                        result |= Errors.UnreadableTexture;
                }
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
        }

        struct WarningReporter {
            readonly UnityEngine.Object _owner;
            Texture _lastReadTexture;
            Sprite _lastUsedSprite;
            Sprite _lastUsedImageSprite;
            Image.Type _lastUsedImageType;
        
            public WarningReporter(UnityEngine.Object owner) {
                _owner = owner;
                _lastReadTexture = null;
                _lastUsedSprite = null;
                _lastUsedImageSprite = null;
                _lastUsedImageType = Image.Type.Simple;
            }

            public void TextureRead(Texture texture, MaterialParameters.SampleMaskResult sampleResult) {
                if (_lastReadTexture == texture)
                    return;
                _lastReadTexture = texture;
                switch (sampleResult) {
                    case MaterialParameters.SampleMaskResult.NonReadable:
                        Debug.LogErrorFormat(_owner,
                            "Raycast Threshold greater than 0 can't be used on Soft Mask with texture '{0}' because "
                            + "it's not readable. You can make the texture readable in the Texture Import Settings.",
                            texture.name);
                        break;
                    case MaterialParameters.SampleMaskResult.NonTexture2D:
                        Debug.LogErrorFormat(_owner,
                            "Raycast Threshold greater than 0 can't be used on Soft Mask with texture '{0}' because "
                            + "it's not a Texture2D. Raycast Threshold may be used only with regular 2D textures.",
                            texture.name);
                        break;
                }
            }
                
            public void SpriteUsed(Sprite sprite, Errors errors) {
                if (_lastUsedSprite == sprite)
                    return;
                _lastUsedSprite = sprite;
                if ((errors & Errors.TightPackedSprite) != 0)
                    Debug.LogError("SoftMask doesn't support tight packed sprites", _owner);
                if ((errors & Errors.AlphaSplitSprite) != 0)
                    Debug.LogError("SoftMask doesn't support sprites with an alpha split texture", _owner);
            }

            public void ImageUsed(Image image) {
                if (!image) {
                    _lastUsedImageSprite = null;
                    _lastUsedImageType = Image.Type.Simple;
                    return;
                }
                if (_lastUsedImageSprite == image.sprite && _lastUsedImageType == image.type)
                    return;
                _lastUsedImageSprite = image.sprite;
                _lastUsedImageType = image.type;
                if (!image)
                    return;
                if (IsImageTypeSupported(image.type))
                    return;
                Debug.LogErrorFormat(_owner,
                    "SoftMask doesn't support image type {0}. Image type Simple will be used.",
                    image.type);
            }
        }
    }
}
