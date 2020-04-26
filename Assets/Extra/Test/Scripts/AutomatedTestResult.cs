using System.Collections.Generic;
using UnityEngine;

namespace SoftMasking.Tests {
    public class AutomatedTestResult {
        List<AutomatedTestError> _errors;

        public AutomatedTestResult(string sceneName, IEnumerable<AutomatedTestError> errors) {
            this.sceneName = sceneName;
            _errors = new List<AutomatedTestError>(errors);
        }

        public string sceneName { get; private set; }
        public IEnumerable<AutomatedTestError> errors { get { return _errors; } }
        public int errorCount { get { return _errors.Count; } }
        public bool isPass { get { return _errors.Count == 0; } }
        public bool isFail { get { return !isPass; } }
    }
    
    public class AutomatedTestError {
        public AutomatedTestError(string message, int stepNumber = -1, Texture2D diff = null) {
            this.message = message;
            this.stepNumber = stepNumber;
            this.diff = diff;
        }

        public string message { get; private set; }
        public int stepNumber { get; private set; }
        public Texture2D diff { get; private set; }
    }
}
