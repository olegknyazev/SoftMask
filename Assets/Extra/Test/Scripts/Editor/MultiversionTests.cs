using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace SoftMasking.Tests {
    public static class MultiversionTests {
        private static readonly string AutomationProjectsRoot = "Automation";
        private static readonly string Unity2019EditorPath = @"F:\Unity\2019.3.0f5\Editor\Unity.exe";
        private static readonly string TestRunnerScene = "Assets/Extra/Test/Scenes/_RunAllAutomatedTests.unity";

        [MenuItem("Tools/Soft Mask/Run Multiversion Tests")]
        public static void Run() {
            using (new ProgressBarScope()) {
                EditorUtility.DisplayProgressBar("Running multiversion tests", "Cloning project...", 0.25f);
                var automationProjectPath = GetAutomationProjectPath("2019");
                CloneProjectTo(automationProjectPath);
                EditorUtility.DisplayProgressBar("Running multiversion tests", "Running editor...", 0.5f);
                int exitCode = RunClientSideMethodInProject(automationProjectPath);
                Debug.LogFormat("Multiversion testing finished with code {0}", exitCode);
            }
        }

        struct ProgressBarScope : IDisposable {
            public void Dispose() {
                EditorUtility.ClearProgressBar();
            }
        }

        static string GetAutomationProjectPath(string unityVersion) {
            return Path.Combine(projectPath, Path.Combine(AutomationProjectsRoot, unityVersion));
        }

        static string projectPath { get { return Directory.GetCurrentDirectory(); } }

        static void CloneProjectTo(string destinationDir) {
            MakeCreanDirectory(destinationDir);
            CopySubfolder(projectPath, destinationDir, "Assets", excludeDirs: "TextMesh Pro");
            CopySubfolder(projectPath, destinationDir, "ProjectSettings");
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

        static int RunClientSideMethodInProject(string projectPath) {
            using (var editorProcess = Process.Start(Unity2019EditorPath, FormatEditorArguments(projectPath))) {
                editorProcess.WaitForExit();
                return editorProcess.ExitCode;
            }
        }

        static string FormatEditorArguments(string projectPath) {
            return string.Join(" ", new [] { 
                "-projectPath", projectPath,
                "-batchmode",
                "-accept-apiupdate",
                "-executeMethod", "SoftMasking.Tests.MultiversionTests.RemoteSideRun" });
        }

        public static void RemoteSideRun() {
            // Here (and in TextMeshProTestUtils too) probably should be a lesser version
            // (one where TextMesh Pro moved to package manager) but for now we run
            // multiversion tests only for 2019, so go with it.
        #if UNITY_2019_1_OR_NEWER
            Debug.Log("Importing TMPro essentials");
            TextMeshProTestUtils.ImportProjectResources();
            Debug.Log("Converting TMPro GUIDs");
            TextMeshProTestUtils.ConvertProjectGUIDs();
        #endif
        #if UNITY_2019_1_OR_NEWER
            Debug.Log("Disabling shader compilation");
            EditorSettings.asyncShaderCompilation = false;
        #endif
            Debug.Log("Updating TextMesh Pro intergration");
            SoftMasking.TextMeshPro.Editor.ShaderGenerator.UpdateShaders();
            Debug.Log("Opening test runner scene");
            var scene = EditorSceneManager.OpenScene(TestRunnerScene);
            Debug.Log("Starting play mode");
            var runner = FindTestRunner(scene);
            runner.exitOnFinish = true;
            EditorApplication.isPlaying = true;
        }

        static AutomatedTestsRunner FindTestRunner(Scene scene) {
            return scene.GetRootGameObjects()
                .Select(x => x.GetComponent<AutomatedTestsRunner>())
                .Where(x => x != null)
                .First();
        }
    }
}