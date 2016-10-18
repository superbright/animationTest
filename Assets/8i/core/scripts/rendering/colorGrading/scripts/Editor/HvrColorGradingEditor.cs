using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace HVR
{
    [CanEditMultipleObjects, CustomEditor(typeof(HvrColorGrading))]
    public class HvrColorGradingEditor : UnityEditor.Editor
    {
        #region Property drawers
        [CustomPropertyDrawer(typeof(HvrColorGrading.ColorPrimariesGroup))]
        private class ColorWheelGroupDrawer : PropertyDrawer
        {
            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                return 0;
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                EditorGUILayout.BeginHorizontal();
                {
                    foreach (SerializedProperty prop in property)
                    {
                        if (prop.propertyType == SerializedPropertyType.Vector4)
                        {
                            Rect area = EditorGUILayout.BeginVertical("box", GUILayout.MaxHeight(200));
                            {
                                float rgbMin = 0;
                                float rgbMax = 1;

                                System.Type type = HvrColorGrading.ColorPrimariesRGBMinMax.defaultSettings.GetType();
                                FieldInfo[] fields = type.GetFields();
                                foreach (FieldInfo fi in fields)
                                {
                                    if (prop.name == fi.Name)
                                    {
                                        object val = fi.GetValue(HvrColorGrading.ColorPrimariesRGBMinMax.defaultSettings);

                                        if (val.GetType() == typeof(Vector2))
                                        {
                                            rgbMin = ((Vector2)val).x;
                                            rgbMax = ((Vector2)val).y;
                                        }
                                    }
                                }

                                prop.vector4Value = Primaries.DoGUI(area, prop.displayName, prop.vector4Value, rgbMin, rgbMax);
                            }
                            EditorGUILayout.EndVertical();

                            // Reset
                            Color c = GUI.backgroundColor;
                            GUI.backgroundColor = Color.grey;

                            if (GUI.Button(new Rect(area.xMax - 30, area.y, 30, 30), icon_reset))
                            {
                                System.Type type = HvrColorGrading.ColorPrimariesSettings.defaultSettings.GetType();
                                FieldInfo[] fields = type.GetFields();

                                foreach (FieldInfo fi in fields)
                                {
                                    if (prop.name == fi.Name)
                                    {
                                        object val = fi.GetValue(HvrColorGrading.ColorPrimariesSettings.defaultSettings);

                                        if (val.GetType() == typeof(Vector4))
                                            prop.vector4Value = (Vector4)val;
                                    }
                                }
                            }
                            GUI.backgroundColor = c;
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        [CustomPropertyDrawer(typeof(HvrColorGrading.IndentedGroup))]
        private class IndentedGroupDrawer : PropertyDrawer
        {
            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                return 0f;
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

                EditorGUI.indentLevel++;

                foreach (SerializedProperty prop in property)
                    EditorGUILayout.PropertyField(prop);

                EditorGUI.indentLevel--;
            }
        }

        [CustomPropertyDrawer(typeof(HvrColorGrading.ChannelMixer))]
        private class ChannelMixerDrawer : PropertyDrawer
        {
            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                return 0f;
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                // TODO: Hardcoded variable names, rewrite this function
                if (property.type != "ChannelMixerSettings")
                    return;

                SerializedProperty currentChannel = property.FindPropertyRelative("currentChannel");
                int intCurrentChannel = currentChannel.intValue;

                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

                EditorGUI.indentLevel++;

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.PrefixLabel("Channel");
                    if (GUILayout.Toggle(intCurrentChannel == 0, "Red", EditorStyles.miniButtonLeft)) intCurrentChannel = 0;
                    if (GUILayout.Toggle(intCurrentChannel == 1, "Green", EditorStyles.miniButtonMid)) intCurrentChannel = 1;
                    if (GUILayout.Toggle(intCurrentChannel == 2, "Blue", EditorStyles.miniButtonRight)) intCurrentChannel = 2;
                }
                EditorGUILayout.EndHorizontal();

                SerializedProperty serializedChannel = property.FindPropertyRelative("channels").GetArrayElementAtIndex(intCurrentChannel);
                currentChannel.intValue = intCurrentChannel;

                Vector3 v = serializedChannel.vector3Value;
                v.x = EditorGUILayout.Slider("Red", v.x, -2f, 2f);
                v.y = EditorGUILayout.Slider("Green", v.y, -2f, 2f);
                v.z = EditorGUILayout.Slider("Blue", v.z, -2f, 2f);
                serializedChannel.vector3Value = v;

                EditorGUI.indentLevel--;
            }
        }

        [CustomPropertyDrawer(typeof(HvrColorGrading.Curve))]
        private class CurveDrawer : PropertyDrawer
        {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                HvrColorGrading.Curve attribute = (HvrColorGrading.Curve)base.attribute;

                if (property.propertyType != SerializedPropertyType.AnimationCurve)
                {
                    EditorGUI.LabelField(position, label.text, "Use ClampCurve with an AnimationCurve.");
                    return;
                }

                property.animationCurveValue = EditorGUI.CurveField(position, label, property.animationCurveValue, attribute.color, new Rect(0f, 0f, 1f, 1f));
            }
        }
        #endregion

        #region Styling
        private static Styles s_Styles;
        private class Styles
        {
            public GUIStyle thumb2D = "ColorPicker2DThumb";
            public GUIStyle header = "ShurikenModuleTitle";
            public GUIStyle headerCheckbox = "ShurikenCheckMark";
            public Vector2 thumb2DSize;

            internal Styles()
            {
                thumb2DSize = new Vector2(
                        !Mathf.Approximately(thumb2D.fixedWidth, 0f) ? thumb2D.fixedWidth : thumb2D.padding.horizontal,
                        !Mathf.Approximately(thumb2D.fixedHeight, 0f) ? thumb2D.fixedHeight : thumb2D.padding.vertical
                        );

                header.font = (new GUIStyle("Label")).font;
                header.border = new RectOffset(15, 7, 4, 4);
                header.fixedHeight = 22;
                header.contentOffset = new Vector2(20f, -2f);
            }
        }

        public static readonly Color masterCurveColor = new Color(1f, 1f, 1f, 2f);
        public static readonly Color redCurveColor = new Color(1f, 0f, 0f, 2f);
        public static readonly Color greenCurveColor = new Color(0f, 1f, 0f, 2f);
        public static readonly Color blueCurveColor = new Color(0f, 1f, 1f, 2f);

        public static Texture2D icon_reset;

        #endregion

        private HvrColorGrading concreteTarget
        {
            get { return target as HvrColorGrading; }
        }

        // settings group <setting, property reference>
        private Dictionary<FieldInfo, List<SerializedProperty>> m_GroupFields = new Dictionary<FieldInfo, List<SerializedProperty>>();

        private void PopulateMap(FieldInfo group)
        {
            var searchPath = group.Name + ".";
            foreach (var setting in group.FieldType.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                List<SerializedProperty> settingsGroup;
                if (!m_GroupFields.TryGetValue(group, out settingsGroup))
                {
                    settingsGroup = new List<SerializedProperty>();
                    m_GroupFields[group] = settingsGroup;
                }

                var property = serializedObject.FindProperty(searchPath + setting.Name);
                if (property != null)
                    settingsGroup.Add(property);
            }
        }

        private void OnEnable()
        {
            var settingsGroups = typeof(HvrColorGrading).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(x => x.GetCustomAttributes(typeof(HvrColorGrading.SettingsGroup), false).Any());

            foreach (var settingGroup in settingsGroups)
                PopulateMap(settingGroup);

            if (icon_reset == null)
                icon_reset = Resources.Load("8iEditor/icon_hvrgrade_reset") as Texture2D;
        }

        private void OnDisable()
        {

        }

        private bool Header(SerializedProperty group, SerializedProperty enabledField)
        {
            var display = group == null || group.isExpanded;
            var enabled = enabledField != null && enabledField.boolValue;
            var title = group == null ? "Unknown Group" : ObjectNames.NicifyVariableName(group.displayName);

            Rect rect = GUILayoutUtility.GetRect(16f, 22f, s_Styles.header);
            GUI.Box(rect, title, s_Styles.header);

            Rect toggleRect = new Rect(rect.x + 4f, rect.y + 4f, 13f, 13f);
            if (Event.current.type == EventType.Repaint)
                s_Styles.headerCheckbox.Draw(toggleRect, false, false, enabled, false);

            Event e = Event.current;
            if (e.type == EventType.MouseDown)
            {
                if (toggleRect.Contains(e.mousePosition) && enabledField != null)
                {
                    enabledField.boolValue = !enabledField.boolValue;
                    e.Use();
                }
                else if (rect.Contains(e.mousePosition) && group != null)
                {
                    display = !display;
                    group.isExpanded = !group.isExpanded;
                    e.Use();
                }
            }
            return display;
        }

        private void DrawFields()
        {
            foreach (var group in m_GroupFields)
            {
                var enabledField = group.Value.FirstOrDefault(x => x.propertyPath == group.Key.Name + ".enabled");
                var groupProperty = serializedObject.FindProperty(group.Key.Name);

                GUILayout.Space(5);
                bool display = Header(groupProperty, enabledField);
                if (!display)
                    continue;

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(10);
                    GUILayout.BeginVertical();
                    {
                        GUILayout.Space(3);
                        foreach (var field in group.Value.Where(x => x.propertyPath != group.Key.Name + ".enabled"))
                            EditorGUILayout.PropertyField(field);
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
            }
        }

        public override void OnInspectorGUI()
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            serializedObject.Update();

            DrawFields();

            serializedObject.ApplyModifiedProperties();
        }

        public static class Primaries
        {
            // hue Wheel
            private static GUIStyle s_CenteredStyle;

            public static Vector4 DoGUI(Rect area, string title, Vector4 vec, float rgbMin, float rgbMax)
            {
                if (s_CenteredStyle == null)
                {
                    s_CenteredStyle = new GUIStyle(GUI.skin.GetStyle("Label"))
                    {
                        alignment = TextAnchor.UpperCenter
                    };
                }

                GUILayout.Label(title, s_CenteredStyle);

                GUILayout.Space(10);

                ColorPickerHDRConfig hdrConfig = new ColorPickerHDRConfig(rgbMin, rgbMax, 0, 1);

                Color color = new Color(vec.x, vec.y, vec.z);

                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    GUIContent content = new GUIContent();
                    color = EditorGUILayout.ColorField(content, color, false, true, true, hdrConfig, GUILayout.Width(100), GUILayout.Height(20));
                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(2);

                EditorGUILayout.BeginHorizontal();
                {
                    vec.x = DrawColorSlider("R", color.r, rgbMin, rgbMax, Color.red);
                    vec.y = DrawColorSlider("G", color.g, rgbMin, rgbMax, Color.green);
                    vec.z = DrawColorSlider("B", color.b, rgbMin, rgbMax, Color.blue);
                    GUI.color = Color.white;
                }
                EditorGUILayout.EndHorizontal();

                return vec;
            }

            static float DrawColorSlider(string title, float val, float min, float max, Color colour)
            {
                Rect rect = EditorGUILayout.BeginVertical("box");
                {
                    Color origColor = GUI.color;
                    GUI.color = colour;
                    GUI.Box(rect, "");
                    GUI.color = origColor;

                    EditorGUILayout.BeginHorizontal();
                    {
                        GUILayout.FlexibleSpace();
                        val = GUILayout.VerticalSlider(val, max, min);
                        GUILayout.FlexibleSpace();
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    {
                        GUILayout.FlexibleSpace();
                        val = EditorGUILayout.FloatField(val, GUILayout.MaxWidth(30));
                        GUILayout.FlexibleSpace();
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    {
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(title);
                        GUILayout.FlexibleSpace();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();

                return val;
            }
        }
    }
}
