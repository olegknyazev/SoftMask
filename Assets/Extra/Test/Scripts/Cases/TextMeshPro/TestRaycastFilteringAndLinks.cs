using UnityEngine;
using UnityEngine.UI;

namespace SoftMasking.TextMeshPro.Tests {
    public class TestRaycastFilteringAndLinks : MonoBehaviour {
        public Transform mask;
        public Text characterText;
        public Text wordText;
        public Text linkText;

        public void OnCharacterSelected(char ch, int index) {
            characterText.text = "Char: " + ch;
        }

        public void OnWordSelection(string word, int start, int length) {
            wordText.text = "Word: " + word;
        }

        public void OnLinkSelection(string linkId, string linkTxt, int linkIndex) {
            linkText.text = "Link: " + linkId;
        }

        public void SetRotation(float rotation) {
            mask.rotation = Quaternion.AngleAxis(rotation, Vector3.forward);
        }
    }
}
