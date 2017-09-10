using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SoftMasking {
    [AttributeUsage(AttributeTargets.Class)]
    public class GlobalMaterialReplacerAttribute : Attribute { }

    public interface IMaterialReplacer {
        // Determines order in which IMaterialReplacer will be called. Order of default
        // implementation is 0. If you want your function to be called before, return a
        // value lesser than 0.
        int order { get; }

        // Should return null if this replacer can't replace given material.
        Material Replace(Material material);
    }

    public static class MaterialReplacer {
        static List<IMaterialReplacer> _globalReplacers;

        public static IEnumerable<IMaterialReplacer> globalReplacers {
            get {
                if (_globalReplacers == null)
                    _globalReplacers = CollectGlobalReplacers().ToList();
                return _globalReplacers;
            }
        }

        static IEnumerable<IMaterialReplacer> CollectGlobalReplacers() {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetExportedTypes())
                .Where(t => !t.IsAbstract)
                .Where(t => t.GetCustomAttributes(typeof(GlobalMaterialReplacerAttribute), false).Length > 0)
                .Where(t => typeof(IMaterialReplacer).IsAssignableFrom(t))
                .Select(t => TryCreateInstance(t))
                .Where(t => t != null);
        }

        static IMaterialReplacer TryCreateInstance(Type t) {
            try {
                return (IMaterialReplacer)Activator.CreateInstance(t);
            } catch (Exception ex) {
                Debug.LogErrorFormat("Could not create instance of {0}: {1}", t.Name, ex);
                return null;
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
            order = _replacers.Min(x => x.order);
            _replacers.Sort((a, b) => a.order.CompareTo(b.order));
        }
    }
}
