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

        [SerializeField] List<CapturedStepState> _steps = new List<CapturedStepState>();
        [SerializeField] string _sceneRelativePath;

        public int count { get { return _steps.Count; } }
        public CapturedStepState this[int index] { get { return _steps[index]; } }

    #if UNITY_EDITOR
        public void Load(string sceneRelativePath) {
            _sceneRelativePath = AppendVersionSpecificFolderIfPresent(sceneRelativePath);
            // Despite _referenceScreens are serialized, we still need to re-load them
            // each start. Otherwise we will be not able to transfer a new reference screen
            // sequence from play mode to edit mode.
            LoadReferenceScreens();
        }

        string AppendVersionSpecificFolderIfPresent(string sceneRelativePath) {
        #if UNITY_2019_1_OR_NEWER // Currently we support only 2019+ specific screens
            var relativeScenePath2019 = Path.Combine(sceneRelativePath, "2019");
            var scenePath2019 = Path.Combine(ReferenceScreensFolder, relativeScenePath2019);
            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(scenePath2019)))
                return relativeScenePath2019;
        #endif
            return sceneRelativePath;
        }
        
        void LoadReferenceScreens() {
            _steps.Clear();
            foreach (var potentialPath in IterateScreenshotPaths()) {
                var screen = AssetDatabase.LoadAssetAtPath<Texture2D>(potentialPath);
                if (!screen)
                    break;
                _steps.Add(new CapturedStepState(screen));
            }
        }

        public void ReplaceBy(List<CapturedStepState> newSteps) {
            DeleteReferenceScreens();
            if (!Directory.Exists(currentSceneReferenceDir))
                Directory.CreateDirectory(currentSceneReferenceDir);
            for (int i = 0; i < newSteps.Count; ++i) {
                var screenshot = newSteps[i].texture;
                var screenshotPath = GetScreenshotPath(i);
                File.WriteAllBytes(screenshotPath, screenshot.EncodeToPNG());
                AssetDatabase.ImportAsset(screenshotPath);
                SetupScreenshotImportSettings(screenshotPath);
                _steps.Add(newSteps[i]);
            }
        }
        
        void DeleteReferenceScreens() {
            foreach (var screenPath in IterateScreenshotPaths())
                if (!AssetDatabase.DeleteAsset(screenPath))
                    break;
            _steps.Clear();
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

        IEnumerable<string> IterateScreenshotPaths() {
            for (int i = 0;; ++i)
                yield return GetScreenshotPath(i);
        }
            
        public void Clear() {
            DeleteReferenceScreens();
        }

        string GetScreenshotPath(int stepIndex) {
            return Path.ChangeExtension(
                Path.Combine(
                    currentSceneReferenceDir,
                    string.Format("{0:D2}", stepIndex)),
                ScreenshotExt);
        }
        
        string currentSceneReferenceDir {
            get { return Path.Combine(ReferenceScreensFolder, _sceneRelativePath); }
        }
    #endif
        
        public void RemoveObsoletes() {
            _steps.RemoveAll(x => !x.texture);
        }
    }
}
