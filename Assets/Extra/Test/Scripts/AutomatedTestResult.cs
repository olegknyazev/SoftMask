using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SoftMasking.Tests {
    public class AutomatedTestResults {
        List<AutomatedTestResult> _results;
        
        public AutomatedTestResults(IList<AutomatedTestResult> results) {
            _results = new List<AutomatedTestResult>(results);
            isPass = _results.All(x => x.isPass);
        }
        
        public bool isPass { get; private set; }
        public bool isFail { get { return !isPass; } }
        public int testCount { get { return _results.Count; } }

        public IEnumerable<AutomatedTestResult> failures { 
            get { return _results.Where(x => x.isFail); }
        }
    }

    public class AutomatedTestResult {
        List<AutomatedTestError> _errors = new List<AutomatedTestError>();

        // TODO there is always no more than one error
        public AutomatedTestResult(string sceneName, AutomatedTestError error) {
            this.sceneName = sceneName;
            if (error != null)
                _errors.Add(error);
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
