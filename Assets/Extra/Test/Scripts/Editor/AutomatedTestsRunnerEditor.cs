using System.Linq;
using UnityEditor;
using UnityEngine;
using SoftMasking.Tests;

namespace SoftMasking.Editor {
    [CustomEditor(typeof(AutomatedTestsRunner))]
    public class AutomatedTestsRunnerEditor : UnityEditor.Editor {
        Vector2 _errorDiffScrollPos;

        AutomatedTestsRunner targetRunner {
            get { return target as AutomatedTestsRunner; }
        }

        public void OnEnable() {
            targetRunner.changed += OnRunnerChanged;
        }

        public void OnDisable() {
            if (targetRunner)
                targetRunner.changed -= OnRunnerChanged;
        }

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            StateGUI();
            BuildTestPlayerGUI();
        }

        void BuildTestPlayerGUI() {
            using (new EditorGUI.DisabledScope(Application.isPlaying)) {
                if (GUILayout.Button("Build Test Player"))
                    targetRunner.BuildTestPlayer();
                if (GUILayout.Button("Build and Run Test Player"))
                    targetRunner.BuildTestPlayer(BuildOptions.AutoRunPlayer);
            }
        }

        void OnRunnerChanged(AutomatedTestsRunner runner) {
            Repaint();
        }

        enum State { NotStarted, InProgress, Finished }

        State currentState {
            get {
                if (Application.isPlaying)
                    return targetRunner.isFinished ? State.Finished : State.InProgress;
                else
                    return State.NotStarted;
            }
        }

        void StateGUI() {
            switch (currentState) {
                case State.NotStarted:
                    GUILayout.Label("Not Started");
                    break;
                case State.InProgress:
                    GUILayout.Label("In Progress", AutomatedTestStyles.inProgress);
                    break;
                case State.Finished:
                    var results = targetRunner.testResults;
                    var isFail = results.isFail;
                    if (isFail) {
                        var errorCount = results.failures.Count();
                        GUILayout.Label(
                            string.Format("Failed ({0} errors)", errorCount),
                            AutomatedTestStyles.failed);
                        using (var scrollScope = new GUILayout.ScrollViewScope(_errorDiffScrollPos)) {
                            GUILayout.Box(results.failures.First().error.diff);
                            _errorDiffScrollPos = scrollScope.scrollPosition;
                        }
                        using (new IndentScope())
                            foreach (var result in results.failures) {
                                EditorGUILayout.LabelField(result.sceneName);
                                using (new IndentScope())
                                    EditorGUILayout.LabelField(result.error.message);
                            }
                    } else
                        GUILayout.Label("Passed", AutomatedTestStyles.passed);
                    break;
            }
        }
    }
}
