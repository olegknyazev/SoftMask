# Soft Mask

Soft Mask is a package for Unity 3D that implements a smooth masking for UI (UGUI). It works almost like standard
Unity's mask but supports alpha, which enables gradual and semi-transparent masks.

The key feature of Soft Mask is the ease of use. You don't need to be a programmer to use it—just drop the Soft Mask
component on a UI object as you do with the standard Unity Mask and you go.

The package have been sold via Asset Store for several years, but in late 2022 it was open-sourced
under [MIT license with Commons Clause](https://github.com/olegknyazev/SoftMask/blob/main/LICENSE.md). In short, this
license means that you're free to use this tool in your games but you don't have the rights to resell the Soft Mask
itself.

To better understand what the Soft Mask is, [see the online demo](https://olegknyazev.itch.io/softmask).

## Getting Started

The easiest way to install Soft Mask is via Package Manager window by
using [GitHub URL](https://docs.unity3d.com/Manual/upm-git.html). Press *Add* button in the Package Manager window and
enter the following URL:

```
https://github.com/olegknyazev/SoftMask.git?path=/Packages/com.olegknyazev.softmask#1.7.0
```

Pay attention to the version that's encoded in this URL.

Alternatively, you can get the package directly from `Packages/com.olegknyazev.softmask` subfolder. A pre-built artifact
is not provided at the moment.

### Useful links

- [Documentation](https://github.com/olegknyazev/SoftMask/blob/main/Packages/com.olegknyazev.softmask/Documentation%7E/Documentation.pdf)
- [Changelog](https://github.com/olegknyazev/SoftMask/blob/main/Packages/com.olegknyazev.softmask/CHANGELOG.md)
- [Support Thread](https://forum.unity.com/threads/soft-mask-a-replacement-of-unity-ui-mask-with-alpha-support.454707) — This thread was one of the primary support lines while the package was paid. You still can find some useful
  information there or post a bug, but GitHub is a preferred place to reporting bugs.

## Development

The remaining of the document is aimed at those who is interested in modifying the package.

### Project Structure

At the root of the repository we have a regular Unity project which contains the package itself under
`Packages/com.olegknyazev.softmask` directory as well as some additional assets and scripts for development.

### Automated Tests

Soft Mask has a set of automated tests which work by comparing render results in various test scenes against
pre-recorded screenshots. In general, it's a bad idea to use rendering results in testing because they may depend on
specific software (version of OS, Unity, selected render system) or even hardware. But in the case of Soft Mask, which
highly depend on shaders, I don't see a good alternative, so I decided to use this approach.

All the screenshot-comparing test were recorded on MacOS 15.1 and Unity 2020 with Metal renderer and they may not be
compatible with screenshots taken on different setup.

The tests use [perceptualdiff](https://github.com/myint/perceptualdiff) utility, so you need to have it installed in
order to run the tests.

To run a set of automated tests, perform the following:

1. Import TextMesh Pro package, essential resources and additional examples. The additional resources are used in some
   TMPro-related tests scenes, so you have to have them in project in order for these tests to work.
2. Update TextMesh Pro integration.
3. Open scene `Assets/Extra/Test/Scenes/_RunAllAutomationTests.unity`.
4. \[Optional\] Select TestsRunner object and modify properties you need for this specific run.
5. Run the scene in Play Mode.
6. Wait for automation tests to end. Do not remove focus from the Unity Editor windows during the testing.

Besides of these screenshot-comparing tests we also have several classic editor-mode tests for the functionality that
could be tested this way.