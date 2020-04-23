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
        
        Dictionary<string, AutomatedTestResult> _testResults = new Dictionary<string, AutomatedTestResult>();

        public bool isFinished { get; private set; }

        public IDictionary<string, AutomatedTestResult> testResults { get { return _testResults; } }

        public event Action<AutomatedTestsRunner> changed;

        class Ref<T> { public T value; }

        public IEnumerator Start() {
            ResolutionUtility.SetTestResolution();
            try {
                foreach (var sceneKey in GetTestSceneKeys()) {
                    var testSucceeded = new Ref<bool>();
                    yield return LoadAndTestScene(sceneKey, testSucceeded);
                    if (!testSucceeded.value)
                        break;
                }
            } finally {
                ResolutionUtility.RevertTestResolution();
                isFinished = true;
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
    #endif

    #if UNITY_EDITOR
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

        IEnumerator LoadAndTestScene(string sceneKey, Ref<bool> outSuccess) {
            EditorApplication.LoadLevelAdditiveInPlayMode(sceneKey);
            yield return LoadAndTestSceneImpl(outSuccess);
        }
    #else
        IEnumerable<int> GetTestSceneKeys() {
            // Skip the very first scene which is the scene containing this object
            return Enumerable.Range(1, SceneManager.sceneCountInBuildSettings - 1);
        }
        
        IEnumerator LoadAndTestScene(int sceneKey, Ref<bool> outSuccess) {
            SceneManager.LoadScene(sceneKey, LoadSceneMode.Additive);
            yield return LoadAndTestSceneImpl(outSuccess);
        }
    #endif

        IEnumerator LoadAndTestSceneImpl(Ref<bool> outSuccess) {
            var scene = SceneManager.GetSceneAt(1);
            try {
                yield return ActivateScene(scene);
                yield return TestScene(scene, outSuccess);
            } finally {
                SceneManager.UnloadScene(SceneManager.GetSceneAt(1));
            }
        }

        IEnumerator ActivateScene(Scene scene) {
            while (!SceneManager.SetActiveScene(scene))
                yield return null;
            Assert.IsTrue(SceneManager.GetActiveScene() == scene);
        }

        IEnumerator TestScene(Scene scene, Ref<bool> outSuccess) {
            var test = SceneTest.FromScene(scene);
            if (test != null) {
                test.speedUp = speedRun;
                yield return StartCoroutine(test.WaitFinish());
                outSuccess.value = test.result.isPass;
                _testResults.Add(scene.name, test.result);
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
