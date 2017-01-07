using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SoftMasking.Samples {
    public class ImageSwitcher : MonoBehaviour {
        public RawImage image;
        public Texture2D[] textures;
        public float twistSpeed = 10.0f;
        public float twistAngle = 10.0f;
        public float delay = 2.0f;

        public IEnumerator Start() {
            var idx = 0;
            while (true) {
                // Twist in
                image.texture = textures[idx];
                yield return StartCoroutine(Twist(0.0f, twistAngle));
                // Change image
                idx = (idx + 1) % textures.Length;
                image.texture = textures[idx];
                // Twist out
                yield return StartCoroutine(Twist(twistAngle, 0.0f));
                // Delay
                yield return new WaitForSeconds(delay);
            }
        }

        IEnumerator Twist(float from, float to) {
            var twist = from;
            while (Mathf.Abs(twist - to) > 0) {
                twist = Mathf.MoveTowards(twist, to, Time.deltaTime * twistSpeed);

                // It's not good to modify shared material of Image, we do it just to keep sample simple.
                image.material.SetFloat("_TwistAngle", twist);

                // We obliged to notify Unity UI system that out material is changed. Without this
                // new properties would not be applied, if there are some IMaterialModifier active
                // on out Graphic. Sadly, but SetMaterialDirty() doesn't help standard Unity Mask :-(
                image.SetMaterialDirty();

                yield return null;
            }
        }
    }
}
