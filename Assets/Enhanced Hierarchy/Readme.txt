Thanks for using Enhanced Hierarchy :)

DESCRIPTION:
Enhanced Hierarchy for Unity is an editor extension that allows you to easily manipulate your Game Objects directly in the hierarchy, it adds a bunch of toggles and buttons in the hierarchy and some information icons.
Are you tired of selecting and deselecting Game Objects just to enable and disable them? Or to change their static flags? Or to change their layer? Then, this extension is made for you, you can do these tedious tasks faster, just clicking on the hierarchy.
Don't you know where those strange errors in your console are coming from? Well, the extension will place an icon next to the game object that is throwing the errors.
Does your partner keeps changing the objects that you don't want him to change? Lock the object and prevent him (or even yourself) from messing with your things.
Do you want a draggable selection on the hierarchy? With this extension you have it, just drag over the items using your right mouse button.
And it is totally customizable, you can enable and disable anything you want.

Source code included

Any suggestion, bug report or question feel free to contact me:
samuelschultze@gmail.com

KNOWN ISSUES:
- Clicking on "Add Tag" or "Add Layer" on certain version of Unity will make it crash, be aware to not click on these buttons (this happens in the inspector too, so it's not a problem with Enhanced Hierarchy, but rather with Unity Editor).

FAQ:
Q: How to use the Enhanced Selection?
A: Just drag over the hierarchy items using your right mouse button.

Q: How to open the settings window?
A: The settings are along the unity preferences, go to Edit/Preferences, you'll see a Hierarchy tab.

Q: The extension disabled itself, what do I do?
A: It might have some errors, and to prevent spamming your console with error, it will disable, or maybe you pressed Ctrl + H, the shortcut for disabling and enabling it, in both cases press Ctrl + H to enable it again. (If it keeps disabling itself all the time send me the errors in your console and I'll fix it)

Q: I see this "OptionalModule.zip" thing in the extension folder, what is it?
A: It's a compiled assembly of the plugin, if you want the extension to be available in all projects without the need to import the package, there is a InstallMe.txt with further explanation and installation guide, you can delete it if you're not going to install.

Q: Debug.Log, Debug.LogWarning and Debug.LogError don't show up on the hierarchy with some scripts.
A: The script need to pass the object in the parameters of the Debug.Log, like this: Debug.Log("Something", this), or use the "print" method.

Q: My game fail to compile if I use the extension, how to fix it?
A: The extension must be placed inside a folder called Editor because it uses the UnityEditor API.

CHANGELOG:
Version 2.2.2:
- Fixed bug that wouldn't allow the user to remove or add an icon on settings.

Version 2.2.1:
- Fixed optional module not saving settings.

Version 2.2.0: (I apologize for all the users that send feedbacks and reports for the delay of this update, I was a little busy I couldn't find time to publish it)
- Unity focus fix while using "Ask" on change modes.
- Ctrl and Cmd modifiers to children change mode.
- New preferences for the row separators.
- Added new per layer row color.
- Fixed NullReferenceException while attempting to lock a game object with a missing mono behavior.
- Added an option to unlock all objects in the scene.
- Change multiple object icons at once.
- Apply multiple prefabs at once.
- Fixed label icon size if option "Left icon at leftmost" was disabled.
- Locking/Unlocking performance improved.
- Fixed selection inconsistency if not using "Allow locked selection" in hierarchy.
- Improved settings window.
- Improved the numeric child foldout.
- Fixed Unity 2017 logs in MacOS.
- Selection will now scroll when the mouse is beyond hierarchy boundaries.
- Icons are now easier to implement, just inherit it from RightIcon and it will work.
- Code improvements.
- Better undo performance.
- Monobehaviour icon, appears when the object contains any mono behaviour script.
- Sound icon, appears when the object is playing any audio clip.
- Better undo performance.
- Better exception management.

Version 2.1.4:
- Prevent selection of locked objects in the scene view.
- Unity 4.7 support.

Version 2.1.3:
- Fixed a bug related to the previous version.

Version 2.1.2:
- Unity 2017 support.

Version 2.1.1:
- Fixed warning not showing up for missing mono behaviors.
- Fixed warnings, log and messages icons color when using linear color space.
- Fixed bug of the layer button not appearing in the settings.
- Added "child ask mode" for tag, layer and lock buttons.
- Fixed a bug where the separators wouldn't draw immediately after an assembly reload.
- Compatibility with "Favorite Tab[s]" (http://u3d.as/3hG).
- New settings to change all the selected objects, not only the object owner of the button or toggle.
- Added the possibility to add one icon to the left side, it's configurable in the preferences.
- New child expand toggle that shows how many children the object have.
- Smaller mini label for narrow hierarchies.

Version 2.1.0:
- Fixed bug that wouldn't let the user select models if the "Allow locked selection" box was disabled.
- Code improvements.
- Split the extension into multiple files because it was getting harder to read the code as the extension grow up.
- Removed vertical separators option due to performance reasons.
- Added Enable/Disable menu item under Edit/Enhanced Hierarchy.
- Enable/Disable shortcut will now work even if the hierarchy window is not focused.
- Fixed color of the selection behind the trailing.
- Improved line separator, it's a little clearer now.
- Included zip file containing the module, instruction of how to install it are in the InstallMe.txt.

Version 2.0.2:
- Unity 5.6 support.
- Bug fixes.
- New feature: prevent selecting locked objects.

Version 2.0.1:
- Improved performance of warnings, now the hierarchy can handle thousands of logs without lagging.
- Added alpha change for disabled toggles.
- Readded the ability to change tag and layer by clicking on the mini label, this was removed in the previous version.
- Added a feature to save prefabs, appears when apply prefab button is clicked and the object is not a prefab yet.
- Added trailing when the name is bigger than the view area.
- Minor bug fixes.

Version 2.0.0:
- Big performance improvements.
- Visual improvements, new icons and styles.
- New feature: Enhanced selection, allows you to select GameObjects by dragging over them with right mouse button.
- New feature: Vertical lines separating the buttons, like the ones in blender's outliner.
- New feature: When changing static flags of an object it asks if you want to change children flags as well, like the inspector (can be disabled).
- Support for both tag and layer dropdowns at the same time.
- New preferences interface, now it's easier to understand, enable and disable features.
- Coding improvements.
- Minor bug fixes.

Version 1.3.1:
- Added warning icons for game objects used as context in logs.
- Prefab apply improvements.

Version 1.3.0:
- Added a shortcut to enable and disable all features.
- Now you can apply prefab changes from hierarchy.
- Added an offset preference to move the buttons to the left if you're using another extension that uses hierarchy.
- Tooltips in all controls.
- Small code improvements.

Version 1.2.0:
- Fixed a NullReferenceException in Unity 5.3.
- Fixed game object hide flags when locking.

Version 1.1.0:
- Select game object icon directly on hierarchy.
- Change drawing order of hierarchy contents.
- New GUIStyles.
- Color sorting.
- Several code improvements.
- Support for Unity 5.0 or higher.