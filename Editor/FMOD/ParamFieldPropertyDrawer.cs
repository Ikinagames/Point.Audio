using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using Point.Collections.Editor;

namespace Point.Audio.FMODEditor
{
    [CustomPropertyDrawer(typeof(ParamField), true)]
    public sealed class ParamFieldPropertyDrawer : PropertyDrawer
    {
        private sealed class Helper
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

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (string.IsNullOrEmpty(label.text)) label = new GUIContent("Item");

            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label);
            if (!property.isExpanded) return;
            EditorGUI.indentLevel++;

            using (new EditorUtilities.BoxBlock(Color.black))
            using (var change = new EditorGUI.ChangeCheckScope())
            using (new EditorGUI.PropertyScope(position, null, property))
            {
                var isGlobal = Helper.GetIsGlobalField(property);
                isGlobal.boolValue 
                    = EditorGUI.ToggleLeft(PropertyDrawerHelper.GetRect(position), Helper.IsGlobalContent, isGlobal.boolValue);
                
                EditorGUILayout.Space();
                EditorUtilities.Line();

                var name = Helper.GetNameField(property);
                if (isGlobal.boolValue)
                {
                    EditorGUI.PropertyField(PropertyDrawerHelper.GetRect(position), name, Helper.NameContent);
                }
                else
                {
                    name.stringValue
                        = EditorGUI.TextField(PropertyDrawerHelper.GetRect(position), Helper.NameContent, name.stringValue);
                }

                var value = Helper.GetValueField(property);
                value.floatValue
                    = EditorGUI.FloatField(PropertyDrawerHelper.GetRect(position), Helper.ValueContent, value.floatValue);

                var ignoreSeekSpeed = Helper.GetIgnoreSeekSpeedField(property);
                ignoreSeekSpeed.boolValue
                    = EditorGUI.Toggle(PropertyDrawerHelper.GetRect(position), Helper.IgnoreSeekSpeedContent, ignoreSeekSpeed.boolValue);

                EditorGUILayout.Space();
                EditorUtilities.Line();

                var enableReflection = Helper.GetEnableReflectionField(property);
                enableReflection.boolValue
                    = EditorGUI.ToggleLeft(PropertyDrawerHelper.GetRect(position), Helper.EnableValueReflectionContent, enableReflection.boolValue);

                if (enableReflection.boolValue)
                {
                    EditorGUI.indentLevel++;

                    var refObj = Helper.GetReferenceObjectField(property);
                    EditorGUI.PropertyField(PropertyDrawerHelper.GetRect(position), refObj, Helper.ReferenceObjectContent);

                    var fieldname = Helper.GetValueFieldNameField(property);
                    fieldname.stringValue
                        = EditorGUI.TextField(PropertyDrawerHelper.GetRect(position), Helper.ValueFieldNameContent, fieldname.stringValue);

                    EditorGUI.indentLevel--;
                }

                if (change.changed)
                {
                    property.serializedObject.ApplyModifiedProperties();
                }
            }

            EditorGUI.indentLevel--;
        }
    }
}
