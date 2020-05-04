using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

namespace SoftMasking.Tests {
    [Serializable] public class CapturedStep {
        [SerializeField] Texture2D _texture;
        [SerializeField] List<LogRecord> _logRecords;

        public CapturedStep(Texture2D texture) : this(texture, null) {}
        public CapturedStep(Texture2D texture, IEnumerable<LogRecord> log) {
            _texture = texture;
            _logRecords = log != null ? log.ToList() : new List<LogRecord>();
        }

        public Texture2D texture { get { return _texture; } }
        public IEnumerable<LogRecord> logRecords { get { return _logRecords; } }
        public bool hasLog { get { return _logRecords.Count > 0; } }
    }
}
