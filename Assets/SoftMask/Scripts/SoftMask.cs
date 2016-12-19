using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoftMask {
    [ExecuteInEditMode]
    [AddComponentMenu("UI/Soft Mask", 14)]
    public class SoftMask : UIBehaviour {
        protected virtual void Update() {
            SpawnMaskablesInChildren();
        }

        void SpawnMaskablesInChildren() {
            foreach (var g in transform.GetComponentsInChildren<Graphic>())
                if (!g.GetComponent<SoftMaskable>())
                    g.gameObject.AddComponent<SoftMaskable>();
        }
    }
}
