using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SoftMasking {
    public interface IMaterialReplacer {
        // Determines order in which IMaterialReplacer will be called. Order of default
        // implementation is 0. If you want your function to be called before, return a
        // value lesser than 0.
        int order { get; }

        // Should return null if this replacer can't replace given material.
        Material Replace(Material material);
    }

    public static class MaterialReplacer {
        static List<IMaterialReplacer> s_globalReplacers;

        public static IEnumerable<IMaterialReplacer> globalReplacers {
            get {
                if (s_globalReplacers == null)
                    s_globalReplacers = CollectGlobalReplacers().ToList();
                return s_globalReplacers;
            }
        }

        static IEnumerable<IMaterialReplacer> CollectGlobalReplacers() {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetExportedTypes())
                .Where(t => t.IsSubclassOf(typeof(IMaterialReplacer)))
                .Select(t => (IMaterialReplacer)Activator.CreateInstance(t));
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
