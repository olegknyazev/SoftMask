using UnityEngine.EventSystems;

namespace SoftMasking.Tests {
    public class StandaloneInputModuleProxy : StandaloneInputModule {
        // StandaloneInputModule has this method in 2017.1.
        // In 5.1 we use a derived proxy to override input.
        public BaseInput inputOverride {
            get { return m_InputOverride; }
            set { m_InputOverride = value; }
        }
    }
}
