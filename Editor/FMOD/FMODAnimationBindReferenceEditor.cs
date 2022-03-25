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

#if UNITY_2020_1_OR_NEWER
#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !POINT_DISABLE_CHECKS
#define DEBUG_MODE
#endif
#define UNITYENGINE

using Point.Collections.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Point.Audio.FMODEditor
{
    [CustomEditor(typeof(FMODAnimationBindReference))]
    public sealed class FMODAnimationBindReferenceEditor : InspectorEditor<FMODAnimationBindReference>
    {
        private SerializedProperty m_Events;
        private ReorderableList m_EventList;

        private void OnEnable()
        {
            m_Events = GetSerializedProperty("m_Events");

            m_EventList = new ReorderableList(serializedObject, m_Events);
            
            m_EventList.drawHeaderCallback = DrawEventHeader;
            m_EventList.elementHeightCallback = EventElementHeight;
            m_EventList.drawElementCallback = DrawEventElement;
        }

        private static void DrawEventHeader(Rect rect) => EditorGUI.LabelField(rect, "Events");
        private float EventElementHeight(int index)
        {
            SerializedProperty element = m_Events.GetArrayElementAtIndex(index);

            if (element.isExpanded) return PropertyDrawerHelper.GetPropertyHeight(7);
            return PropertyDrawerHelper.GetPropertyHeight(1);
        }
        private void DrawEventElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            rect = PropertyDrawerHelper.FixedIndentForList(rect);

            SerializedProperty element = m_Events.GetArrayElementAtIndex(index);

            Rect targetRect = PropertyDrawerHelper.UseRect(ref rect, PropertyDrawerHelper.GetPropertyHeight(1));
            element.isExpanded =
                EditorGUI.Foldout(targetRect, element.isExpanded, element.displayName, true);
            if (!element.isExpanded) return;
            PropertyDrawerHelper.Indent(ref rect, 14);

            element.Next(true);
            targetRect = PropertyDrawerHelper.UseRect(ref rect, PropertyDrawerHelper.GetPropertyHeight(1));
            EditorGUI.PropertyField(targetRect, element);

            element.Next(false); // audio reference
            SerializedProperty ev = GetSerializedProperty(element, "m_Event");
            targetRect = PropertyDrawerHelper.UseRect(ref rect, PropertyDrawerHelper.GetPropertyHeight(1));
            EditorGUI.PropertyField(targetRect, ev);

            SerializedProperty overrides = GetSerializedProperty(element, "m_AllowFadeOut");
            targetRect = PropertyDrawerHelper.UseRect(ref rect, PropertyDrawerHelper.GetPropertyHeight(1));
            overrides.boolValue = EditorGUI.Toggle(targetRect, "Allow Fade-Out", overrides.boolValue);

            overrides.Next(false);
            targetRect = PropertyDrawerHelper.UseRect(ref rect, PropertyDrawerHelper.GetPropertyHeight(1));
            EditorGUI.PropertyField(targetRect, overrides);

            SerializedProperty minDis = GetSerializedProperty(element, "m_OverrideMinDistance");
            SerializedProperty maxDis = GetSerializedProperty(element, "m_OverrideMaxDistance");
            targetRect = PropertyDrawerHelper.UseRect(ref rect, PropertyDrawerHelper.GetPropertyHeight(1));
            var result = EditorUtilities.MinMaxSlider(targetRect, "Override Distance", minDis.floatValue, maxDis.floatValue, 1, 300);
            //targetRect.width -= 50;
            //EditorGUI.MinMaxSlider(targetRect, "Override Distance", ref min, ref max, 1, 300);

            //{
            //    var tempRect = targetRect;
            //    tempRect.x += targetRect.width + .75f;
            //    tempRect.width = 25 - 1.5f;

            //    min = EditorGUI.FloatField(tempRect, min);
            //    tempRect.x += 1.5f + 25;
            //    max = EditorGUI.FloatField(tempRect, max);
            //}

            minDis.floatValue = result.x;
            maxDis.floatValue = result.y;


        }

        public override void OnInspectorGUI()
        {
            EditorUtilities.StringHeader("Animation Bind Reference");
            EditorUtilities.Line();

            m_EventList.DoLayoutList();

            EditorGUILayout.Space();
            base.OnInspectorGUI();
        }
    }
}

#endif