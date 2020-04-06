using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;
using UnityEngine;

namespace EnhancedHierarchy {
    /// <summary>
    /// Generic preference item interface.
    /// </summary>
    internal interface IPrefItem {

        string Key { get; }
        bool Drawing { get; }
        object Value { get; set; }

        GUIContent Content { get; }

        void DoGUI();

        GUIEnabled GetEnabledScope();
        GUIEnabled GetEnabledScope(bool enabled);
        GUIFade GetFadeScope(bool enabled);
    }

    /// <summary>
    /// Generic preference item.
    /// </summary>
    internal sealed class PrefItem<T> : IPrefItem {

        private T _value;
        private bool loaded;
        private GUIFade fade;
        private Type typeOfT = typeof(T);
        private static readonly BinaryFormatter formatter = new BinaryFormatter();

        public string Key { get; private set; }
        public T DefaultValue { get; private set; }
        public GUIContent Content { get; private set; }
        public bool Drawing { get { return fade.Visible; } }

        public T Value {
            get {
                if(!loaded)
                    _value = LoadValue();
                return _value;
            }
            set {
                if(!_value.Equals(value))
                    SaveValue(value);
            }
        }

        object IPrefItem.Value {
            get { return Value; }
            set { Value = (T)value; }
        }

        public PrefItem(string key, T defaultValue, string text, string tooltip) {
            fade = new GUIFade();
            Key = "HierarchyPreferences." + key;

            Preferences.allKeys[Preferences.currentKeyIndex++] = Key;

            Content = new GUIContent(text, tooltip);
            DefaultValue = defaultValue;
        }

        private T LoadValue() {
            try {
                if(!EditorPrefs.HasKey(Key))
                    return _value = DefaultValue;

                else if(typeOfT == typeof(bool))
                    return (T)(object)EditorPrefs.GetBool(Key);

                else if(typeOfT == typeof(int) || typeOfT.IsEnum)
                    return (T)(object)EditorPrefs.GetInt(Key);

                else if(typeOfT == typeof(float))
                    return (T)(object)EditorPrefs.GetFloat(Key);

                else if(typeOfT == typeof(string))
                    return (T)(object)EditorPrefs.GetString(Key);

                else if(typeOfT == typeof(Color))
                    return (T)(object)(Color)(SerializableVector)GetSerializedValue(EditorPrefs.GetString(Key));

                else if(typeOfT == typeof(Vector2))
                    return (T)(object)(Vector2)(SerializableVector)GetSerializedValue(EditorPrefs.GetString(Key));

                else if(typeOfT == typeof(Vector3))
                    return (T)(object)(Vector3)(SerializableVector)GetSerializedValue(EditorPrefs.GetString(Key));

                else if(typeOfT == typeof(Vector4))
                    return (T)(object)(Vector4)(SerializableVector)GetSerializedValue(EditorPrefs.GetString(Key));

                else
                    return (T)GetSerializedValue(EditorPrefs.GetString(Key));
            }
            catch(Exception e) {
                Debug.LogWarning(string.Format("Failed to load {0}, using default value: {1}", Key, e));
                return DefaultValue;
            }
            finally {
                loaded = true;
            }
        }

        private void SaveValue(T value) {
            try {
                if(value is bool)
                    EditorPrefs.SetBool(Key, (bool)(object)value);

                else if(value is int || value is Enum)
                    EditorPrefs.SetInt(Key, (int)(object)value);

                else if(value is float)
                    EditorPrefs.SetFloat(Key, (float)(object)value);

                else if(value is string)
                    EditorPrefs.SetString(Key, (string)(object)value);

                else if(value is Color || value is Vector2 || value is Vector3 || value is Vector4)
                    EditorPrefs.SetString(Key, GetSerializedString(SerializableVector.GetVectorFromObject(value)));

                else
                    EditorPrefs.SetString(Key, GetSerializedString(value));
            }
            catch(Exception e) {
                Debug.LogWarning(string.Format("Failed to save {0}: {1}", Key, e));
            }
            finally {
                _value = value;
            }
        }

        public void ForceSave() {
            SaveValue(_value);
        }

        private static object GetSerializedValue(string base64) {
            using(var stream = new MemoryStream(Convert.FromBase64String(base64)))
                return formatter.Deserialize(stream);
        }

        private static string GetSerializedString(object value) {
            using(var stream = new MemoryStream()) {
                formatter.Serialize(stream, value);

                var buffer = new byte[stream.Length];

                stream.Position = 0;
                stream.Read(buffer, 0, buffer.Length);

                return Convert.ToBase64String(buffer);
            }
        }

        public void DoGUI() {
            if(Drawing) {
                if(Value is int)
                    Value = (T)(object)EditorGUILayout.IntField(Content, (int)(object)Value);

                else if(Value is Enum)
                    Value = (T)(object)EditorGUILayout.EnumPopup(Content, (Enum)(object)Value);

                else if(Value is float)
                    Value = (T)(object)EditorGUILayout.FloatField(Content, (float)(object)Value);

                else if(Value is bool)
                    Value = (T)(object)EditorGUILayout.Toggle(Content, (bool)(object)Value);

                else if(Value is string)
                    Value = (T)(object)EditorGUILayout.TextField(Content, (string)(object)Value);

                else if(Value is Color)
                    Value = (T)(object)EditorGUILayout.ColorField(Content, (Color)(object)Value);

                else if(Value is RightSideIcon) {
                    var icons = RightSideIcon.AllIcons;
                    var index = Array.IndexOf(icons, Value);
                    var labels = (from icon in icons
                                  select new GUIContent(icon)).ToArray();

                    index = EditorGUILayout.Popup(Content, index, labels);

                    if(index >= 0 && index < icons.Length)
                        Value = (T)(object)icons[index];
                }
            }
        }

        public void DoSlider(T min, T max) {
            if(Drawing)
                if(Value is float)
                    Value = (T)(object)EditorGUILayout.Slider(Content, (float)(object)Value, (float)(object)min, (float)(object)max);
                else
                    Value = (T)(object)EditorGUILayout.IntSlider(Content, (int)(object)Value, (int)(object)min, (int)(object)max);
        }

        public GUIEnabled GetEnabledScope() {
            return GetEnabledScope(_value.Equals(true));
        }

        public GUIEnabled GetEnabledScope(bool enabled) {
            return new GUIEnabled(enabled);
        }

        public GUIFade GetFadeScope(bool enabled) {
            fade.SetTarget(enabled);
            return fade;
        }

        public static implicit operator T(PrefItem<T> pb) {
            if(pb == null) {
                try { Preferences.ReloadPrefs(); }
                catch { }
                return default(T);
            }
            else
                return pb.Value;
        }

        public static implicit operator GUIContent(PrefItem<T> pb) {
            return pb.Content;
        }

    }
}