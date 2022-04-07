﻿// Copyright 2022 Ikina Games
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
    [CustomPropertyDrawer(typeof(FMODEventReference))]
    public sealed class FMODEventReferencePropertyDrawer : PropertyDrawer<FMODEventReference>
    {
        private sealed class Helper
        {
            const string
                c_Event = "m_Event",
                c_Parameters = "m_Parameters";

            public static GUIContent
                EventContent = new GUIContent("Event",
                    ""),
                ParametersContent = new GUIContent("Parameters",
                    "");

            public static SerializedProperty GetEventField(SerializedProperty property)
                => property.FindPropertyRelative(c_Event);
            public static SerializedProperty GetParametersField(SerializedProperty property)
                => property.FindPropertyRelative(c_Parameters);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded) return PropertyDrawerHelper.GetPropertyHeight(1);

            var ev = Helper.GetEventField(property);
            var param = Helper.GetParametersField(property);

            float height = EditorGUI.GetPropertyHeight(ev) + EditorGUI.GetPropertyHeight(param, true);

            height += PropertyDrawerHelper.GetPropertyHeight(1);
            if (!property.IsInArray())
            {
                height += PropertyDrawerHelper.GetPropertyHeight(1);
            }

            //height += EditorGUI.GetPropertyHeight(SerializedPropertyType.Boolean, GUIContent.none);
            //height += EditorGUI.GetPropertyHeight(SerializedPropertyType.Boolean, GUIContent.none);
            //height += EditorGUI.GetPropertyHeight(SerializedPropertyType.Float, GUIContent.none);
            //height += EditorGUI.GetPropertyHeight(SerializedPropertyType.Float, GUIContent.none);

            return height;
        }

        protected override void OnPropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            var ev = Helper.GetEventField(property);
            {
                var labelName = ev.FindPropertyRelative("Path");
                if (string.IsNullOrEmpty(labelName.stringValue)) label = new GUIContent(property.displayName);
                else label = new GUIContent(labelName.stringValue);
            }

            if (!property.IsInArray())
            {
                property.isExpanded = EditorGUI.Foldout(rect.Pop(), property.isExpanded, label, true);
                if (!property.isExpanded) return;
            }

            EditorGUI.PropertyField(
                rect.Pop(EditorGUI.GetPropertyHeight(ev)),
                ev, Helper.EventContent, true);

            var param = Helper.GetParametersField(property);
            EditorGUI.PropertyField(
                rect.Pop(EditorGUI.GetPropertyHeight(param)),
                param, Helper.ParametersContent, true);
        }
    }
}