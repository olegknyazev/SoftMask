using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if !NET_STANDARD_2_0
using System.Reflection.Emit;
#endif
using UnityEngine;

namespace SoftMasking {
    /// <summary>
    /// Mark an implementation of the IMaterialReplacer interface with this attribute to
    /// register it in the global material replacer chain. The global replacers will be
    /// used automatically by all SoftMasks.
    ///
    /// Globally registered replacers are called in order of ascending of their `order`
    /// value. The traversal is stopped on the first IMaterialReplacer which returns a
    /// non-null value and the returned value is the replacement being used.
    ///
    /// Implementation of IMaterialReplacer marked by this attribute should have a
    /// default constructor.
    ///
    /// NOTE You may also need to mark global IMaterialReplacer by Unity's
    /// <see cref="UnityEngine.Scripting.PreserveAttribute"/> if you're using IL2CPP to
    /// prevent code stripping.
    /// </summary>
    /// <seealso cref="IMaterialReplacer"/>
    /// <seealso cref="MaterialReplacer.globalReplacers"/>
    [AttributeUsage(AttributeTargets.Class)]
    public class GlobalMaterialReplacerAttribute : Attribute { }

    /// <summary>
    /// Used by SoftMask to automatically replace materials which don't support
    /// Soft Mask by those that do.
    /// </summary>
    /// <seealso cref="GlobalMaterialReplacerAttribute"/>
    public interface IMaterialReplacer {
        /// <summary>
        /// Determines the mutual order in which IMaterialReplacers will be called.
        /// The lesser the return value, the earlier it will be called, that is,
        /// replacers are sorted by ascending of the `order` value.
        /// The order of default implementation is 0. If you want your function to be
        /// called before, return a value lesser than 0.
        /// </summary>
        int order { get; }

        /// <summary>
        /// Should return null if this replacer can't replace the given material.
        /// </summary>
        Material Replace(Material material);
    }

    public static class MaterialReplacer {
        static List<IMaterialReplacer> _globalReplacers;

        /// <summary>
        /// Returns the collection of all globally registered replacers.
        /// </summary>
        /// <seealso cref="GlobalMaterialReplacerAttribute"/>
        public static IEnumerable<IMaterialReplacer> globalReplacers {
            get {
                if (_globalReplacers == null)
                    _globalReplacers = CollectGlobalReplacers().ToList();
                return _globalReplacers;
            }
        }

        static IEnumerable<IMaterialReplacer> CollectGlobalReplacers() {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypesSafe())
                .Where(IsMaterialReplacerType)
                .Select(TryCreateInstance)
                .Where(t => t != null);
        }

        static bool IsMaterialReplacerType(Type t) {
        #if NET_STANDARD_2_0
            var isTypeBuilder = false;
        #else
            var isTypeBuilder = t is TypeBuilder;
        #endif
            return !isTypeBuilder
                && !t.IsAbstract
                && t.IsDefined(typeof(GlobalMaterialReplacerAttribute), false)
                && typeof(IMaterialReplacer).IsAssignableFrom(t);
        }

        static IMaterialReplacer TryCreateInstance(Type t) {
            try {
                return (IMaterialReplacer)Activator.CreateInstance(t);
            } catch (Exception ex) {
                Debug.LogErrorFormat("Could not create instance of {0}: {1}", t.Name, ex);
                return null;
            }
        }

        static IEnumerable<Type> GetTypesSafe(this Assembly asm) {
            try {
                return asm.GetTypes();
            } catch (ReflectionTypeLoadException e) {
                return e.Types.Where(t => t != null);
            }
        }
    }

    public class MaterialReplacerChain : IMaterialReplacer {
        readonly List<IMaterialReplacer> _replacers;

        public MaterialReplacerChain(IEnumerable<IMaterialReplacer> replacers, IMaterialReplacer yetAnother) {
            _replacers = replacers.ToList();
            _replacers.Add(yetAnother);
            Initialize();
        }

        public int order { get; private set; }

        public Material Replace(Material material) {
            for (int i = 0; i < _replacers.Count; ++i) {
                var result = _replacers[i].Replace(material);
                if (result != null)
                    return result;
            }
            return null;
        }

        void Initialize() {
            _replacers.Sort((a, b) => a.order.CompareTo(b.order));
            order = _replacers[0].order;
        }
    }
}
