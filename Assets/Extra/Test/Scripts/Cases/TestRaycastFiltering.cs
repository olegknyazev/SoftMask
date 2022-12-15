using System.Collections;
using UnityEngine;

namespace SoftMasking.Tests {
    public class TestRaycastFiltering : MonoBehaviour {
        public SoftMask mask;
        public StandaloneInputModuleProxy inputModule;
        public AutomatedTest automatedTest;
        public ClickCatcher clickCatcher;

        [Header("Click positions")]
        public Vector2 insideNonMaskedPosition;
        public Vector2 insideMaskedPosition;
        public Vector2 outsidePosition;

        InputStub _input;

        public IEnumerator Start() {
            OverrideInput();
            while (true) {
                for (int i = 0; i < testCases.Length; ++i) {
                    var c = testCases[i];
                    c.Setup(mask);
                    yield return PerformAndValidateClicks(
                        i,
                        c.clickInsideNonMaskedExpected,
                        c.clickInsideMaskedExpected,
                        c.clickOutsideExpected);
                }
                yield return automatedTest.Finish();
            }
        }
        
        static readonly TestCase[] testCases = new [] {
            //           inv. mask   inv. outsides   raycast thresh.     pass unmasked?   pass masked?   pass outside?
            new TestCase(false,      false,          0f,                 true,            true,          false),
            new TestCase(false,      false,          1f,                 true,            false,         false),
            new TestCase(true,       false,          0f,                 true,            true,          false),
            new TestCase(true,       false,          1f,                 false,           true,          false),
            new TestCase(false,      true,           0f,                 true,            true,          true),
            new TestCase(false,      true,           1f,                 true,            false,         true),
            new TestCase(true,       true,           0f,                 true,            true,          true),
            new TestCase(true,       true,           1f,                 false,           true,          true),
        };
        
        struct TestCase {
            public bool invertMask;
            public bool invertOutsides;
            public float raycastThreshold;

            public bool clickInsideNonMaskedExpected;
            public bool clickInsideMaskedExpected;
            public bool clickOutsideExpected;

            public TestCase(
                    bool invertMask,
                    bool invertOutsides,
                    float raycastThreshold,
                    bool clickInsideNonMaskedExpected,
                    bool clickInsideMaskedExpected,
                    bool clickOutsideExpected) {
                this.invertMask = invertMask;
                this.invertOutsides = invertOutsides;
                this.raycastThreshold = raycastThreshold;
                this.clickInsideNonMaskedExpected = clickInsideNonMaskedExpected;
                this.clickInsideMaskedExpected = clickInsideMaskedExpected;
                this.clickOutsideExpected = clickOutsideExpected;
            }

            public void Setup(SoftMask mask) {
                mask.invertMask = invertMask;
                mask.invertOutsides = invertOutsides;
                mask.raycastThreshold = raycastThreshold;
            }
        }

        void OverrideInput() {
            _input = inputModule.gameObject.AddComponent<InputStub>();
            inputModule.inputOverride = _input;
            inputModule.forceModuleActive = true;
        }

        IEnumerator PerformAndValidateClicks(int caseNumber, bool insideNonMaskedClickExpected, bool insideMaskedClickExpected, bool outsideClickExpected) {
            var stepNumber = caseNumber * 3;
            yield return PerformAndValidateClick(stepNumber, insideNonMaskedPosition, insideNonMaskedClickExpected);
            yield return PerformAndValidateClick(stepNumber + 1, insideMaskedPosition, insideMaskedClickExpected);
            yield return PerformAndValidateClick(stepNumber + 2, outsidePosition, outsideClickExpected);
        }

        IEnumerator PerformAndValidateClick(int stepNumber, Vector2 position, bool clickExpected) {
            yield return PerformClick(position);
            yield return ValidateLastClick(stepNumber, position, clickExpected);
        }

        IEnumerator PerformClick(Vector2 position) {
            _input.Set(position, InputStub.KeyState.Down);
            yield return null;
            _input.Set(position, InputStub.KeyState.Pressed);
            yield return automatedTest.Proceed(0.1f);
            _input.Set(position, InputStub.KeyState.Up);
            yield return null;
            _input.Set(position, InputStub.KeyState.Normal);
            yield return automatedTest.Proceed(0.5f);
        }

        IEnumerator ValidateLastClick(int stepNumber, Vector2 expectedPosition, bool clickExpected) {
            var lastClick = clickCatcher.ConsumeLastClick();
            var expectationMatched = 
                clickExpected && lastClick.HasValue && expectedPosition == lastClick.Value
                || !clickExpected && !lastClick.HasValue;
            if (!expectationMatched)
                yield return automatedTest.Fail(
                    clickExpected
                        ? $"At step {stepNumber} expected click {expectedPosition} but actual is {lastClick}"
                        : $"At step {stepNumber} no click expected but actual is {lastClick}");
        }
    }
}
