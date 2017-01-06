using UnityEngine;
using UnityEngine.UI;

namespace SoftMasking.Samples {
    public class SoftMaskToggler : MonoBehaviour {
        public GameObject viewport;

        public void Toggle(bool enabled) {
            viewport.GetComponent<SoftMask>().enabled = enabled;
            viewport.GetComponent<Image>().enabled = !enabled;
            viewport.GetComponent<Mask>().enabled = !enabled;
        }
    }
}
