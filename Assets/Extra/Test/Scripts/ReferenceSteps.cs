using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SoftMasking.Tests {
    [Serializable]
    public class ReferenceSteps {
        static readonly string ReferenceScreensFolder = "Assets/Extra/Test/Scenes/ReferenceScreens";
        static readonly string ScreenshotExt = ".png";
        static readonly string LogExt = ".txt";

        [SerializeField] List<CapturedStep> _steps = new List<CapturedStep>();
        [SerializeField] string _sceneRelativeReadPath;
        [SerializeField] string _sceneRelativeWritePath;

        public int count { get { return _steps.Count; } }
        public CapturedStep this[int index] { get { return _steps[index]; } }

    #if UNITY_EDITOR
        public void Load(string sceneRelativePath) {
            _sceneRelativeReadPath = AppendVersionSpecificFolderIfPresent(sceneRelativePath);
            _sceneRelativeWritePath = AppendVersionSpecificFolder(sceneRelativePath);
            // Despite _referenceScreens are serialized, we still need to re-load them
            // each start. Otherwise we will be not able to transfer a new reference screen
            // sequence from play mode to edit mode.
            LoadSteps();
        }

        string AppendVersionSpecificFolderIfPresent(string sceneRelativePath) {
#if UNITY_2019_1_OR_NEWER
    #if UNITY_2020_1_OR_NEWER
        #if UNITY_2021_1_OR_NEWER
            var sceneRelativePath2021 = VersionSpecificScenePathIfPresent(sceneRelativePath, "2021");
            if (sceneRelativePath2021 != null)
                return sceneRelativePath2021;
        #endif
            var sceneRelativePath2020 = VersionSpecificScenePathIfPresent(sceneRelativePath, "2020");
            if (sceneRelativePath2020 != null)
                return sceneRelativePath2020;
    #endif
            var sceneRelativePath2019 = VersionSpecificScenePathIfPresent(sceneRelativePath, "2019");
            if (sceneRelativePath2019 != null)
                return sceneRelativePath2019;
#endif
            return sceneRelativePath;
        }

        static string VersionSpecificScenePathIfPresent(string sceneRelativePath, string unityVersion) {
            var versionSceneRelativePath = VersionSpecificScenePath(sceneRelativePath, unityVersion);
            var versionScenePath = Path.Combine(ReferenceScreensFolder, versionSceneRelativePath);
            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(versionScenePath)))
                return versionSceneRelativePath;
            return null;
        }

        string AppendVersionSpecificFolder(string sceneRelativePath) {
        #if UNITY_2021_1_OR_NEWER
            return VersionSpecificScenePath(sceneRelativePath, "2021");
        #elif UNITY_2020_1_OR_NEWER
            return VersionSpecificScenePath(sceneRelativePath, "2020");
        #elif UNITY_2019_1_OR_NEWER
            return VersionSpecificScenePath(sceneRelativePath, "2019");
        #else
            return sceneRelativePath;
        #endif
        }

        static string VersionSpecificScenePath(string sceneRelativePath, string unityVersion) {
            return Path.Combine(sceneRelativePath, unityVersion);
        }
        
        void LoadSteps() {
            _steps.Clear();
            for (int i = 0;; ++i) {
                var screen = TryLoadAssetForStep<Texture2D>(i, ScreenshotExt);
                if (!screen)
                    break;
                var log = TryLoadAssetForStep<TextAsset>(i, LogExt);
                var logRecords = log ? LogRecords.Parse(log.text) : new LogRecord[0];
                _steps.Add(new CapturedStep(screen, logRecords));
            }
        }

        T TryLoadAssetForStep<T>(int step, string assetExtension) where T : UnityEngine.Object {
            var potentialPath = GetAssetReadPath(step, assetExtension);
            return AssetDatabase.LoadAssetAtPath<T>(potentialPath);
        }

        public void ReplaceBy(List<CapturedStep> newSteps) {
            DeleteReference();
            if (!Directory.Exists(currentSceneReferenceWriteDir))
                Directory.CreateDirectory(currentSceneReferenceWriteDir);
            for (int i = 0; i < newSteps.Count; ++i) {
                var newStep = newSteps[i];
                SaveScreenshot(newStep, GetScreenshotWritePath(i));
                SaveLogIfPresent(newStep, GetLogWritePath(i));
                _steps.Add(newStep);
            }
        }

        void DeleteReference() {
            for (int i = 0;; ++i) {
                AssetDatabase.DeleteAsset(GetLogWritePath(i));
                if (!AssetDatabase.DeleteAsset(GetScreenshotWritePath(i)))
                    break;
            }
            _steps.Clear();
        }
          
        static void SaveScreenshot(CapturedStep step, string screenshotPath) {
            File.WriteAllBytes(screenshotPath, step.texture.EncodeToPNG());
            AssetDatabase.ImportAsset(screenshotPath);
            SetupScreenshotImportSettings(screenshotPath);
        }
        
        void SaveLogIfPresent(CapturedStep newStep, string logPath) {
            if (newStep.hasLog) {
                File.WriteAllText(logPath, LogRecords.Format(newStep.logRecords));
                AssetDatabase.ImportAsset(logPath);
            }
        }
 
        static void SetupScreenshotImportSettings(string screenshotPath) {
            var importer = (TextureImporter)AssetImporter.GetAtPath(screenshotPath);
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        public void Clear() {
            DeleteReference();
        }

        string GetScreenshotWritePath(int stepIndex) {
            return GetAssetWritePath(stepIndex, ScreenshotExt);
        }

        string GetLogWritePath(int stepIndex) {
            return GetAssetWritePath(stepIndex, LogExt);
        }

        string GetAssetReadPath(int stepIndex, string assetExtension) {
            return Path.ChangeExtension(
                Path.Combine(currentSceneReferenceReadDir, string.Format("{0:D2}", stepIndex)),
                assetExtension);
        }
        
        string GetAssetWritePath(int stepIndex, string assetExtension) {
            return Path.ChangeExtension(
                Path.Combine(currentSceneReferenceWriteDir, string.Format("{0:D2}", stepIndex)),
                assetExtension);
        }

        // TODO extract a concept of "read & write path pair"?
        
        string currentSceneReferenceReadDir {
            get { return Path.Combine(ReferenceScreensFolder, _sceneRelativeReadPath); }
        }
        
        string currentSceneReferenceWriteDir {
            get { return Path.Combine(ReferenceScreensFolder, _sceneRelativeWritePath); }
        }
    #endif
        
        public void RemoveObsoletes() {
            _steps.RemoveAll(x => !x.texture);
        }
    }
}
