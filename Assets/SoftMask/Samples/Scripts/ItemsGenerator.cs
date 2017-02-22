using UnityEngine;

namespace SoftMasking.Samples {
    public class ItemsGenerator : MonoBehaviour {
        public RectTransform target;
        public Sprite image;
        public int count;
        public string baseName;
        public Item itemPrefab;

        static Color[] colors = new[] {
            Color.red,
            Color.green,
            Color.blue,
            Color.cyan,
            Color.yellow,
            Color.magenta,
            Color.gray
        };

        public void Generate() {
            DestroyChildren();
            for (int i = 0; i < count; ++i) {
                var item = Instantiate(itemPrefab);
                item.transform.SetParent(target, false);
                item.Set(
                    string.Format("{0} {1:D2}", baseName, i), 
                    image, 
                    colors[i % colors.Length], 
                    Random.Range(0.4f, 1), 
                    Random.Range(0.4f, 1));
            }
        }

        void DestroyChildren() {
            while (target.childCount > 0)
                DestroyImmediate(target.GetChild(0).gameObject);
        }
    }
}
