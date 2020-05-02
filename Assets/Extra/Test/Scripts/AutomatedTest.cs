using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using UnityEngine;

namespace SoftMasking.Tests {
    [Serializable] public class CapturedStepState {
        [SerializeField] Texture2D _texture;

        public CapturedStepState(Texture2D texture) {
            _texture = texture;
        }

        public Texture2D texture { get { return _texture; } }
    }

    [ExecuteInEditMode]
    public class AutomatedTest : MonoBehaviour {
        static readonly string TestScenesPath = "Assets/Extra/Test/Scenes/";

        public bool speedUp = false;

        [SerializeField] List<ScreenValidationRuleKeyValuePair> _validationRulePairs = new List<ScreenValidationRuleKeyValuePair>();
        [SerializeField] List<CapturedStepState> _lastExecutionSteps = new List<CapturedStepState>();
        [SerializeField] ReferenceSteps _referenceSteps = new ReferenceSteps();
        AutomatedTestResult _result = null;
        bool _updatedAtLeastOnce = false;
        List<ExpectedLogRecord> _expectedLog = new List<ExpectedLogRecord>();
        List<LogRecord> _lastExecutionLog = new List<LogRecord>();
        AutomatedTestError _explicitFail;

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
        public void SaveLastRecordAsExample() {
            _referenceSteps.ReplaceBy(_lastExecutionSteps);
            NotifyChanged();
        }

        public void DeleteReference() {
            _referenceSteps.Clear();
            _validationRulePairs.Clear();
            NotifyChanged();
        }
    #endif
        
        public void ExpectLog(ExpectedLogRecord expectedRecord) {
            _expectedLog.Add(expectedRecord);
        }

        public void ExpectLog(string messagePattern, LogType logType, UnityEngine.Object context) {
            _expectedLog.Add(new ExpectedLogRecord(messagePattern, logType, context));
        }

        public YieldInstruction Proceed(float delaySeconds = 0f) {
            return StartCoroutine(WaitAll(
                StartCoroutine(CaptureStep()),
                new WaitForSeconds(speedUp ? 0 : delaySeconds)));
        }
       
        static IEnumerator WaitAll(params YieldInstruction[] instructions) {
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
                _lastExecutionSteps.Add(new CapturedStepState(texture));
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
            var errors = new List<AutomatedTestError>();
            var unexpectedLog = _expectedLog.Aggregate(_lastExecutionLog, (log, pat) => pat.Filter(log));
            if (_explicitFail != null)
                errors.Add(_explicitFail);
            else if (unexpectedLog.Count > 0)
                errors.Add(new AutomatedTestError(
                    string.Format("{0} unexpected log records occured. First unexpected:\n{1}",
                        unexpectedLog.Count,
                        unexpectedLog[0].message)));
            else if (_expectedLog.Count > _lastExecutionLog.Count)
                errors.Add(new AutomatedTestError(
                    string.Format("Not all expected log records occured. Expected: {0}, occured: {1}",
                        _expectedLog.Count,
                        _lastExecutionLog.Count)));
            else if (_lastExecutionSteps.Count != _referenceSteps.count)
                errors.Add(new AutomatedTestError(
                    string.Format("Expected {0} steps but {1} occured.", 
                        _referenceSteps.count,
                        _lastExecutionSteps.Count)));
            else
                for (int step = 0; step < _lastExecutionSteps.Count; ++step) {
                    var validator = ValidationRuleForStep(step);
                    var referenceStep = _referenceSteps[step];
                    var lastExecutionStep = _lastExecutionSteps[step];
                    if (!validator.Validate(referenceStep.texture, lastExecutionStep.texture)) {
                        File.WriteAllBytes("actual.png", lastExecutionStep.texture.EncodeToPNG());
                        File.WriteAllBytes("ref.png", referenceStep.texture.EncodeToPNG());
                        File.WriteAllBytes("diff.png", validator.Diff(referenceStep.texture, lastExecutionStep.texture).EncodeToPNG());
                        errors.Add(new AutomatedTestError(
                            string.Format("Screenshots differ at step {0}.", step), 
                            step,
                            validator.Diff(referenceStep.texture, lastExecutionStep.texture)));
                        break;
                    }
                }
            return new AutomatedTestResult(currentSceneRelativeDir, errors);
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
            _referenceScreens.RemoveObsoletes();
        #endif
            if (Application.isPlaying)
                InjectLogHandler();
        }

        public void Start() {
            _lastExecutionSteps.Clear();
            if (Application.isPlaying)
                ResolutionUtility.SetTestResolution();
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
            Debug.logger.logHandler = new LogHandler(_lastExecutionLog, Debug.logger.logHandler);
        }

        void EjectLogHandler() {
            var injectedHandler = Debug.logger.logHandler as LogHandler;
            if (injectedHandler != null)
                Debug.logger.logHandler = injectedHandler.originalHandler;
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
