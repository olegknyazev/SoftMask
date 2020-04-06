using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using UnityEngine;
using System.Linq;

namespace SoftMasking.Tests {
    public class LogRecord {
        public readonly string message;
        public readonly LogType logType;
        public readonly UnityEngine.Object context;

        public LogRecord(string message, LogType logType, UnityEngine.Object context) {
            this.message = message;
            this.logType = logType;
            this.context = context;
        }
    }

    [Serializable] public class ExpectedLogRecord {
        public string messagePattern;
        public LogType logType;
        public UnityEngine.Object context;

        public ExpectedLogRecord(string message, LogType logType, UnityEngine.Object context) {
            this.messagePattern = message;
            this.logType = logType;
            this.context = context;
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
