using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SoftMasking.Tests {
    public class AutomatedTestResults {
        readonly List<AutomatedTestResult> _results;
        
        public AutomatedTestResults(IList<AutomatedTestResult> results) {
            _results = new List<AutomatedTestResult>(results);
            isPass = _results.All(x => x.isPass);
        }
        
        public bool isPass { get; }
        public bool isFail => !isPass;
        public int testCount => _results.Count;

        public IEnumerable<AutomatedTestResult> failures { 
            get { return _results.Where(x => x.isFail); }
        }
    }

    public class AutomatedTestResult {
        public AutomatedTestResult(string sceneName, AutomatedTestError error) {
            this.sceneName = sceneName;
            this.error = error;
        }

        public string sceneName { get; }
        public AutomatedTestError error { get; }
        public bool isPass => error == null;
        public bool isFail => !isPass;
    }
    
    public class AutomatedTestError {
        public AutomatedTestError(string message, int stepNumber = -1, Texture2D diff = null) {
            this.message = message;
            this.stepNumber = stepNumber;
            this.diff = diff;
        }

        public string message { get; }
        public int stepNumber { get; }
        public Texture2D diff { get; }
    }
}
