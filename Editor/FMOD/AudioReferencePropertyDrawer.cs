// Copyright 2022 Ikina Games
// Author : Seung Ha Kim (Syadeu)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Point.Collections.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Point.Audio.FMODEditor
{
    //[CustomPropertyDrawer(typeof(AudioReference), true)]
    //public sealed class AudioReferencePropertyDrawer : PropertyDrawer
    //{
    //    private sealed class Helper
    //    {
    //        const string
    //            c_Event = "m_Event",
    //            c_Parameters = "m_Parameters";

    //        public static GUIContent
    //            EventContent = new GUIContent("Event",
    //                ""),
    //            ParametersContent = new GUIContent("Parameters",
    //                "");

    //        public static SerializedProperty GetEventField(SerializedProperty property)
    //            => property.FindPropertyRelative(c_Event);
    //        public static SerializedProperty GetParametersField(SerializedProperty property)
    //            => property.FindPropertyRelative(c_Parameters);

    //        //private static ReorderableList s_ParamList;
    //        public static ReorderableList GetParamList(SerializedProperty property)
    //        {
    //            //if (s_ParamList == null)
    //            {
    //                //s_ParamList = new ReorderableList(property.serializedObject, property);
    //                return new ReorderableList(
    //                    property.serializedObject, 
    //                    GetParametersField(property),
    //                    true, true, true, true);
    //            }

    //            //return s_ParamList;
    //        }
    //    }

    //    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    //    {
    //        if (string.IsNullOrEmpty(label.text)) label = new GUIContent(property.displayName);

    //        property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label);
    //        if (!property.isExpanded) return;
    //        EditorGUI.indentLevel++;

    //        PropertyDrawerHelper.Space(ref position);

    //        using (new EditorUtilities.BoxBlock(Color.black))
    //        using (var change = new EditorGUI.ChangeCheckScope())
    //        using (new EditorGUI.PropertyScope(position, null, property))
    //        {
    //            var eventField = Helper.GetEventField(property);
    //            int count = eventField.isExpanded ? 6 : 1;
    //            EditorGUI.PropertyField(PropertyDrawerHelper.GetRect(position, count), eventField);

    //            EditorUtilities.Line();

    //            var parameters = Helper.GetParametersField(property);
    //            parameters.isExpanded 
    //                = EditorGUI.Foldout(PropertyDrawerHelper.GetRect(position), parameters.isExpanded, Helper.ParametersContent);
    //            if (parameters.isExpanded)
    //            {
    //                EditorGUI.indentLevel++;

    //                var list = Helper.GetParamList(property);
    //                list.drawHeaderCallback = (Rect rect) =>
    //                {
    //                    EditorGUI.LabelField(rect, Helper.ParametersContent);
    //                };
    //                //list.elementHeightCallback = (int index) =>
    //                //{
    //                //    var element = parameters.GetArrayElementAtIndex(index);
    //                //    var value = element.isExpanded ? 8 : 1;

    //                //    return PropertyDrawerHelper.lineHeight * value;
    //                //};
    //                list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
    //                {
    //                    var element = parameters.GetArrayElementAtIndex(index);
    //                    //var value = element.isExpanded ? 7 : 0;
    //                    //rect.y -= (PropertyDrawerHelper.lineHeight * value) * .5f;

    //                    var name = ParamFieldPropertyDrawer.Helper.GetNameField(element);

    //                    EditorGUI.PropertyField(rect, element, new GUIContent(name.stringValue), false);
    //                    //EditorGUI.LabelField(rect, "test");
    //                };

    //                //property.serializedObject.Update();
    //                list.DoLayoutList();

    //                //list.DoList(PropertyDrawerHelper.GetRect(position, parameters.arraySize + 3));

    //                EditorGUI.indentLevel--;
    //            }

    //            if (change.changed)
    //            {
    //                property.serializedObject.ApplyModifiedProperties();
    //            }
    //        }

    //        EditorGUI.indentLevel--;
    //    }
    //}
}
