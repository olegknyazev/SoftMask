using System.Collections.Generic;
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
                item.image.sprite = image;
                item.image.color = colors[i % colors.Length];
                item.title.text = string.Format("{0} {1:D2}", baseName, i);
                item.description.text = string.Format("Full description of {0} {1:D2}", baseName, i);
            }
        }

        void DestroyChildren() {
            var children = new List<Transform>();
            for (int i = 0; i < target.childCount; ++i)
                children.Add(target.GetChild(i));
            foreach (var c in children)
                DestroyImmediate(c.gameObject);
        }
    }
}
