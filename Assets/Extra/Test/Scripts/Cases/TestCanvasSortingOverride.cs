using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SoftMasking.Tests {
    public class TestCanvasSortingOverride : MonoBehaviour {
        [Header("Objects")]
        public GameObject rootPanel;
        public GameObject nestedPanel;
        public Canvas nestedCanvas;

        [Header("Toggles")]
        public PushToggle rootPanelToggle;
        public PushToggle nestedPanelToggle;
        public PushToggle nestedCanvasToggle;

        [Space]
        public Shader shader;

        [Space]
        public AutomatedTest automatedTest;

        static readonly object[] SORTING_OVERRIDES = { false, true };
        static readonly Type[] MASK_TYPES = { typeof(Mask), typeof(RectMask2D), typeof(SoftMask) };

        public void Awake() {
            InitToggle(rootPanelToggle, MASK_TYPES, x => ((Type)x).Name);
            InitToggle(nestedPanelToggle, MASK_TYPES, x => ((Type)x).Name);
            InitToggle(
                nestedCanvasToggle,
                SORTING_OVERRIDES,
                x => (bool)x ? "Override" : "Do not override");
        }

        void InitToggle(PushToggle toggle, object[] maskTypes, Func<object, string> formatText) {
            toggle.options = maskTypes;
            toggle.formatText = formatText;
            toggle.optionChoosen += (_, __) => Apply();
        }

        public IEnumerator Start() {
            while (true) {
                Apply();
                yield return automatedTest.Proceed(1f);
                if (nestedCanvasToggle.ToggleNext())
                    if (rootPanelToggle.ToggleNext())
                        if (nestedPanelToggle.ToggleNext())
                            break;
            }
            yield return automatedTest.Finish();
        }

        void Apply() {
            SetMaskType(rootPanel, (Type)rootPanelToggle.option);
            SetMaskType(nestedPanel, (Type)nestedPanelToggle.option);
            nestedCanvas.overrideSorting = (bool)nestedCanvasToggle.option;
        }

        void SetMaskType(GameObject obj, Type type) {
            foreach (var t in MASK_TYPES.Where(x => x != type)) RemoveMask(obj, t);
            foreach (var t in MASK_TYPES.Where(x => x == type)) AddMask(obj, t);
        }

        void RemoveMask(GameObject obj, Type type) {
            var mask = obj.GetComponent(type);
            if (mask) DestroyImmediate(mask);
        }

        void AddMask(GameObject obj, Type type) {
            var mask = obj.GetComponent(type) ?? obj.AddComponent(type);
            var softMask = mask as SoftMask;
            if (softMask) {
                softMask.defaultShader = shader;
                softMask.enabled = true; // might been disabled in case of nested SoftMasks
            }
        }
    }
}
