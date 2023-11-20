# Soft Mask

Soft Mask is a package for Unity 3D that implements smooth masking for UI (UGUI). It works almost like the standard
Unity's mask, but supports alpha, which enables gradual and semi-transparent masks.

The key feature of Soft Mask is its ease of use. You don't need to be a programmer to use it—just drop the Soft Mask
component on a UI object as you do with the standard Unity Mask and here you go.

The package had been sold on Asset Store for several years, but in late 2022 it was open-sourced
under the [MIT license with Commons Clause](https://github.com/olegknyazev/SoftMask/blob/main/LICENSE.md). In short, this
license means that you're free to use this tool in your games, but you don't have the rights to resell the Soft Mask
itself.

To better understand what the Soft Mask is, [check out the online demo](https://olegknyazev.itch.io/softmask).

## Getting Started

The easiest way to install Soft Mask is via the Package Manager window by
using [GitHub URL](https://docs.unity3d.com/Manual/upm-git.html). Press the *Add* button in the Package Manager window and
enter the following URL:

```
https://github.com/olegknyazev/SoftMask.git?path=/Packages/com.olegknyazev.softmask#1.7.0
```

Pay attention to the version that's encoded within this URL.

Alternatively, you can get the package directly from the `Packages/com.olegknyazev.softmask` subfolder. A pre-built artifact
is not provided at the moment.

### Useful links

- [Documentation](https://github.com/olegknyazev/SoftMask/blob/main/Packages/com.olegknyazev.softmask/Documentation%7E/Documentation.pdf)
- [Changelog](https://github.com/olegknyazev/SoftMask/blob/main/Packages/com.olegknyazev.softmask/CHANGELOG.md)
- [Support Thread](https://forum.unity.com/threads/soft-mask-a-replacement-of-unity-ui-mask-with-alpha-support.454707) —
  This thread was one of the primary support lines while the package was paid. You still can find some useful
  information there or post a bug, but GitHub is a preferred place to reporting bugs.

## Development

The remaining of the document is aimed at those who is interested in modifying the package.

### Project Structure

At the root of the repository we have a regular Unity project which contains the package itself under
`Packages/com.olegknyazev.softmask` directory as well as some additional assets and scripts for development.

### Automated Tests

Soft Mask has a set of automated tests that work by comparing render results in various test scenes against
the pre-recorded screenshots. In general, it's a bad idea to use rendering results in testing because they may depend on
specific software (version of OS, Unity, selected render system) or hardware. But in the case of Soft Mask, which
highly depends on shaders, I don't see a good alternative, so I decided to use this approach.

All the screenshot-comparing tests were recorded on MacOS 15.1 and Unity 2020 with the Metal renderer, and they may not be
compatible with screenshots taken on a different setup.

The tests use the [perceptualdiff](https://github.com/myint/perceptualdiff) utility, so you need to have it installed in
order to run the tests.

To run the automated tests suite, perform the following:

1. Import the TextMesh Pro package, essential resources, and additional examples. The additional resources are used in some
   TMPro-related test scenes, so you have to have them in the project in order for these tests to work.
2. Update TextMesh Pro integration.
3. Open scene `Assets/Extra/Test/Scenes/_RunAllAutomationTests.unity`.
4. \[Optional\] Select the TestsRunner object and modify properties as need for this specific run.
5. Run the scene in Play Mode.
6. Wait for automation tests to end. Do not remove focus from the Unity Editor windows during the testing.

Besides these screenshot-comparing tests, we also have several classic editor-mode tests for the functionality that
could be tested this way.

### Documentation

The documentation for Soft Mask is written in Google Documents and exported as a PDF. The source document for 1.7.0
is [available here](https://docs.google.com/document/d/1YBWxbaGjm2t1u6AVN0iMI-zLmpA4954hhL5S3BxZGH4/edit?usp=sharing).