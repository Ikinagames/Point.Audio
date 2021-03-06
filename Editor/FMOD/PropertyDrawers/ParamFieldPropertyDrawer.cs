using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;
using UnityEditor;
using Point.Collections.Editor;
using UnityEditor.AnimatedValues;

namespace Point.Audio.FMODEditor
{
    [CustomPropertyDrawer(typeof(ParamField), true)]
    public sealed class ParamFieldPropertyDrawer : PropertyDrawer<ParamField>
    {
        public sealed class Helper
        {
            const string
                c_IsGlobal = "m_IsGlobal",
                c_Name = "m_Name",
                c_Value = "m_Value",
                c_IgnoreSeekSpeed = "m_IgnoreSeekSpeed",

                c_EnableValueReflection = "m_EnableValueReflection",
                c_ReferenceObject = "m_ReferenceObject",
                c_ValueFieldName = "m_ValueFieldName";

            public static GUIContent
                IsGlobalContent = new GUIContent("Is Global Parameter",
                    "전역 Parameter 인지 설정합니다."),
                NameContent = new GUIContent("Name",
                    "Parameter 의 이름을 설정합니다. " +
                    "이름은 FMOD 에서 설정된 Parameter 의 실제 이름입니다."),
                ValueContent = new GUIContent("Value",
                    "입력될 값을 설정합니다."),
                IgnoreSeekSpeedContent = new GUIContent("Ignore Seek Speed",
                    "FMOD 에서 설정된 Fade 가 무시될 지 설정합니다."),

                EnableValueReflectionContent = new GUIContent("Enable Value Reflection",
                    "Value 값이 Reflection 으로 입력될 지 설정합니다."),
                ReferenceObjectContent = new GUIContent("Reference Object",
                    ""),
                ValueFieldNameContent = new GUIContent("Field Name",
                    "Reflection 을 할 Field, 혹은 Property 의 이름을 설정합니다.")
                ;

            public static SerializedProperty GetIsGlobalField(SerializedProperty property)
                => property.FindPropertyRelative(c_IsGlobal);
            
            public static SerializedProperty GetNameField(SerializedProperty property)
                => property.FindPropertyRelative(c_Name);
            public static SerializedProperty GetValueField(SerializedProperty property)
                => property.FindPropertyRelative(c_Value);
            public static SerializedProperty GetIgnoreSeekSpeedField(SerializedProperty property)
                => property.FindPropertyRelative(c_IgnoreSeekSpeed);

            public static SerializedProperty GetEnableReflectionField(SerializedProperty property)
                => property.FindPropertyRelative(c_EnableValueReflection);
            public static SerializedProperty GetReferenceObjectField(SerializedProperty property)
                => property.FindPropertyRelative(c_ReferenceObject);
            public static SerializedProperty GetValueFieldNameField(SerializedProperty property)
                => property.FindPropertyRelative(c_ValueFieldName);
        }

        protected override float PropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height;
            if (property.isExpanded)
            {
                var paramAtt = fieldInfo.GetCustomAttribute<FMODParamAttribute>();
                if (paramAtt != null)
                {
                    height = PropertyDrawerHelper.GetPropertyHeight(4) + 3;
                    if (!paramAtt.DisableReflection)
                    {
                        height += EditorGUIUtility.standardVerticalSpacing + 5;
                        height += PropertyDrawerHelper.GetPropertyHeight(1);
                    }

                    if (Helper.GetEnableReflectionField(property).boolValue)
                    {
                        height += PropertyDrawerHelper.GetPropertyHeight(2);
                    }
                }
                else
                {
                    height = PropertyDrawerHelper.GetPropertyHeight(5) + 8;
                }
            }
            else height = PropertyDrawerHelper.GetPropertyHeight(1);

            //if (m_Height == null)
            //{
            //    m_Height = new AnimFloat(height);
            //}
            //else
            //{
            //    m_Height.target = height;
            //}

            return height;
        }

        protected override void OnPropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            bool isInArray = property.IsInArray();
            if (!isInArray)
            {
                if (string.IsNullOrEmpty(label.text)) label = new GUIContent(property.displayName);
            }
            else
            {
                const string c_NameFormat = "{0}\t: {1}";

                string valueName = property.FindPropertyRelative("m_Name").stringValue;
                if (string.IsNullOrEmpty(valueName))
                {
                    label = new GUIContent(property.displayName);
                }
                else
                {
                    float valueTemp = property.FindPropertyRelative("m_Value").floatValue;
                    label = new GUIContent(string.Format(c_NameFormat, valueName, valueTemp));
                }
            }

            PropertyDrawerHelper.DrawBlock(EditorGUI.IndentedRect(rect.TotalRect), Color.black);
            {
                Rect foldRect;
                if (isInArray)
                {
                    foldRect = PropertyDrawerHelper.FixedIndentForList(rect.Pop());
                }
                else foldRect = rect.Pop();
                property.isExpanded = EditorGUI.Foldout(foldRect, property.isExpanded, label, true);
            }

            if (!property.isExpanded)
            {
                //RepaintInspector(property.serializedObject);
                return;
            }
            rect.Pop(EditorGUIUtility.standardVerticalSpacing);
            rect.SetLeftPadding(5);

            var isGlobal = Helper.GetIsGlobalField(property);
            FMODParamAttribute paramSetting = fieldInfo.GetCustomAttribute<FMODParamAttribute>();

            if (paramSetting != null)
            {
                if (paramSetting.GlobalParameter && !isGlobal.boolValue)
                {
                    isGlobal.boolValue = true;
                    property.serializedObject.ApplyModifiedProperties();
                }
                else if (!paramSetting.GlobalParameter && isGlobal.boolValue)
                {
                    isGlobal.boolValue = false;
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
            else
            {
                isGlobal.boolValue
                    = EditorGUI.ToggleLeft(rect.Pop(), Helper.IsGlobalContent, isGlobal.boolValue);

                rect.Pop(EditorGUIUtility.standardVerticalSpacing);
                EditorUtilities.Line(rect.Pop(5));
            }

            var name = Helper.GetNameField(property);
            if (isGlobal.boolValue)
            {
                EditorGUI.PropertyField(rect.Pop(), name, Helper.NameContent);
            }
            else
            {
                name.stringValue
                    = EditorGUI.TextField(rect.Pop(), Helper.NameContent, name.stringValue);
            }

            var value = Helper.GetValueField(property);
            value.floatValue
                = EditorGUI.FloatField(rect.Pop(), Helper.ValueContent, value.floatValue);

            var ignoreSeekSpeed = Helper.GetIgnoreSeekSpeedField(property);
            ignoreSeekSpeed.boolValue
                = EditorGUI.Toggle(rect.Pop(), Helper.IgnoreSeekSpeedContent, ignoreSeekSpeed.boolValue);

            var enableReflection = Helper.GetEnableReflectionField(property);

            if (paramSetting != null)
            {
                if (paramSetting.DisableReflection && enableReflection.boolValue)
                {
                    enableReflection.boolValue = false;
                    property.serializedObject.ApplyModifiedProperties();
                }
            }

            if (paramSetting != null && !paramSetting.DisableReflection)
            {
                rect.Pop(EditorGUIUtility.standardVerticalSpacing);
                EditorUtilities.Line(rect.Pop(5));

                enableReflection.boolValue
                    = EditorGUI.ToggleLeft(rect.Pop(), Helper.EnableValueReflectionContent, enableReflection.boolValue);

                if (enableReflection.boolValue)
                {
                    //EditorGUI.indentLevel++;

                    var refObj = Helper.GetReferenceObjectField(property);
                    EditorGUI.PropertyField(rect.Pop(), refObj, Helper.ReferenceObjectContent);

                    var fieldname = Helper.GetValueFieldNameField(property);
                    fieldname.stringValue
                        = EditorGUI.TextField(rect.Pop(), Helper.ValueFieldNameContent, fieldname.stringValue);

                    //EditorGUI.indentLevel--;
                }
            }
        }
    }
}
