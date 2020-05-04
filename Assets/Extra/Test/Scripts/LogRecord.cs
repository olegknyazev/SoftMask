using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Assertions;

namespace SoftMasking.Tests {
    [Serializable] public class LogRecord {
        [SerializeField] private string _message;
        [SerializeField] private LogType _logType;
        [SerializeField] private string _context;

        public LogRecord(string message, LogType logType, UnityEngine.Object context)
            : this(message, logType, context ? context.name : null) 
        {}
        
        LogRecord(string message, LogType logType, string context) {
            _message = message;
            _logType = logType;
            _context = context;
        }

        public string message { get { return _message; } }
        public LogType logType { get { return _logType; } }
        public string context { get { return _context; } }

        public override string ToString() {
            return string.Format("{1} {0} {2} {0} {3}", separator, context, logType, message);
        }
        
        public static LogRecord FromString(string str) {
            var parts = str.Split(separators, StringSplitOptions.None).Select(x => x.Trim()).ToList();
            Assert.AreEqual(3, parts.Count);
            return new LogRecord(
                parts[0],
                (LogType)Enum.Parse(typeof(LogType), parts[1]),
                parts[2]);
        }
        
        public static IEnumerable<LogRecord> ParseAll(string content) {
            var lines = content.Split(new [] { "\n" }, StringSplitOptions.None);
            return lines.Select(line => FromString(line));
        }

        public static string FormatAll(IEnumerable<LogRecord> log) {
            return string.Join("\n", log.Select(x => x.ToString()).ToArray());
        }

        static readonly string separator = "::";
        static readonly string[] separators = new [] { separator };
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
