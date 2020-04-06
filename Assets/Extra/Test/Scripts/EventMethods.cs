using System;

namespace SoftMasking.Tests {
    public static class EventMethods {
        public static void InvokeSafe<A0>(this Action<A0> action, A0 a0) {
            if (action != null) action(a0); 
        }
        public static void InvokeSafe<A0, A1>(this Action<A0, A1> action, A0 a0, A1 a1) {
            if (action != null) action(a0, a1); 
        }
    }
}
