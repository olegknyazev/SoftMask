using System.Collections;
using UnityEngine;
using TMPro;
using SoftMasking.Tests;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoftMasking.TextMeshPro.Tests {
    public class TestTextSelection : MonoBehaviour {
        public TMP_InputField inputField;
        public AutomatedTest automatedTest;

        public IEnumerator Start() {
            while (true) {
                EventSystem.current.SetSelectedGameObject(inputField.gameObject);
                yield return automatedTest.Proceed(0.5f);
                SelectText(5, 30);
                yield return automatedTest.Proceed(0.5f);
                SelectText(57, 15);
                yield return automatedTest.Proceed(0.5f);
                SelectText(72, 40);
                yield return automatedTest.Proceed(0.5f);
                SelectText(30, 0);
                yield return automatedTest.Proceed(0.5f);
                yield return automatedTest.Finish();
            }
        }

        void SelectText(int startIndex, int length) {
            inputField.selectionStringAnchorPosition = startIndex;
            inputField.selectionStringFocusPosition = startIndex + length;
            CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(inputField);
        }
    }
}
