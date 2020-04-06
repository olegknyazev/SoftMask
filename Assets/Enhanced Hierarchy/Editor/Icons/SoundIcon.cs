using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;

namespace EnhancedHierarchy.Icons {
    [Serializable]
    internal sealed class SoundIcon : LeftSideIcon {

        [NonSerialized]
        private static bool isPlaying;
        [NonSerialized]
        private static AudioSource audioSource;
        [NonSerialized]
        private static AnimBool currentAnim;
        [NonSerialized]
        private static Dictionary<AudioSource, AnimBool> sourcesAnim = new Dictionary<AudioSource, AnimBool>();
        [NonSerialized]
        private static Texture icon;

        public override string Name { get { return "Audio Source Icon"; } }
        public override float Width { get { return currentAnim.faded * (base.Width - 2f); } }

        static SoundIcon() {
            EditorApplication.update += () => {
                if(!Preferences.IsButtonEnabled(new SoundIcon()))
                    return;

                foreach(var kvp in sourcesAnim)
                    if(kvp.Key && kvp.Value != null)
                        kvp.Value.target = kvp.Key.isPlaying;
            };
        }

        public override void Init() {
            if(!EnhancedHierarchy.IsRepaintEvent || !EnhancedHierarchy.IsGameObject)
                return;

            audioSource = EnhancedHierarchy.CurrentGameObject.GetComponent<AudioSource>();
            isPlaying = audioSource && audioSource.isPlaying;

            if(!sourcesAnim.TryGetValue(audioSource, out currentAnim)) {
                sourcesAnim[audioSource] = currentAnim = new AnimBool(isPlaying);
                currentAnim.valueChanged.AddListener(EditorApplication.RepaintHierarchyWindow);
            }
        }

        public override void DoGUI(Rect rect) {
            if(!EnhancedHierarchy.IsRepaintEvent || !EnhancedHierarchy.IsGameObject || !audioSource || Width <= 1f)
                return;

            using(ProfilerSample.Get()) {
                if(!icon)
                    icon = EditorGUIUtility.ObjectContent(null, typeof(AudioSource)).image;

                rect.yMax -= 1f;
                rect.yMin += 1f;

                GUI.DrawTexture(rect, icon, ScaleMode.StretchToFill);
            }
        }
    }
}