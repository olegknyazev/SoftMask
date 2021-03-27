using System.Collections;
using TMPro;
using UnityEngine;

namespace SoftMasking.Tests {
    public class TestMultifontTMPro : MonoBehaviour {
        public TextMeshProUGUI textMeshPro;
        [Multiline] public string textToApply; 
            
        public AutomatedTest automatedTest;

        public IEnumerator Start() {
            yield return automatedTest.Proceed();
            textMeshPro.text = textToApply;
            yield return automatedTest.Proceed();
            yield return automatedTest.Finish();
        }
    }
}
