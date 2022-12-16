# Soft Mask

Soft Mask is a package for Unity 3D that implements an alpha-tolerant UI mask. It works almost like standard Unity's
mask but support alpha, which enables making gradual and semi-transparent masks. The key feature of Soft Mask is the
ease of use, you don't need to be a programmer to use it—just drop the Soft Mask component on the UI element as you do
with standard Unity Mask component and you ready to go.

The package have been selling in Asset Store for several years, but in late 2022 it was open-sourced under MIT + Common
Clause license. In short, this license means that you're free to use this tool in your games but you don't have the
rights to resell the Soft Mask itself.

## Getting Started

The easiest way to use Soft Mask is by using GitHub link it via Package Manager.

**TODO**

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

These tests use [perceptualdiff](https://github.com/myint/perceptualdiff) utility, so you need to have it installed in
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