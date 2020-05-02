using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SoftMasking.Tests {
    [Serializable] public class LogRecord {
        [SerializeField] private string _message;
        [SerializeField] private LogType _logType;
        [SerializeField] private string _context;

        public LogRecord(string message, LogType logType, UnityEngine.Object context) {
            _message = message;
            _logType = logType;
            _context = context ? context.name : null;
        }

        public string message { get { return _message; } }
        public LogType logType { get { return _logType; } }
        public string context { get { return _context; } }
    }

    [Serializable] public class ExpectedLogRecord {
        public string messagePattern;
        public LogType logType;
        public string context;

        public ExpectedLogRecord(string message, LogType logType, UnityEngine.Object context) {
            this.messagePattern = message;
            this.logType = logType;
            this.context = context ? context.name : null;
        }
            
        public bool Match(LogRecord record) {
            return record.logType == logType
                && (record.context == context || context == null)
                && Regex.IsMatch(record.message, messagePattern);
        }

        public List<LogRecord> Filter(List<LogRecord> log) {
            var matchIndex = log.FindIndex(x => Match(x));
            if (matchIndex == -1)
                return log;
            var nextIndex = matchIndex + 1;
            var beforeMatch = log.Take(matchIndex);
            var afterMatch = log.Skip(nextIndex).Take(log.Count - nextIndex);
            return beforeMatch.Concat(afterMatch).ToList();
        }
    }
}
