using System;
#if HIERARCHY_PROFILING
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Profiling;
#endif
using Object = UnityEngine.Object;

namespace EnhancedHierarchy {
    /// <summary>
    /// Prevents wrong profiler samples count.
    /// Very useful for things other than Enhanced Hierarchy, Unity could implement this on its API, just saying :).
    /// </summary>
    internal class ProfilerSample : IDisposable {

        private ProfilerSample(string name, Object targetObject) {
#if HIERARCHY_PROFILING
            Profiler.BeginSample(name, targetObject);
#endif
        }

        public static ProfilerSample Get() {
            var name = (string)null;

#if HIERARCHY_PROFILING
            Profiler.BeginSample("Getting Stack Frame");
            var stack = new StackFrame(1, false);
            name = stack.GetMethod().DeclaringType.Name;
            name += ".";
            name += stack.GetMethod().Name;
            Profiler.EndSample();
#endif

            return Get(name, null);
        }

        public static ProfilerSample Get(string name) {
            return Get(name, null);
        }

        public static ProfilerSample Get(string name, Object targetObject) {
#if HIERARCHY_PROFILING
            return new ProfilerSample(name, targetObject);
#else
            return null;
#endif
        }

        public void Dispose() {
#if HIERARCHY_PROFILING
            Profiler.EndSample();
#endif
        }

    }
}