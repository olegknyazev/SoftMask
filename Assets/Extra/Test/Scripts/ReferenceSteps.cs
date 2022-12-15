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
            _sceneRelativeReadPath = sceneRelativePath;
            _sceneRelativeWritePath = sceneRelativePath;
            // Despite _referenceScreens are serialized, we still need to re-load them
            // each start. Otherwise we will be not able to transfer a new reference screen
            // sequence from play mode to edit mode.
            LoadSteps();
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
