using System;
using System.Reflection;
using UnityEngine.Assertions;
#if UNITY_EDITOR
using UnityEditor;
#else
using UnityEngine;
#endif

namespace SoftMasking.Tests {
    public static class ResolutionUtility {
        static int setCallCount = 0;

        const int testWidth = 1024;
        const int testHeight = 768;

    #if UNITY_EDITOR
        static int customSizeIndex = -1;
        static object gameViewSizesInstance;
        static MethodInfo getGroup;

        static ResolutionUtility() {
            // This method reflectively does following:
            //  gameViewSizesInstance = ScriptableSingleton<GameViewSizes>.instance;
            var sizesType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSizes");
            var singleType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var instanceProp = singleType.GetProperty("instance");
            getGroup = sizesType.GetMethod("GetGroup");
            gameViewSizesInstance = instanceProp.GetValue(null, null);
        }
    #endif

        public static void SetTestResolution() {
            if (++setCallCount == 1) {
            #if UNITY_EDITOR
                Assert.AreEqual(-1, customSizeIndex);
                customSizeIndex = 
                    AddCustomSize(
                        GameViewSizeType.FixedResolution, 
                        GameViewSizeGroupType.Standalone, 
                        testWidth,
                        testHeight, 
                        "Soft Mask Test");
                SetSize(customSizeIndex);
            #else
                Screen.SetResolution(testWidth, testHeight, false);
            #endif
            }
        }

        public static void RevertTestResolution() {
            if (--setCallCount == 0) {
            #if UNITY_EDITOR
                Assert.AreNotEqual(-1, customSizeIndex);
                RemoveCustomSize(GameViewSizeGroupType.Standalone, customSizeIndex);
                SetSize(0);
                customSizeIndex = -1;
            #endif
            }
        }
       
    #if UNITY_EDITOR
        static object GetGroup(GameViewSizeGroupType type) {
            return getGroup.Invoke(gameViewSizesInstance, new object[] { (int)type });
        }
        
        enum GameViewSizeType { AspectRatio, FixedResolution }

        static int AddCustomSize(
                GameViewSizeType viewSizeType,
                GameViewSizeGroupType sizeGroupType,
                int width,
                int height,
                string text) {
            // This method reflectively does the following:
            //  GameViewSizes group = gameViewSizesInstance.GetGroup(sizeGroupTyge);
            //  group.AddCustomSize(new GameViewSize(viewSizeType, width, height, text);
            var group = GetGroup(sizeGroupType);
            var addCustomSize = getGroup.ReturnType.GetMethod("AddCustomSize");
            var getTotalCount = getGroup.ReturnType.GetMethod("GetTotalCount");
            var gvsType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSize");
        #if UNITY_2019_1_OR_NEWER
            // Not sure in which exactly version they've changed signature, but I know that in 2019 it is
            var gvstType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSizeType");
            var ctor = gvsType.GetConstructor(new Type[] { gvstType, typeof(int), typeof(int), typeof(string) });
            var newSize = ctor.Invoke(new object[] { (int)viewSizeType, width, height, text });
        #else
            var ctor = gvsType.GetConstructor(new Type[] { typeof(int), typeof(int), typeof(int), typeof(string) });
            var newSize = ctor.Invoke(new object[] { (int)viewSizeType, width, height, text });
        #endif
            addCustomSize.Invoke(group, new object[] { newSize });
            var totalCount = (int)getTotalCount.Invoke(group, new object[] { });
            return totalCount - 1;
        }

        static void RemoveCustomSize(GameViewSizeGroupType sizeGroupType, int sizeIndex) {
            // This method reflectively does following:
            //  GameViewSizes group = gameViewSizesInstance.GetGroup(sizeGroupTyge);
            //  group.RemoveCustomSize(group.IndexOf(size));
            var group = GetGroup(sizeGroupType);
            var removeCustomSize = getGroup.ReturnType.GetMethod("RemoveCustomSize");
            removeCustomSize.Invoke(group, new object[] { sizeIndex });
        }
         
        static void SetSize(int sizeIndex) {
            var gameViewType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
            var gameView = EditorWindow.GetWindow(gameViewType);
            var selectedSizeIndexProp =
                gameViewType.GetProperty("selectedSizeIndex",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            selectedSizeIndexProp.SetValue(gameView, sizeIndex, null);
        }
    #endif
    }
}
