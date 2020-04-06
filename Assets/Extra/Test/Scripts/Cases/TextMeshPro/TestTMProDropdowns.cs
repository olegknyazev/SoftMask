using System.Collections;
using UnityEngine;
using TMPro;
using SoftMasking.Tests;

namespace SoftMasking.TextMeshPro.Tests {
    public class TestTMProDropdowns : MonoBehaviour {
        public TMP_Dropdown[] dropdowns;
        public AutomatedTest automatedTest;

        public IEnumerator Start() {
            var waitForTMProAnimation = new WaitForSeconds(0.2f);
            while (true) {
                foreach (var dropdown in dropdowns) {
                    yield return automatedTest.Proceed(0.5f);
                    dropdown.Show();
                    yield return waitForTMProAnimation;
                    yield return automatedTest.Proceed(0.5f);
                    dropdown.Hide();
                    yield return waitForTMProAnimation;
                    yield return automatedTest.Proceed(0.5f);
                }
                yield return automatedTest.Finish();
            }
        }
    }
}
