using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SoftMasking.Tests {
    public class TestCustomReplacerOrdering : MonoBehaviour {
        public Text output;
        public AutomatedTest automatedTest;

        public IEnumerator Start() {
            yield return null;
            if (output)
                output.text = "Last calls: " +
                    string.Join(", ", CustomReplacer.lastCalls.Select(x => x.ToString()).ToArray());
            yield return automatedTest.Proceed();
            yield return automatedTest.Finish();
        }

        class CustomReplacer : IMaterialReplacer {
            static readonly Queue<int> s_lastCalls = new Queue<int>();

            public CustomReplacer(int order) { this.order = order; }

            public int order { get; private set; }

            public Material Replace(Material material) {
                s_lastCalls.Enqueue(order);
                while (s_lastCalls.Count > 5)
                    s_lastCalls.Dequeue();
                return null;
            }

            public static IEnumerable<int> lastCalls { get { return s_lastCalls; } }
        }

        [GlobalMaterialReplacer]
        class CustomReplacer1 : CustomReplacer {
            public CustomReplacer1() : base(-5) { }
        }

        [GlobalMaterialReplacer]
        class CustomReplacer2 : CustomReplacer {
            public CustomReplacer2() : base(0) { }
        }

        [GlobalMaterialReplacer]
        class CustomReplacer3 : CustomReplacer {
            public CustomReplacer3() : base(14) { }
        }

        [GlobalMaterialReplacer]
        class CustomReplacer4 : CustomReplacer {
            public CustomReplacer4() : base(7) { }
        }
    }
}
