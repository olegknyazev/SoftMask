using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EnhancedHierarchy {
    /// <summary>
    /// Log Entries from the console, to check if a gameobject has any errors or warnings.
    /// </summary>
    internal class LogEntry {

        private object referenceEntry;

        public string Condition { get { return (string)logEntryFields["condition"].GetValue(referenceEntry); } }
        public int ErrorNum { get { return (int)logEntryFields["errorNum"].GetValue(referenceEntry); } }
        public string File { get { return (string)logEntryFields["file"].GetValue(referenceEntry); } }
        public int Line { get { return (int)logEntryFields["line"].GetValue(referenceEntry); } }
        public EntryMode Mode { get { return (EntryMode)logEntryFields["mode"].GetValue(referenceEntry); } }
        public int InstanceID { get { return (int)logEntryFields["instanceID"].GetValue(referenceEntry); } }
        public int Identifier { get { return (int)logEntryFields["identifier"].GetValue(referenceEntry); } }
        public int IsWorldPlaying { get { return (int)logEntryFields["isWorldPlaying"].GetValue(referenceEntry); } }
        public Object Obj { get { return InstanceID == 0 ? null : EditorUtility.InstanceIDToObject(InstanceID); } }

        public static Dictionary<GameObject, List<LogEntry>> ReferencedObjects { get; private set; }

        private static bool needLogReload;
        private readonly static Dictionary<string, FieldInfo> logEntryFields;

        private static readonly MethodInfo getEntryMethod;
        private static readonly MethodInfo startMethod;
        private static readonly MethodInfo endMethod;
        private static readonly ConstructorInfo logEntryConstructor;

        static LogEntry() {
            try {
                var logEntriesType = typeof(Editor).Assembly.GetType("UnityEditorInternal.LogEntries", false);
                var logEntryType = typeof(Editor).Assembly.GetType("UnityEditorInternal.LogEntry", false);

                if(logEntriesType == null)
                    logEntriesType = typeof(Editor).Assembly.GetType("UnityEditor.LogEntries", false);
                if(logEntryType == null)
                    logEntryType = typeof(Editor).Assembly.GetType("UnityEditor.LogEntry", false);

                getEntryMethod = logEntriesType.GetMethod("GetEntryInternal", ReflectionHelper.FullBinding);
                startMethod = logEntriesType.GetMethod("StartGettingEntries", ReflectionHelper.FullBinding);
                endMethod = logEntriesType.GetMethod("EndGettingEntries", ReflectionHelper.FullBinding);
                logEntryConstructor = logEntryType.GetConstructor(new Type[0]);
                logEntryFields = new Dictionary<string, FieldInfo>();

                foreach(var field in logEntryType.GetFields())
                    logEntryFields.Add(field.Name, field);

                ReloadReferences();
            }
            catch(Exception e) {
                Debug.LogException(e);
                Preferences.ForceDisableButton(new Icons.Warnings());
            }

#if UNITY_5 || UNITY_2017
            Application.logMessageReceivedThreaded += (logString, stackTrace, type) => needLogReload = true;
#else
			needLogReload = true;
#endif
            EditorApplication.update += () => {
                if(needLogReload && Preferences.IsButtonEnabled(new Icons.Warnings()) && Preferences.Enabled) {
                    ReloadReferences();
#if UNITY_5 || UNITY_2017
                    needLogReload = false;
#endif
                }
            };
        }

        private LogEntry(object referenceEntry) {
            this.referenceEntry = referenceEntry;
        }

        private static void ReloadReferences() {
            ReferencedObjects = new Dictionary<GameObject, List<LogEntry>>();

            try {
                var count = (int)startMethod.Invoke(null, null);

                for(var i = 0; i < count; i++) {
                    var logEntry = logEntryConstructor.Invoke(null);
                    var entry = new LogEntry(logEntry);
                    var go = (GameObject)null;

                    getEntryMethod.Invoke(null, new object[] { i, logEntry });

                    if(entry.Obj)
                        if(entry.Obj is GameObject)
                            go = entry.Obj as GameObject;
                        else if(entry.Obj is Component)
                            go = (entry.Obj as Component).gameObject;

                    if(go) {
                        if(ReferencedObjects.ContainsKey(go))
                            ReferencedObjects[go].Add(entry);
                        else
                            ReferencedObjects.Add(go, new List<LogEntry>() { entry });
                    }
                }

                EditorApplication.RepaintHierarchyWindow();
            }
            catch(Exception e) {
                Debug.LogException(e);
                Preferences.ForceDisableButton(new Icons.Warnings());
            }
            finally {
                if(endMethod != null)
                    endMethod.Invoke(null, null);
            }
        }

        public bool HasMode(EntryMode mode) {
            return (Mode & mode) != 0;
        }

        public override string ToString() {
            return Condition;
        }

    }
}