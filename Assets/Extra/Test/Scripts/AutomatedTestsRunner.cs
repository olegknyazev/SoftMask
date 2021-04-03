using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Assertions;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SoftMasking.Tests {
    public class AutomatedTestsRunner : MonoBehaviour {
        public string testScenesPath = "Assets/Extra/Test/Scenes";
        public string testScenesNamePattern = "^Test.+$";
        public string[] standaloneSkipScenes = new string[0];
        public bool speedRun = false;
        public bool stopOnFirstFail = true;
        public bool exitOnFinish = false;
        public bool replaceReferenceOnFail = false;
        
        public AutomatedTestResults testResults { get; private set; }
        public bool isFinished { get { return testResults != null; } }

        public event Action<AutomatedTestsRunner> changed;

        class Ref<T> { public T value; }

        public IEnumerator Start() {
            ResolutionUtility.SetTestResolution();
            var testResultList = new List<AutomatedTestResult>();
            try {
                foreach (var sceneKey in GetTestSceneKeys()) {
                    var testResult = new Ref<AutomatedTestResult>();
                    LoadScene(sceneKey);
                    yield return TestAndUnloadScene(testResult);
                    testResultList.Add(testResult.value);
                    if (testResult.value.isFail)
                        if (stopOnFirstFail)
                            break;
                }
            } finally {
                ResolutionUtility.RevertTestResolution();
                testResults = new AutomatedTestResults(testResultList);
                ReportToLog(testResults);
                ExitIfRequested();
            }
        }

    #if UNITY_EDITOR
        public void BuildTestPlayer(BuildOptions additionalOptions = BuildOptions.None) {
            var currentScene = gameObject.scene.path;
            var testScenes = 
                GetTestSceneKeys()
                    .Where(x => !standaloneSkipScenes.Any(pat => Regex.IsMatch(x, pat)));
            BuildPipeline.BuildPlayer(
                new [] { currentScene }.Concat(testScenes).ToArray(),
                "Build/AutomatedTests/SoftMaskTests.exe",
                BuildTarget.StandaloneWindows,
                BuildOptions.AllowDebugging | BuildOptions.ForceEnableAssertions | additionalOptions);
        }

        IEnumerable<string> GetTestSceneKeys() {
            return
                AssetDatabase
                    .FindAssets("t: Scene", new [] { testScenesPath })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(path => !string.IsNullOrEmpty(path))
                    .Where(IsTestScene);
        }

        bool IsTestScene(string scenePath) {
            Assert.IsFalse(string.IsNullOrEmpty(scenePath));
            var sceneName = Path.GetFileNameWithoutExtension(scenePath);
            return testScenesNamePatternRegex.IsMatch(sceneName)
                && MatchesUnityVersion(sceneName);
        }
        
        Regex _testScenesNamePatternRegex;
        Regex testScenesNamePatternRegex {
            get {
                if (_testScenesNamePatternRegex == null)
                    _testScenesNamePatternRegex = new Regex(testScenesNamePattern);
                return _testScenesNamePatternRegex;
            }
        }

        bool MatchesUnityVersion(string testSceneName) {
            return unityMajorVersion >= GetSceneVersion(testSceneName);
        }

        int GetSceneVersion(string sceneName) {
            var split = sceneName.Split('_');
            if (split.Length > 1) {
                var lastPart = split.Last();
                int version;
                if (int.TryParse(lastPart, out version))
                    return version;
            }
            return 0;
        }

        static int unityMajorVersion {
            get { return int.Parse(Application.unityVersion.Split('.').First()); }
        }

        void LoadScene(string sceneKey) {
            EditorApplication.LoadLevelAdditiveInPlayMode(sceneKey);
        }
    #else
        IEnumerable<int> GetTestSceneKeys() {
            // Skip the very first scene which is the scene containing this object
            return Enumerable.Range(1, SceneManager.sceneCountInBuildSettings - 1);
        }
        
        void LoadScene(int sceneKey) {
            SceneManager.LoadScene(sceneKey, LoadSceneMode.Additive);
        }
    #endif

        IEnumerator TestAndUnloadScene(Ref<AutomatedTestResult> outResult) {
            var scene = SceneManager.GetSceneAt(1);
            try {
                yield return ActivateScene(scene);
                yield return TestScene(scene, outResult);
            } finally {
                SceneManager.UnloadScene(SceneManager.GetSceneAt(1));
            }
        }

        IEnumerator ActivateScene(Scene scene) {
            while (!scene.isLoaded)
                yield return null;
            while (!SceneManager.SetActiveScene(scene))
                yield return null;
            Assert.IsTrue(SceneManager.GetActiveScene() == scene);
        }

        IEnumerator TestScene(Scene scene, Ref<AutomatedTestResult> outResult) {
            var test = SceneTest.FromScene(scene);
            if (test != null) {
                test.speedUp = speedRun;
                yield return StartCoroutine(test.WaitFinish());
                if (test.result.isFail && replaceReferenceOnFail)
                    test.ReplaceReference();
                outResult.value = test.result;
                changed.InvokeSafe(this);
            }
        }

        class SceneTest {
            AutomatedTest _automatedTest;

            SceneTest(AutomatedTest test) {
                _automatedTest = test;
            }

            public static SceneTest FromScene(Scene scene) {
                return scene.GetRootGameObjects()
                    .Select(x => x.GetComponent<AutomatedTest>())
                    .Where(x => x != null)
                    .Select(x => new SceneTest(x))
                    .FirstOrDefault();
            }

            public bool speedUp {
                get { return _automatedTest.speedUp; }
                set { _automatedTest.speedUp = value; }
            }

            public AutomatedTestResult result { get { return _automatedTest.result; } }

            public IEnumerator WaitFinish() {
                while (!_automatedTest.isFinished)
                    yield return null;
            }

            public void ReplaceReference() {
            #if UNITY_EDITOR
                _automatedTest.SaveLastRecordAsReference();
            #else
                Debug.LogAssertion("ReplaceReference should not be called from non-editor");
            #endif
            }
        }

        static void ReportToLog(AutomatedTestResults results) {
            Debug.LogFormat("Testing finished: {0}", results.isPass ? "PASS" : "FAIL");
            if (results.isFail) {
                var failure = results.failures.First();
                Debug.LogFormat("First failure: {0}\n{1}", failure.sceneName, failure.error.message);
            }
        }

        void ExitIfRequested() {
            if (exitOnFinish) {
            #if UNITY_EDITOR
                EditorApplication.Exit(testResults.isPass ? 0 : 1);
            #else
                Debug.LogError("exitOnFinish facility implemented only in editor mode for now");
            #endif
            }
        }
    }
}
