using System.Collections;
using System.Linq;
using UnityEngine;

namespace SoftMasking.Tests {
    public class TestMoveMask : MonoBehaviour {
        public RectTransform mask;
        public RectTransform[] immovables;

        public IEnumerator Start() {
            var originalPositions = immovables.Select(x => x.position).ToArray();
            var angle = 0.0f;
            while (true) {
                angle += Time.deltaTime;
                mask.anchoredPosition = new Vector2(Mathf.Cos(angle) * 100.0f, Mathf.Sin(angle) * 100.0f);
                for (int i = 0; i < immovables.Length; ++i)
                    immovables[i].transform.position = originalPositions[i];
                yield return null;
            }
        }
    }
}
