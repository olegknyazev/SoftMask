using UnityEngine;
using UnityEngine.EventSystems;

namespace SoftMasking.Tests {
    public class InputStub : BaseInput {
        public enum KeyState { Normal, Down, Pressed, Up }

        KeyState _buttonState;
        Vector2 _mousePosition;

        public override bool GetMouseButtonDown(int button) {
            return button == 0 ? _buttonState == KeyState.Down : false;
        }

        public override bool GetMouseButtonUp(int button) {
            return button == 0 ? _buttonState == KeyState.Up : false;
        }

        public override bool GetMouseButton(int button) {
            return button == 0 ? _buttonState != KeyState.Normal : false;
        }

        public override Vector2 mousePosition {
            get { return _mousePosition; }
        }

        public override bool mousePresent {
            get { return true; }
        }

        public override bool touchSupported {
            get { return false; }
        }

        public void Set(Vector2 mousePosition, KeyState buttonState) {
            _mousePosition = mousePosition;
            _buttonState = buttonState;
        }
    }
}
