using System;
using System.Collections.Generic;
using System.Linq;
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
                parts[2],
                (LogType)Enum.Parse(typeof(LogType), parts[1]),
                parts[0]);
        }

        static readonly string separator = "::";
        static readonly string[] separators = new [] { separator };

        public override bool Equals(object obj) {
            var that = obj as LogRecord;
            if (that == null)
                return false;
            return this.context == that.context
                && this.logType == that.logType
                && this.message == that.message;
        }

        public override int GetHashCode() {
            int hash = 17;
            hash = hash * 31 + context.GetHashCode();
            hash = hash * 31 + logType.GetHashCode();
            hash = hash * 31 + message.GetHashCode();
            return hash;
        }
    }

    public static class LogRecords {
        public static IEnumerable<LogRecord> Parse(string content) {
            var lines = content.Split(new [] { "\n" }, StringSplitOptions.None);
            return lines.Select(line => LogRecord.FromString(line));
        }

        public static string Format(IEnumerable<LogRecord> log) {
            return string.Join("\n", log.Select(x => x.ToString()).ToArray());
        }

        public struct DiffResult {
            public readonly List<LogRecord> missing;
            public readonly List<LogRecord> extra;
            public DiffResult(IEnumerable<LogRecord> missing, IEnumerable<LogRecord> extra) {
                this.missing = missing.ToList();
                this.extra = extra.ToList();
            }
        }
        
        public static DiffResult Diff(IEnumerable<LogRecord> expected, IEnumerable<LogRecord> actual) {
            var missing = expected.ToList();
            foreach (var a in actual)
                missing.Remove(a);
            var extra = actual.ToList();
            foreach (var e in expected)
                extra.Remove(e);
            return new DiffResult(missing, extra);
        }
    }
}
