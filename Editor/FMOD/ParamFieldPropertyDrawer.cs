using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;
using UnityEditor;
using Point.Collections.Editor;

namespace Point.Audio.FMODEditor
{
    [CustomPropertyDrawer(typeof(ParamField), true)]
    public sealed class ParamFieldPropertyDrawer : PropertyDrawer
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
                    "���� Parameter ���� �����մϴ�."),
                NameContent = new GUIContent("Name",
                    "Parameter �� �̸��� �����մϴ�. " +
                    "�̸��� FMOD ���� ������ Parameter �� ���� �̸��Դϴ�."),
                ValueContent = new GUIContent("Value",
                    "�Էµ� ���� �����մϴ�."),
                IgnoreSeekSpeedContent = new GUIContent("Ignore Seek Speed",
                    "FMOD ���� ������ Fade �� ���õ� �� �����մϴ�."),

                EnableValueReflectionContent = new GUIContent("Enable Value Reflection",
                    "Value ���� Reflection ���� �Էµ� �� �����մϴ�."),
                ReferenceObjectContent = new GUIContent("Reference Object",
                    ""),
                ValueFieldNameContent = new GUIContent("Field Name",
                    "Reflection �� �� Field, Ȥ�� Property �� �̸��� �����մϴ�.")
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
            if (string.IsNullOrEmpty(label.text)) label = new GUIContent(property.displayName);

            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label);
            if (!property.isExpanded) return;
            EditorGUI.indentLevel++;

            using (new EditorUtilities.BoxBlock(Color.black))
            using (var change = new EditorGUI.ChangeCheckScope())
            using (new EditorGUI.PropertyScope(position, null, property))
            {
                var isGlobal = Helper.GetIsGlobalField(property);
                var paramSetting = fieldInfo.GetCustomAttribute<FMODParamAttribute>();
                if (paramSetting != null && paramSetting.GlobalParameter)
                {
                    if (!isGlobal.boolValue)
                    {
                        isGlobal.boolValue = true;
                        property.serializedObject.ApplyModifiedProperties();
                    }
                }
                else
                {
                    isGlobal.boolValue
                        = EditorGUI.ToggleLeft(PropertyDrawerHelper.GetRect(position), Helper.IsGlobalContent, isGlobal.boolValue);

                    EditorGUILayout.Space();
                    EditorUtilities.Line();
                }
                
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

                if (paramSetting != null && !paramSetting.DisableReflection)
                {
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
