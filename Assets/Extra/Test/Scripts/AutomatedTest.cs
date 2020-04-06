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
        static readonly string ReferencePath = TestScenesPath + "ReferenceScreens";
        static readonly string ScreenshotExt = ".png";

        public bool speedUp = false;

        [SerializeField] List<ScreenValidationRuleKeyValuePair> _validationRulePairs = new List<ScreenValidationRuleKeyValuePair>();
        [SerializeField] List<Texture2D> _lastExecutionScreens = new List<Texture2D>();
        [SerializeField] List<Texture2D> _referenceScreens = new List<Texture2D>();
        AutomatedTestResult _result = null;
        bool _updatedAtLeastOnce = false;
        List<ExpectedLogRecord> _expectedLog = new List<ExpectedLogRecord>();
        List<LogRecord> _lastExecutionLog = new List<LogRecord>();
        AutomatedTestError _explicitFail;

        public int referenceStepsCount {
            get { return _referenceScreens.Count; }
        }
        public IEnumerable<ScreenValidationRule> validationRules {
            get { return _validationRulePairs.Select(x => x.rule); }
        }
        public bool isReferenceEmpty {
            get { return referenceStepsCount == 0; }
        }
        public int lastExecutionStepsCount {
            get { return _lastExecutionScreens.Count; }
        }
        public bool isLastExecutionEmpty {
            get { return _lastExecutionScreens.Count == 0; }
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
            DeleteReferenceScreens();
            if (!Directory.Exists(currentSceneReferenceDir))
                Directory.CreateDirectory(currentSceneReferenceDir);
            for (int i = 0; i < _lastExecutionScreens.Count; ++i) {
                var screenshot = _lastExecutionScreens[i];
                var screenshotPath = GetScreenshotPath(i);
                File.WriteAllBytes(screenshotPath, screenshot.EncodeToPNG());
                AssetDatabase.ImportAsset(screenshotPath);
                SetupScreenshotImportSettings(screenshotPath);
                _referenceScreens.Add(screenshot);
            }
            NotifyChanged();
        }

        public void DeleteReference() {
            DeleteReferenceScreens();
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
            var saveScreenshotCoroutine = StartCoroutine(ProcessImpl());
            return WaitForDelayAfterStep(delaySeconds, saveScreenshotCoroutine);
        }
        
        IEnumerator ProcessImpl() {
            yield return new WaitForEndOfFrame();
            if (!isFinished) {
                var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0, false);
                _lastExecutionScreens.Add(texture);
                NotifyChanged();
            }
        }

        YieldInstruction WaitForDelayAfterStep(float delaySeconds, Coroutine stepCoroutine) {
            return delaySeconds > 0f && !speedUp
                ? new WaitForSeconds(delaySeconds)
                : (YieldInstruction)stepCoroutine;
        }

        public YieldInstruction ProceedAnimation(Animator animator, float normalizedTime) {
            return StartCoroutine(ProcessAnimation(animator, normalizedTime));
        }
        
        IEnumerator ProcessAnimation(Animator animator, float normalizedTime) {
            if (!_updatedAtLeastOnce)
                yield return null; // to prevent execution before Update
            if (!speedUp)
                while (GetAnimationTime(animator) < normalizedTime)
                    yield return null;
            var state = animator.GetCurrentAnimatorStateInfo(0);
            animator.Play(state.shortNameHash, 0, normalizedTime);            
            yield return StartCoroutine(ProcessImpl());
        }
        
        float GetAnimationTime(Animator animator) {
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
            else if (_lastExecutionScreens.Count != _referenceScreens.Count)
                errors.Add(new AutomatedTestError(
                    string.Format("Expected {0} steps but {1} occured.", 
                        _referenceScreens.Count, 
                        _lastExecutionScreens.Count)));
            else
                for (int step = 0; step < _lastExecutionScreens.Count; ++step) {
                    var validator = ValidationRuleForStep(step);
                    if (!validator.Validate(_referenceScreens[step], _lastExecutionScreens[step])) {
                        File.WriteAllBytes("actual.png", _lastExecutionScreens[step].EncodeToPNG());
                        File.WriteAllBytes("ref.png", _referenceScreens[step].EncodeToPNG());
                        File.WriteAllBytes("diff.png", validator.Diff(_referenceScreens[step], _lastExecutionScreens[step]).EncodeToPNG());
                        errors.Add(new AutomatedTestError(
                            string.Format("Screenshots differ at step {0}.", step), 
                            step, 
                            validator.Diff(_referenceScreens[step], _lastExecutionScreens[step])));
                        break;
                    }
                }
            return new AutomatedTestResult(errors);
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
            LoadReferenceScreens();
        #else
            RemoveObsoleteReferenceScreens();
        #endif
            if (Application.isPlaying)
                InjectLogHandler();
        }

        public void Start() {
            _lastExecutionScreens.Clear();
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

        void RemoveObsoleteReferenceScreens() {
            _referenceScreens.RemoveAll(x => !x);
        }

        void InjectLogHandler() {
            Debug.logger.logHandler = new LogHandler(_lastExecutionLog, Debug.logger.logHandler);
        }

        void EjectLogHandler() {
            var injectedHandler = Debug.logger.logHandler as LogHandler;
            if (injectedHandler != null)
                Debug.logger.logHandler = injectedHandler.originalHandler;
        }
                
    #if UNITY_EDITOR
        void LoadReferenceScreens() {
            _referenceScreens.Clear();
            foreach (var potentialPath in IterateScreenshotPaths()) {
                var screen = AssetDatabase.LoadAssetAtPath<Texture2D>(potentialPath);
                if (!screen)
                    break;
                _referenceScreens.Add(screen);
            }
        }

        void DeleteReferenceScreens() {
            foreach (var screenPath in IterateScreenshotPaths())
                if (!AssetDatabase.DeleteAsset(screenPath))
                    break;
            _referenceScreens.Clear();
        }
               
        IEnumerable<string> IterateScreenshotPaths() {
            for (int i = 0;; ++i)
                yield return GetScreenshotPath(i);
        }
    #endif

        string GetScreenshotPath(int stepIndex) {
            return Path.ChangeExtension(
                Path.Combine(
                    currentSceneReferenceDir,
                    string.Format("{0:D2}", stepIndex)),
                ScreenshotExt);
        }
        
        string currentSceneReferenceDir {
            get {
                var currentScenePath = gameObject.scene.path;
                var relativeScenePath =
                    currentScenePath.StartsWith(TestScenesPath)
                        ? currentScenePath.Substring(TestScenesPath.Length).Replace(".unity", "")
                        : gameObject.scene.name;
                return Path.Combine(ReferencePath, relativeScenePath);
            }
        }

    #if UNITY_EDITOR
        static void SetupScreenshotImportSettings(string screenshotPath) {
            var importer = (TextureImporter)AssetImporter.GetAtPath(screenshotPath);
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.isReadable = true;
            importer.SaveAndReimport();
        }
    #endif
    }
}
