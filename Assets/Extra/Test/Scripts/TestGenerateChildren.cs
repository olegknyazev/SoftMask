using UnityEngine;
using Random = System.Random;

namespace SoftMasking.Tests {
    public class TestGenerateChildren : MonoBehaviour {
        public RectTransform root;
        public int childrenToGenerate;

        Random random = new Random();

        public void Start() {
            var random = new System.Random();
            var existingCount = root.childCount;
            for (int i = 0; i < childrenToGenerate; ++i) {
                var prototype = root.GetChild(random.Next() % existingCount);
                var instance = Instantiate(prototype.gameObject);
                instance.transform.localPosition = RandomPosition();
                instance.transform.localRotation = RandomRotation();
                instance.transform.SetParent(root, false);
            }
        }

        Vector3 RandomPosition() {
            var field = root.rect;
            return new Vector3(
                (float)(random.NextDouble() * field.width) + field.x, 
                (float)(random.NextDouble() * field.height) + field.y);
        }

        Quaternion RandomRotation() {
            return Quaternion.AngleAxis((float)(random.NextDouble() * 360), Vector3.forward);
        }
    }
}
