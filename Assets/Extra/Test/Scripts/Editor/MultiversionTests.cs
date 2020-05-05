using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class MultiversionTests {
    private static readonly string AutomationProjectsRoot = "Automation";
    private static readonly string Unity2019EditorPath = @"F:\Unity\2019.3.0f5\Editor\Unity.exe";

    [MenuItem("Tools/Soft Mask/Run Multiversion Tests")]
    public static void Run() {
        EditorUtility.DisplayProgressBar("Running multiversion tests", "Cloning project", 0.5f);
        try {
            var projectPath = Directory.GetCurrentDirectory();
            var automationProjectPath = Path.Combine(projectPath, Path.Combine(AutomationProjectsRoot, "2019"));
            MakeCreanDirectory(automationProjectPath);
            CopySubfolder(projectPath, automationProjectPath, "Assets");
            CopySubfolder(projectPath, automationProjectPath, "ProjectSettings");
        } finally {
            EditorUtility.ClearProgressBar();
        }
    }

    static void MakeCreanDirectory(string automationProjectDir) {
        if (Directory.Exists(automationProjectDir))
            Directory.Delete(automationProjectDir, true);
        Directory.CreateDirectory(automationProjectDir);
    }


    static void CopySubfolder(string from, string to, string subFolder) {
        CopyFilesRecursively(
            new DirectoryInfo(Path.Combine(from, subFolder)),
            new DirectoryInfo(Path.Combine(to, subFolder)));
    }

    static void CopyFilesRecursively(DirectoryInfo from, DirectoryInfo to) {
        if (!to.Exists)
            to.Create();
        foreach (var dir in from.GetDirectories())
            CopyFilesRecursively(dir, to.CreateSubdirectory(dir.Name));
        foreach (var file in from.GetFiles())
            file.CopyTo(Path.Combine(to.FullName, file.Name));
    }
}
