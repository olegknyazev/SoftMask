using UnityEngine;
using UnityEngine.UI;

namespace SoftMasking.Samples {
    public class TextVanishing : MonoBehaviour {
        public GameObject viewport;

        public void ToggleSoftMask(bool enabled) {
            viewport.GetComponent<SoftMask>().enabled = enabled;
            viewport.GetComponent<Image>().enabled = !enabled;
            viewport.GetComponent<Mask>().enabled = !enabled;
        }
    }
}
