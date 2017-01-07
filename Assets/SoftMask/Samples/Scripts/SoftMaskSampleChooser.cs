using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SoftMasking.Samples {
    public class SoftMaskSampleChooser : MonoBehaviour {
        public Dropdown dropdown;

        public void Start() {
            var activeSceneName = SceneManager.GetActiveScene().name;
            dropdown.value = dropdown.options.FindIndex(x => x.text == activeSceneName);
            dropdown.onValueChanged.AddListener(Choose);
        }

        public void Choose(int sampleIndex) {
            var sceneName = dropdown.options[sampleIndex].text;
            SceneManager.LoadScene(sceneName);
        }
    }
}
