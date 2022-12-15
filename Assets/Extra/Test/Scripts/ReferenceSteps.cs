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
        [SerializeField] string _sceneRelativePath;

        public int count => _steps.Count;
        public CapturedStep this[int index] => _steps[index];

#if UNITY_EDITOR
        public void Load(string sceneRelativePath) {
            _sceneRelativePath = sceneRelativePath;
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
            var potentialPath = GetAssetPath(step, assetExtension);
            return AssetDatabase.LoadAssetAtPath<T>(potentialPath);
        }

        public void ReplaceBy(List<CapturedStep> newSteps) {
            DeleteReference();
            if (!Directory.Exists(currentSceneReferenceDir))
                Directory.CreateDirectory(currentSceneReferenceDir);
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

        string GetScreenshotWritePath(int stepIndex) => GetAssetPath(stepIndex, ScreenshotExt);

        string GetLogWritePath(int stepIndex) => GetAssetPath(stepIndex, LogExt);

        string GetAssetPath(int stepIndex, string assetExtension) =>
            Path.ChangeExtension(
                Path.Combine(currentSceneReferenceDir, $"{stepIndex:D2}"),
                assetExtension);

        string currentSceneReferenceDir => Path.Combine(ReferenceScreensFolder, _sceneRelativePath);
#endif
        
        public void RemoveObsoletes() {
            _steps.RemoveAll(x => !x.texture);
        }
    }
}
