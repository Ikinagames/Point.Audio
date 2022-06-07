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

using log4net.Filter;
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
                c_Parameters = "m_Parameters",

                c_AllowFadeOut = "m_AllowFadeOut",
                c_OverrideAttenuation = "m_OverrideAttenuation",
                c_OverrideMinDistance = "m_OverrideMinDistance", c_OverrideMaxDistance = "m_OverrideMaxDistance",

                c_ExposeGlobalEvent = "m_ExposeGlobalEvent",
                c_ExposedName = "m_ExposedName";

            public static GUIContent
                EventContent = new GUIContent("Event",
                    ""),
                ParametersContent = new GUIContent("Parameters",
                    ""),

                AllowFadeOutContent = new GUIContent("AllowFadeOut"),
                OverrideAttenuationContent = new GUIContent("OverrideAttenuation"),
                OverrideDistanceContent = new GUIContent("OverrideDistance"),

                ExposeGlobalEventContent = new GUIContent("ExposeGlobalEvent"),
                ExposedNameContent = new GUIContent("ExposedName",
                    "");

            public static SerializedProperty GetEventField(SerializedProperty property)
                => property.FindPropertyRelative(c_Event);
            public static SerializedProperty GetParametersField(SerializedProperty property)
                => property.FindPropertyRelative(c_Parameters);

            public static SerializedProperty GetExposeGlobalEventField(SerializedProperty property)
                => property.FindPropertyRelative(c_ExposeGlobalEvent);
            public static SerializedProperty GetExposedNameField(SerializedProperty property)
                => property.FindPropertyRelative(c_ExposedName);

            const string c_Snapshot = "snapshot:/", c_Path = "Path";

            public static bool IsEventEmpty(SerializedProperty property)
            {
                return string.IsNullOrEmpty(GetEventField(property).FindPropertyRelative(c_Path).stringValue);
            }
            public static bool IsSnapshot(SerializedProperty property)
            {
                string path = GetEventField(property).FindPropertyRelative(c_Path).stringValue;
                return path.StartsWith(c_Snapshot);
            }
        }

        protected override void OnPropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            Rect block = rect.TotalRect;
            block.x = rect.Current.x;
            block.height = rect.Current.height;
            CoreGUI.DrawBlock(EditorGUI.IndentedRect(block), Color.black);

            property.isExpanded = LabelToggle(ref rect, property.isExpanded, label, 15, TextAnchor.MiddleLeft);

            if (!property.isExpanded) return;

            SerializedProperty 
                parameterProp = Helper.GetParametersField(property),
                
                exposeGlobalEvProp = Helper.GetExposeGlobalEventField(property),
                exposeNameProp = Helper.GetExposedNameField(property);

            EditorGUI.indentLevel++;
            PropertyField(ref rect, Helper.GetEventField(property));
            EditorGUI.indentLevel--;
            PropertyField(ref rect, parameterProp, parameterProp.isExpanded);

            PropertyField(ref rect, exposeGlobalEvProp);
            if (exposeGlobalEvProp.boolValue)
            {
                PropertyField(ref rect, exposeNameProp);
            }
        }
    }
}
