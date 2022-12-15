using System;
using UnityEngine;
using UnityEngine.UI;

namespace SoftMasking.Tests {
    [RequireComponent(typeof(Button))]
    public class PushToggle : MonoBehaviour {
        public object[] options;
        public Func<object, string> formatText = x => x != null ? x.ToString() : "<N/A>";
        public Text caption;

        public event Action<PushToggle, int> optionChoosen;

        public int optionIndex { get; private set; }

        public object option => options.Length > 0 ? options[optionIndex] : null;

        public bool ToggleNext() {
            var overflow = false;
            optionIndex += 1;
            if (optionIndex >= options.Length) {
                optionIndex %= options.Length;
                overflow = true;
            }
            UpdateCaption();
            optionChoosen.InvokeSafe(this, optionIndex);
            return overflow;
        }

        public void Start() {
            GetComponent<Button>().onClick.AddListener(OnClick);
            UpdateCaption();
        }

        void OnClick() {
            ToggleNext();
        }

        void UpdateCaption() {
            caption.text = formatText(option);
        }
    }
}
