using UnityEngine;
using UnityEngine.EventSystems;

namespace SoftMasking.Tests {
    public class ClickCatcher : MonoBehaviour, IPointerClickHandler {
        Vector2? _lastClick;

        public Vector2? ConsumeLastClick() {
            var result = _lastClick;
            _lastClick = null;
            return result;
        }

        public void OnPointerClick(PointerEventData eventData) {
            _lastClick = eventData.position;
        }
    }
}
