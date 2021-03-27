using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SoftMasking.Tests {
    [ExecuteInEditMode]
    public class AutomatedTest : MonoBehaviour {
        static readonly string TestScenesPath = "Assets/Extra/Test/Scenes/";

        public bool speedUp = false;

        [SerializeField] List<ScreenValidationRuleKeyValuePair> _validationRulePairs = new List<ScreenValidationRuleKeyValuePair>();
        [SerializeField] List<CapturedStep> _lastExecutionSteps = new List<CapturedStep>();
        [SerializeField] ReferenceSteps _referenceSteps = new ReferenceSteps();
        AutomatedTestResult _result = null;
        bool _updatedAtLeastOnce = false;
        AutomatedTestError _explicitFail;
        List<LogRecord> _currentStepRecords = new List<LogRecord>();

        public int referenceStepsCount {
            get { return _referenceSteps.count; }
        }
        public IEnumerable<ScreenValidationRule> validationRules {
            get { return _validationRulePairs.Select(x => x.rule); }
        }
        public bool isReferenceEmpty {
            get { return referenceStepsCount == 0; }
        }
        public int lastExecutionStepsCount {
            get { return _lastExecutionSteps.Count; }
        }
        public bool isLastExecutionEmpty {
            get { return _lastExecutionSteps.Count == 0; }
        }
        public bool isFinished {
            get { return _result != null; }
        }
        public AutomatedTestResult result {
            get { return _result; }
        }

        public event Action<AutomatedTest> changed;

    #if UNITY_EDITOR
        public void SaveLastRecordAsReference() {
            _referenceSteps.ReplaceBy(_lastExecutionSteps);
            NotifyChanged();
        }

        public void DeleteReference() {
            _referenceSteps.Clear();
            _validationRulePairs.Clear();
            NotifyChanged();
        }
    #endif
    
        public YieldInstruction Proceed(float delaySeconds = 0f) {
            return StartCoroutine(WaitInOrder(
                StartCoroutine(CaptureStep()),
                new WaitForSeconds(speedUp ? 0 : delaySeconds)));
        }
       
        static IEnumerator WaitInOrder(params YieldInstruction[] instructions) {
            foreach (var i in instructions)
                yield return i;
        }
 
        IEnumerator CaptureStep() {
            if (!_updatedAtLeastOnce) { // TODO it would be clearer to refer ResolutionUtility's coroutine here?
                // Seems like 2019.1 needs at least two frames to adjust canvas after a game view size change
                yield return null;
                yield return null;
            }
            yield return new WaitForEndOfFrame();
            if (!isFinished) {
                var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0, false);
                _lastExecutionSteps.Add(new CapturedStep(texture, _currentStepRecords));
                _currentStepRecords.Clear();
                NotifyChanged();
            }
        }

        public YieldInstruction ProceedAnimation(Animator animator, float normalizedTime) {
            return StartCoroutine(PlayAnimatorUpTo(animator, normalizedTime));
        }
        
        IEnumerator PlayAnimatorUpTo(Animator animator, float normalizedTime) {
            if (!_updatedAtLeastOnce)
                yield return null; // to prevent execution before Update
            if (!speedUp)
                while (GetAnimationTime(animator) < normalizedTime)
                    yield return null;
            var state = animator.GetCurrentAnimatorStateInfo(0);
            animator.Play(state.shortNameHash, 0, normalizedTime);            
            yield return StartCoroutine(CaptureStep());
        }
        
        static float GetAnimationTime(Animator animator) {
            return animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        }

        public IEnumerator Fail(string reason) {
            if (!isFinished) {
                _explicitFail = new AutomatedTestError(reason, lastExecutionStepsCount - 1);
                yield return Finish();
            }
            yield break;
        }

        public YieldInstruction Finish() {
            EjectLogHandler();
            _result = Validate();
            NotifyChanged();
            return new WaitForEndOfFrame();
        }
        
        AutomatedTestResult Validate() {
            return new AutomatedTestResult(currentSceneRelativeDir, ValidateImpl());
        }

        AutomatedTestError ValidateImpl() {
            if (_explicitFail != null)
                return _explicitFail;
            else if (_lastExecutionSteps.Count != _referenceSteps.count)
                return new AutomatedTestError(
                    string.Format("Expected {0} steps but {1} occured.", 
                        _referenceSteps.count,
                        _lastExecutionSteps.Count));
            else {
                for (int step = 0; step < _lastExecutionSteps.Count; ++step) {
                    var logError = ValidateLog(step);
                    if (logError != null)
                        return logError;
                    var screenError = ValidateScreen(step);
                    if (screenError != null)
                        return screenError;
                }
            }
            return null;
        }

        AutomatedTestError ValidateLog(int step) {
            var expected = _referenceSteps[step];
            var actual = _lastExecutionSteps[step];
            var diff = LogRecords.Diff(expected.logRecords, actual.logRecords);
            if (diff.extra.Count > 0)
                return new AutomatedTestError(
                    string.Format("{0} unexpected log message(s) at step {1}. First unexpected: {2}",
                        diff.extra.Count, step, diff.extra[0]),
                    step);
            if (diff.missing.Count > 0)
                return new AutomatedTestError(
                    string.Format("{0} expected log message(s) are missing at step {1}. First missing: {2}",
                        diff.missing.Count, step, diff.missing[0]),
                    step);
            return null;
        }

        AutomatedTestError ValidateScreen(int step) {
            var expected = _referenceSteps[step];
            var actual = _lastExecutionSteps[step];
            var validator = ValidationRuleForStep(step);
            if (!validator.Validate(expected.texture, actual.texture)) {
                var diff = validator.Diff(expected.texture, actual.texture);
                File.WriteAllBytes("actual.png", actual.texture.EncodeToPNG());
                File.WriteAllBytes("ref.png", expected.texture.EncodeToPNG());
                File.WriteAllBytes("diff.png", diff.EncodeToPNG());
                return new AutomatedTestError(
                    string.Format("Screenshots differ at step {0}.", step), 
                    step,
                    diff);
            }
            return null;
        }

        ScreenValidationRule ValidationRuleForStep(int stepIndex) {
            var rulePair = _validationRulePairs.FirstOrDefault(x => x.MatchesIndex(stepIndex));
            return rulePair != null ? rulePair.rule : ScreenValidationRule.topLeftWholeScreen;
        }

        void NotifyChanged() {
            changed.InvokeSafe(this);
        }

        class LogHandler : ILogHandler {
            readonly List<LogRecord> _log;
            readonly ILogHandler _originalHandler;

            public LogHandler(List<LogRecord> log, ILogHandler original) {
                _log = log;
                _originalHandler = original;
            }

            public ILogHandler originalHandler { get { return _originalHandler; } }

            public void LogException(Exception exception, UnityEngine.Object context) {
                _log.Add(new LogRecord(exception.Message, LogType.Exception, context));
                _originalHandler.LogException(exception, context);
            }

            public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args) {
                _log.Add(new LogRecord(string.Format(format, args), logType, context));
                _originalHandler.LogFormat(logType, context, format, args);
            }
        }

        public void Awake() {
        #if UNITY_EDITOR
            _referenceSteps.Load(currentSceneRelativeDir);
        #else
            _referenceSteps.RemoveObsoletes();
        #endif
            if (Application.isPlaying)
                InjectLogHandler();
        }

        public void Start() {
            _lastExecutionSteps.Clear();
            if (Application.isPlaying) {
                ResolutionUtility.SetTestResolution();
            #if UNITY_2019_1_OR_NEWER && UNITY_EDITOR
                EditorSettings.asyncShaderCompilation = false;
            #endif
            }
        }
        
        public void Update() {
            _updatedAtLeastOnce = true;
        }

        public void OnDestroy() {
            if (Application.isPlaying) {
                ResolutionUtility.RevertTestResolution();
                EjectLogHandler();
            }
        }

        public void OnValidate() {
            foreach (var pair in _validationRulePairs)
                pair.rule.RoundRect();
        }

        void InjectLogHandler() {
            Debug.unityLogger.logHandler = new LogHandler(_currentStepRecords, Debug.unityLogger.logHandler);
        }

        void EjectLogHandler() {
            var injectedHandler = Debug.unityLogger.logHandler as LogHandler;
            if (injectedHandler != null)
                Debug.unityLogger.logHandler = injectedHandler.originalHandler;
        }

        string currentSceneRelativeDir {
            get {
                var currentScenePath = gameObject.scene.path;
                return currentScenePath.StartsWith(TestScenesPath)
                    ? currentScenePath.Substring(TestScenesPath.Length).Replace(".unity", "")
                    : gameObject.scene.name;
            }
        }
    }
}
