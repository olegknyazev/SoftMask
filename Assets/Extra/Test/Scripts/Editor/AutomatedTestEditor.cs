using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using SoftMasking.Tests;

namespace SoftMasking.Editor {
    [CustomEditor(typeof(AutomatedTest))]
    public class AutomatedTestEditor : UnityEditor.Editor {
        SerializedProperty _validationRulePairs;
        SerializedProperty _speedUp;
        Vector2 _errorDiffScrollPos;

        AutomatedTest targetTest {
            get { return target as AutomatedTest; }
        }

        public void OnEnable() {
            _validationRulePairs = serializedObject.FindProperty("_validationRulePairs");
            _speedUp = serializedObject.FindProperty("speedUp");
            Assert.IsNotNull(_validationRulePairs);
            Assert.IsNotNull(_speedUp);
            targetTest.changed += OnTestChanged;
        }

        public void OnDisable() {
            if (targetTest)
                targetTest.changed -= OnTestChanged;
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();
            EditorGUILayout.PropertyField(_validationRulePairs, true);
            EditorGUILayout.PropertyField(_speedUp);
            EditorGUILayout.LabelField(
                "Reference:", 
                string.Format("{0} steps", targetTest.referenceStepsCount));
            EditorGUILayout.LabelField(
                "Last Execution:", 
                string.Format("{0} steps", targetTest.lastExecutionStepsCount));
            StateGUI();
            using (new EditorGUI.DisabledGroupScope(targetTest.isLastExecutionEmpty))
                if (GUILayout.Button("Accept Last Execution as Reference"))
                    targetTest.SaveLastRecordAsExample();
            using (new EditorGUI.DisabledGroupScope(targetTest.isReferenceEmpty || Application.isPlaying))
                if (GUILayout.Button("Delete Reference"))
                    targetTest.DeleteReference();
            serializedObject.ApplyModifiedProperties();
        }

        public void OnSceneGUI() {
            foreach (var step in targetTest.validationRules)
                Handles.DrawSolidRectangleWithOutline(
                    step.ValidationRect(new Rect(Vector2.zero, Handles.GetMainGameViewSize())), 
                    Color.clear, 
                    Color.white);
        }

        void OnTestChanged(AutomatedTest test) {
            Repaint();
        }

        enum State { NotStarted, InProgress, Finished }

        State currentState {
            get {
                if (Application.isPlaying)
                    return targetTest.isFinished ? State.Finished : State.InProgress;
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
                    var result = targetTest.result;
                    if (result.isPass)
                        GUILayout.Label("Passed", AutomatedTestStyles.passed);
                    else {
                        GUILayout.Label(
                            string.Format("Failed ({0} errors)", result.errorCount),
                            AutomatedTestStyles.failed);
                        using (var scrollScope = new GUILayout.ScrollViewScope(_errorDiffScrollPos)) {
                            GUILayout.Box(result.errors.First().diff);
                            _errorDiffScrollPos = scrollScope.scrollPosition;
                        }
                    }
                    using (new IndentScope())
                        foreach (var error in result.errors)
                            EditorGUILayout.LabelField(error.message, AutomatedTestStyles.errorMessage);
                    break;
            }
        }
    }
}
