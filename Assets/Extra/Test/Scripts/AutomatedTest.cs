using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using UnityEngine;
using UnityEngine.Assertions;
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
        LogHandler _logHandler;

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
                _lastExecutionSteps.Add(new CapturedStep(texture, _logHandler.TakeRecords()));
                NotifyChanged();
            }
        }

        public YieldInstruction ProceedAnimation(Animator animator, float normalizedTime) {
            return StartCoroutine(PlayAnimatorUpTo(animator, normalizedTime));
        }
        
        IEnumerator PlayAnimatorUpTo(Animator animator, float normalizedTime) {
            if (!_updatedAtLeastOnce)
                yield return null; // to prevent execution before Update
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

        class LogHandler : ILogHandler, IDisposable {
            readonly List<LogRecord> _logHandlerRecords = new List<LogRecord>();
            readonly List<LogRecord> _applicationRecords = new List<LogRecord>();
            readonly ILogHandler _originalHandler;

            public LogHandler() {
                _originalHandler = Debug.unityLogger.logHandler;
                // We use both logHandler and logMessageReceived ways here because
                // the former doesn't catch Unity's "system" messages ("DestroyImmediate
                // should not be called from physics callback") while the latter
                // does not provide context object which is crucial for some test cases.
                Debug.unityLogger.logHandler = this;
                Application.logMessageReceived += OnLogMessageReceived;
            }

            void OnLogMessageReceived(string condition, string stacktrace, LogType type) {
                _applicationRecords.Add(new LogRecord(condition, type, null));
            }

            void ILogHandler.LogException(Exception exception, UnityEngine.Object context) {
                _logHandlerRecords.Add(new LogRecord(exception.Message, LogType.Exception, context));
                _originalHandler.LogException(exception, context);
            }

            void ILogHandler.LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args) {
                _logHandlerRecords.Add(new LogRecord(string.Format(format, args), logType, context));
                _originalHandler.LogFormat(logType, context, format, args);
            }

            public List<LogRecord> TakeRecords() {
                Assert.IsTrue(_logHandlerRecords.Count <= _applicationRecords.Count);
                var result = new List<LogRecord>();
                for (int appIdx = 0, handlerIdx = 0; appIdx < _applicationRecords.Count; ++appIdx) {
                    var applicationRecord = _applicationRecords[appIdx];
                    var handlerRecord = handlerIdx < _logHandlerRecords.Count ? _logHandlerRecords[handlerIdx] : null;
                    if (handlerRecord != null 
                            && handlerRecord.message == applicationRecord.message
                            && handlerRecord.logType == applicationRecord.logType) {
                        result.Add(handlerRecord);
                        ++handlerIdx;
                    } else
                        result.Add(applicationRecord);
                }
                _logHandlerRecords.Clear();
                _applicationRecords.Clear();
                return result;
            }

            public void Dispose() {
                Debug.unityLogger.logHandler = _originalHandler;
                Application.logMessageReceived -= OnLogMessageReceived;
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
            _logHandler = new LogHandler();
        }

        void EjectLogHandler() {
            if (_logHandler != null) {
                _logHandler.Dispose();
                _logHandler = null;
            }
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
