using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class MultiversionTests {
    private static readonly string AutomationProjectsRoot = "Automation";
    private static readonly string Unity2019EditorPath = @"F:\Unity\2019.3.0f5\Editor\Unity.exe";

    [MenuItem("Tools/Soft Mask/Run Multiversion Tests")]
    public static void Run() {
        EditorUtility.DisplayProgressBar("Running multiversion tests", "Cloning project...", 0.25f);
        try {
            var projectPath = Directory.GetCurrentDirectory();
            var automationProjectPath = Path.Combine(projectPath, Path.Combine(AutomationProjectsRoot, "2019"));
            MakeCreanDirectory(automationProjectPath);
            CopySubfolder(projectPath, automationProjectPath, "Assets", excludeDirs: "TextMesh Pro");
            CopySubfolder(projectPath, automationProjectPath, "ProjectSettings");
            
            EditorUtility.DisplayProgressBar("Running multiversion tests", "Running editor...", 0.5f);
            var editorArguments = string.Format("-projectPath {0} -batchmode -accept-apiupdate -quit -executeMethod MultiversionTests.ClientSideRun", automationProjectPath);
            var editor = Process.Start(Unity2019EditorPath, editorArguments);
            editor.WaitForExit();

        } finally {
            EditorUtility.ClearProgressBar();
        }
    }

    static void MakeCreanDirectory(string automationProjectDir) {
        if (Directory.Exists(automationProjectDir))
            Directory.Delete(automationProjectDir, true);
        Directory.CreateDirectory(automationProjectDir);
    }

    static void CopySubfolder(string from, string to, string subFolder, params string[] excludeDirs) {
        CopyFilesRecursively(
            new DirectoryInfo(Path.Combine(from, subFolder)),
            new DirectoryInfo(Path.Combine(to, subFolder)),
            excludeDirs);
    }

    static void CopyFilesRecursively(DirectoryInfo from, DirectoryInfo to, params string[] excludeDirs) {
        if (!to.Exists)
            to.Create();
        foreach (var dir in from.GetDirectories())
            if (!excludeDirs.Any(exc => dir.FullName.Contains(exc)))
                CopyFilesRecursively(dir, to.CreateSubdirectory(dir.Name));
        foreach (var file in from.GetFiles())
            file.CopyTo(Path.Combine(to.FullName, file.Name));
    }

    public static void ClientSideRun() {
    #if UNITY_2019_1_OR_NEWER
        UnityEngine.Debug.Log("Import TMPro essentials");
        TMPro.TMP_PackageUtilities.ImportProjectResourcesMenu();
        UnityEngine.Debug.Log("Converting TMPro GUIDs");
        TMPro.TMP_PackageUtilities.ConvertProjectGUIDsMenu();
    #endif
    }
}
