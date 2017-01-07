using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace SoftMasking.Samples {
    public class SoftMaskToggler : MonoBehaviour {
        [FormerlySerializedAs("viewport")] public GameObject mask;
        public bool doNotTouchImage = false;

        public void Toggle(bool enabled) {
            mask.GetComponent<SoftMask>().enabled = enabled;
            mask.GetComponent<Mask>().enabled = !enabled;
            if (!doNotTouchImage)
                mask.GetComponent<Image>().enabled = !enabled;
        }
    }
}
