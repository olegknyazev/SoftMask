using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SoftMasking.Samples {
    public class ImageSwitcher : MonoBehaviour {
        public RawImage image;
        public Texture2D[] textures;
        public float twistSpeed = 10.0f;
        public float twist = 10.0f;
        public float pause = 2.0f;

        public IEnumerator Start() {
            var idx = 0;
            while (true) {
                image.texture = textures[idx];
                yield return StartCoroutine(Twist(0.0f, twist, twistSpeed));
                idx = (idx + 1) % textures.Length;
                image.texture = textures[idx];
                yield return StartCoroutine(Twist(twist, 0.0f, twistSpeed));
                yield return new WaitForSeconds(pause);
            }
        }

        IEnumerator Twist(float from, float to, float speed) {
            var twist = from;
            while (Mathf.Abs(twist - to) > 0) {
                twist = Mathf.MoveTowards(twist, to, Time.deltaTime * speed);
                // It's not good to modify shared material of Image, we do it just to keep sample simple.
                image.material.SetFloat("_TwistAngle", twist);
                yield return null;
            }
        }
    }
}
