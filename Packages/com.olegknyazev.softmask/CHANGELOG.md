# Changelog

## 1.7.0

### Breaking Changes

- The minimum Unity version the Soft Mask is compatible with is now 2020.1.

### Changes

- Soft Mask is open-sourced and may be used free of charge under
  the [MIT license with Commons Clause](https://github.com/olegknyazev/SoftMask/blob/5bc9dde11916d3db1a85a2e7697219dd9de505eb/LICENSE.md).
- Soft Mask is now distributed in a package form and may be installed via Unity's Package Manager.

### Fixes

- Fixed
  a [bug](https://forum.unity.com/threads/soft-mask-a-replacement-of-unity-ui-mask-with-alpha-support.454707/page-9#post-7869508)
  caused TextMesh Pro SubMeshes to be not masked on the first frame after activation.
- Fixed a bug due to which Soft Mask was not cooperating properly with standard Rect Mask in Unity 2020 lineup.
- Fixed
  a [bug](https://forum.unity.com/threads/soft-mask-a-replacement-of-unity-ui-mask-with-alpha-support.454707/page-9#post-8601618)
  caused tiled masks to be displayed incorrectly inside canvases having a non-standard *Reference Pixels per Unit* value.

## 1.6.3

### Fixes

- Fixed a bug because of which pre-multiplied alpha shaders were never used. Now they are automatically used in Unity
  2020 and higher.
- Fixed a bug that might cause “Destroying object multiple times” error in Editor.
- Fixed a bug preventing invisible SoftMaskable scripts from removal on inactive GameObjects.

## 1.6.2

### Improvements

- Improved compatibility with Unity 2021.2 UI shaders.

### Fixes

- Fixed a bug due to which *Preserve Aspect Ratio* worked incorrectly for sprites with *Mesh Type* set to *Tight*.

## 1.6.1

### Breaking Changes

- The minimum Unity version the Soft Mask is compatible with is now 2018.4.
- When upgrading from 1.5 it’s highly recommended to remove Soft Mask entirely from the project and then
  import it from scratch.

### Improvements

- Added support for Bitmap-Mobile-Custom-Atlas TextMesh Pro’s shader.

### Fixes

- Fixed
  a [bug](https://forum.unity.com/threads/soft-mask-a-replacement-of-unity-ui-mask-with-alpha-support.454707/page-8#post-7163227)
  in TextMesh Pro integration caused Bitmap and Sprite shaders to appear black on the borders of a mask.
- Fixed
  a [bug](https://forum.unity.com/threads/soft-mask-a-replacement-of-unity-ui-mask-with-alpha-support.454707/page-8#post-7214380)
  caused TextMesh Pro integration-related scripts to be stripped out from a build when Managed Strip Level
  was set to a level higher than Low.
- Fixed a wrong `#include` path in an example shader that is used in example 04-CustomShader.
- Fixed a compilation warning on an unused variable.

## 1.6

### Breaking Changes

- The minimum Unity version the Soft Mask is compatible with is now 2017.1.
- When upgrading from 1.5 it’s highly recommended to remove Soft Mask entirely from the project and then import it from
  scratch.
- Properties `SoftMask.defaultShader` and `SoftMask.defaultETC1Shader` are removed. Now Soft Mask uses Resources to
  dynamically load shaders it needs. You don’t have to specify shaders when instantiating Soft Mask objects from code
  any more, so, any assignments to these properties that you’ve done before may be safely removed and any reads of there
  properties may be replaced by `Resources.Load<Shader>(“SoftMask”)` and `Resources.Load<Shader>(“SoftMaskETC1”)`
  respectively.

### Improvements

- Added support for *Preserve Aspect Ratio* option of Image component that’s available in Simple mode.
- Added support for the *Maskable* option of Maskable Graphic component that was added in Unity 2020.
- All the Soft Mask code moved into a separate assembly.
- Improved compatibility with Unity 2021 UI shaders.

### Fixes

- Fixed
  a [bug](https://forum.unity.com/threads/soft-mask-a-replacement-of-unity-ui-mask-with-alpha-support.454707/page-8#post-6862304)
  caused errors to appear in Console when an object got out of Soft Mask effect in a physics callback.
- Fixed
  a [bug](https://forum.unity.com/threads/soft-mask-a-replacement-of-unity-ui-mask-with-alpha-support.454707/page-7#post-6345471)
  caused multifont TextMesh Pro texts to be not masked under a Soft Mask.

## 1.5

### Improvements

- Added support for Render Texture. Now it’s possible to assign a Render Texture to a Soft Mask via either Inspector or
  code.
- Added a new example scene 06-RenderTexture that shows how to use Soft Mask with Render Textures.
- Added support for *Pixels per Unit Multiplier* property of *Sliced* and *Tiled* Images. The same property is also
  available directly on Soft Mask in *Sprite* mode.
- Added a menu command for easy conversion of a standard Mask into a Soft Mask.
- Restructured TextMesh Pro-related examples. Now the root folder contains examples for the most
  relevant—package—version of TMPro.
- Added support for the *Softness* parameter of Rect Mask in Unity 2020.1 or higher.

### Fixes

- Fixed a bug due to which the Update TextMesh Pro Integration menu might work incorrectly in
  [some cases](https://forum.unity.com/threads/soft-mask-a-replacement-of-unity-ui-mask-with-alpha-support.454707/page-5#post-5256251).
  It doesn’t rely on textual shaders names anymore.

## 1.4

### Breaking Changes

- The minimum Unity version the Soft Mask is compatible with is now 5.6.

### Improvements

- Added the *Invert Mask* and *Invert Outsides* options which allow to separately invert the inner and outer areas of
  the mask.
- Added a new example featuring mask inversion.
- Added tooltips for the Inspector UI.

### Fixes

- Fixed a bug with incorrect display of a mask when a scaled sprite atlas was used.
- Fixed an incorrect calculation of tile repeat count in tiled mode that might occur for some sprites.
- Fixed a bug with inconsistent Inspector state after reverting the *Channel Weights* property to prefab defaults.

## 1.3.1

### Improvements

- Improved the error message that is shown in the case when an unreadable texture is used with the *Raycast Threshold*
  value greater than zero. Also, this error is now displayed in the Inspector window at edit time, not only in the
  Console window at runtime.
- Removed an unnecessary garbage allocation that might be happening because of the `GetComponent<>()` call.

### Fixes

- Fixed
  a [bug](https://forum.unity.com/threads/soft-mask-a-replacement-of-unity-ui-mask-with-alpha-support.454707/page-4#post-3700480)
  caused the mask to lag behind when being moved. In particular, it was causing the lagging of the mask during the
  automatic movement inside a Scroll Rect.
- Fixed a compilation error when targeting .NET Standard 2.0.

## 1.3

### Improvements

- Integration with TextMesh Pro, which was previously implemented in a separate integration package, now included in the
  main package. It corresponds to Unity’s decision to include TextMesh Pro in standard Unity installation.
- Removed unnecessary
  [garbage allocation](https://forum.unity.com/threads/soft-mask-a-replacement-of-unity-ui-mask-with-alpha-support.454707/page-4#post-3568825)
  which was taking place when non-Graphic mask source was used.

### Fixes

- Fixed
  an [assertion failure](https://forum.unity.com/threads/soft-mask-a-replacement-of-unity-ui-mask-with-alpha-support.454707/page-3#post-3553450)
  which was introduced in the previous version.
- Fixed a bug caused the masked UI to entirely disappear sometimes in Editor (after scene saving, after code reload,
  etc.).
- Fixed a bug caused *Update TextMesh Pro Integration* to not work on some Unity versions in the case when the path to
  the project contained “Soft Mask” text.

## 1.2.4

### Fixes

- Fixed
  a [bug](https://forum.unity.com/threads/soft-mask-a-replacement-of-unity-ui-mask-with-alpha-support.454707/page-3#post-3524261)
  that led the mask dimensions to not update when a separate mask resizes.

## 1.2.3

### Improvements

- Improved compatibility of Soft Mask shaders with 2018 lineup. Now they support UV transformations as standard 2018’s
  UI shader does.
- Removed a use of an obsolete name which caused Unity 2017.3 and later versions to run script upgrade on Soft Mask
  import.

### Fixes

- Fixed
  a [bug](https://forum.unity.com/threads/soft-mask-a-replacement-of-unity-ui-mask-with-alpha-support.454707/page-3#post-3435725)
  caused Soft Mask to not work on some AOT platforms.
- Fixed
  a [bug](https://forum.unity.com/threads/soft-mask-a-replacement-of-unity-ui-mask-with-alpha-support.454707/page-3#post-3514114)
  caused masked UI elements to flicker sometimes.

## 1.2.2

### Improvements

- Improved compatibility of Soft Mask shaders with 2017.2. Now they perform rectangular clipping only when
  `UNITY_UI_CLIP_RECT` is defined.

### Fixes

- Fixed a bug causing Soft Mask to not work when the project contained dynamic assemblies with an unfinished process of
  type building i.e. when `TypeBuilder.CreateType()` hasn’t been called.

## 1.2.1

### Fixes

- Fixed a bug causing Soft Mask to not work when the project contained dynamic assemblies.

## 1.2

### Improvements

- Added support of TextMesh Pro in a separate integration package—Soft Mask for TextMesh Pro. The package can be found
  in Asset Store or in the support thread.
- Added an ability to inject custom logic into the process of shader replacement. This feature is used by the new
  TextMesh Pro integration package and also can be used to better integrate Soft Mask with your own UI shaders.
- Added a tutorial on how to add support of Soft Mask into your own shader.

### Fixes

- Removed anti-aliasing from rectangular clipping. Images whose content is close to the texture borders are displayed
  correctly now. If you used Soft Mask without texture set and want to achieve the previous anti-aliased look, you can
  use a special texture with a one-pixel gap on the borders.
- Removed a visual artifact that sometimes could appear on the edge of the masked image in *Tiled* sprite mode.

## 1.1.1

### Improvements

- Improved usability of Soft Mask in Unity versions 5.6 or later. Now Unity doesn’t upgrade Soft Mask shaders and
  warnings aren’t popped up during import.
- Nested masks aren’t disabled automatically now. Instead, each child is masked by the nearest mask only, which gives
  more predictable behavior. See Nested Masks section in the documentation.

### Fixes

- Fixed the way Soft Mask interacts with nested canvases with *Sorting Override* flag enabled. It doesn’t mask those
  canvases anymore which corresponds to the way standard Mask does work.

## 1.1

### Improvements

- Added the *Separate Mask* parameter that allows to separate mask from the masked elements. The sample 04-CustomShaders
  has been reworked to show this feature in use.
- Optimized real-time performance, especially on huge hierarchies.
- Improved mapping of *Sliced* and *Tiled* masks in the case when the central part is collapsed.
- Reworked samples: they now demonstrate more features of Soft Mask and also look better.

### Fixes

- Fixed a bug causing Soft Mask to work incorrectly on nested canvases. The partial consequence of this was the
  inability to use Soft Mask in a standard Dropdown control which uses a nested canvas for the drop-down list. Now it is
  possible.
- Fixed a bug preventing Soft Mask from updating after moving it from one canvas to another.
- Fixed a bug preventing rendering of the child elements when a mask used a sprite with one of its borders set to zero
  in *Sliced* or *Tiled* mode.