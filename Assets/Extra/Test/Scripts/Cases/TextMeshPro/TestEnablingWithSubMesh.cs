using System.Collections;
using UnityEngine;
using SoftMasking.Tests;
using TMPro;

namespace SoftMasking.TextMeshPro.Tests {
    public class TestEnablingWithSubMesh : MonoBehaviour {
        public TextMeshProUGUI textMesh;
        public string textToSet;
        public AutomatedTest automatedTest;

        public IEnumerator Start() {
            yield return automatedTest.Proceed(1f);
            textMesh.text = textToSet;
            textMesh.gameObject.SetActive(true);
            yield return automatedTest.Proceed(1f);
            yield return automatedTest.Finish();
        }
    }
}
