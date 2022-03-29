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

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.isExpanded)
            {
                return PropertyDrawerHelper.GetPropertyHeight(6);
            }

            return PropertyDrawerHelper.GetPropertyHeight(1);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (string.IsNullOrEmpty(label.text)) label = new GUIContent(property.displayName);

            PropertyDrawerHelper.DrawBlock(position, Color.black);
            AutoRect rect = new AutoRect(position);

            using (var change = new EditorGUI.ChangeCheckScope())
            using (new EditorGUI.PropertyScope(position, null, property))
            {
                {
                    Rect foldRect;
                    if (PropertyDrawerHelper.IsPropertyInArray(property))
                    {
                        foldRect = PropertyDrawerHelper.FixedIndentForList(rect.Pop());
                    }
                    else foldRect = rect.Pop();
                    property.isExpanded = EditorGUI.Foldout(foldRect, property.isExpanded, label, true);
                }
                
                if (!property.isExpanded)
                {
                    return;
                }
                rect.Pop(EditorGUIUtility.standardVerticalSpacing);

                EditorGUI.indentLevel++;

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

                if (paramSetting != null && !paramSetting.DisableReflection)
                {
                    rect.Pop(EditorGUIUtility.standardVerticalSpacing);
                    EditorUtilities.Line(rect.Pop(EditorGUIUtility.singleLineHeight));

                    var enableReflection = Helper.GetEnableReflectionField(property);
                    enableReflection.boolValue
                        = EditorGUI.ToggleLeft(rect.Pop(), Helper.EnableValueReflectionContent, enableReflection.boolValue);

                    if (enableReflection.boolValue)
                    {
                        EditorGUI.indentLevel++;

                        var refObj = Helper.GetReferenceObjectField(property);
                        EditorGUI.PropertyField(rect.Pop(), refObj, Helper.ReferenceObjectContent);

                        var fieldname = Helper.GetValueFieldNameField(property);
                        fieldname.stringValue
                            = EditorGUI.TextField(rect.Pop(), Helper.ValueFieldNameContent, fieldname.stringValue);

                        EditorGUI.indentLevel--;
                    }
                }

                if (change.changed)
                {
                    property.serializedObject.ApplyModifiedProperties();
                }
            }

            EditorGUI.indentLevel--;
            //EditorGUI.EndFoldoutHeaderGroup();
        }
    }
}
