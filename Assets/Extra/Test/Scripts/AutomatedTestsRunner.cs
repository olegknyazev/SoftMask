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
                    yield return LoadAndTestScene(sceneKey, testResult);
                    testResultList.Add(testResult.value);
                    if (testResult.value.isFail)
                        break;
                }
            } finally {
                ResolutionUtility.RevertTestResolution();
                testResults = new AutomatedTestResults(testResultList);
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
                    .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                    .Where(path => !string.IsNullOrEmpty(path))
                    .Where(path => testScenesNamePatternRegex.IsMatch(Path.GetFileNameWithoutExtension(path)));
        }
        
        Regex _testScenesNamePatternRegex;
        Regex testScenesNamePatternRegex {
            get {
                if (_testScenesNamePatternRegex == null)
                    _testScenesNamePatternRegex = new Regex(testScenesNamePattern);
                return _testScenesNamePatternRegex;
            }
        }

        IEnumerator LoadAndTestScene(string sceneKey, Ref<AutomatedTestResult> outResult) {
            EditorApplication.LoadLevelAdditiveInPlayMode(sceneKey);
            yield return LoadAndTestSceneImpl(outResult);
        }
    #else
        IEnumerable<int> GetTestSceneKeys() {
            // Skip the very first scene which is the scene containing this object
            return Enumerable.Range(1, SceneManager.sceneCountInBuildSettings - 1);
        }
        
        IEnumerator LoadAndTestScene(int sceneKey, Ref<AutomatedTestResult> outResult) {
            SceneManager.LoadScene(sceneKey, LoadSceneMode.Additive);
            yield return LoadAndTestSceneImpl(outResult);
        }
    #endif

        IEnumerator LoadAndTestSceneImpl(Ref<AutomatedTestResult> outResult) {
            var scene = SceneManager.GetSceneAt(1);
            try {
                yield return ActivateScene(scene);
                yield return TestScene(scene, outResult);
            } finally {
                SceneManager.UnloadScene(SceneManager.GetSceneAt(1));
            }
        }

        IEnumerator ActivateScene(Scene scene) {
            while (!SceneManager.SetActiveScene(scene))
                yield return null;
            Assert.IsTrue(SceneManager.GetActiveScene() == scene);
        }

        IEnumerator TestScene(Scene scene, Ref<AutomatedTestResult> outResult) {
            var test = SceneTest.FromScene(scene);
            if (test != null) {
                test.speedUp = speedRun;
                yield return StartCoroutine(test.WaitFinish());
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
        }
    }
}
