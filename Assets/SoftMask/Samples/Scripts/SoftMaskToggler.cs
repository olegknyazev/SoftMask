using UnityEngine;
using UnityEngine.UI;

namespace SoftMasking.Samples {
    public class SoftMaskToggler : MonoBehaviour {
        public GameObject viewport;
        public bool doNotTouchImage = false;

        public void Toggle(bool enabled) {
            viewport.GetComponent<SoftMask>().enabled = enabled;
            viewport.GetComponent<Mask>().enabled = !enabled;
            if (!doNotTouchImage)
                viewport.GetComponent<Image>().enabled = !enabled;
        }
    }
}
