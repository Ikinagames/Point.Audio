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

            var parameterProp = Helper.GetParametersField(property);

            EditorGUI.indentLevel++;
            PropertyField(ref rect, Helper.GetEventField(property));
            PropertyField(ref rect, parameterProp, parameterProp.isExpanded);
            EditorGUI.indentLevel--;
        }
    }
}
