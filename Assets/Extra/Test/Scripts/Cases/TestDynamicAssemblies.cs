using System;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace SoftMasking.Tests {
    public class TestDynamicAssemblies : MonoBehaviour {
        public GameObject maskPlaceholder;
        public Shader defaultMaskShader;

        [Space]
        public bool createType;

        void Awake() {
            CreateAssembly();
            CreateSoftMask();
        }

        void CreateSoftMask() {
            var sm = maskPlaceholder.AddComponent<SoftMask>();
            sm.defaultShader = defaultMaskShader;
            sm.channelWeights = MaskChannel.gray;
        }

        void CreateAssembly() {
            var name = new AssemblyName("TestDynamicAssemblies");
            var asm =
                AppDomain.CurrentDomain.DefineDynamicAssembly(
                    name,
                    AssemblyBuilderAccess.Save);
            var module = asm.DefineDynamicModule(name.Name);
            var type =
                module.DefineType(
                    "TestType",
                    TypeAttributes.Public,
                    typeof(System.Object),
                    new[] { typeof(IMaterialReplacer) });
            type.SetCustomAttribute(
                new CustomAttributeBuilder(
                    typeof(GlobalMaterialReplacerAttribute).GetConstructor(new Type[] {}),
                    new object[] { }));
            var publicVirtual = MethodAttributes.Public | MethodAttributes.Virtual;
            var replace =
                type.DefineMethod(
                    "Replace",
                    publicVirtual | MethodAttributes.ReuseSlot,
                    typeof(Material),
                    new[] { typeof(Material) });
            var gen = replace.GetILGenerator();
            gen.Emit(OpCodes.Ldstr, "Hello from dynamic assembly!");
            gen.Emit(OpCodes.Call, typeof(Debug).GetMethod("Log", new [] { typeof(string) }));
            gen.Emit(OpCodes.Ldnull);
            gen.Emit(OpCodes.Ret);
            var order = type.DefineProperty("order", PropertyAttributes.HasDefault, typeof(int), null);
            var get_order =
                type.DefineMethod(
                    "get_order",
                    publicVirtual | MethodAttributes.HideBySig | MethodAttributes.SpecialName,
                    typeof(int),
                    new Type[] {});
            var gen2 = get_order.GetILGenerator();
            gen2.Emit(OpCodes.Ldc_I4, -1);
            gen2.Emit(OpCodes.Ret);
            order.SetGetMethod(get_order);

            // The point of leaving type uncreated is that it causes reflection methods like
            // GetMethod() and IsDefined() to throw an exception.
            if (createType)
                type.CreateType();
        }
    }
}
