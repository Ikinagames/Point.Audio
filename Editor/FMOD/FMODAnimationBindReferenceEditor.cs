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

using GraphProcessor;
using Point.Collections;
using Point.Collections.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Point.Audio.FMODEditor
{
    [CustomEditor(typeof(FMODAnimationBindReference))]
    internal sealed class FMODAnimationBindReferenceEditor : InspectorEditorUXML<FMODAnimationBindReference>
    {
        private SerializedProperty m_Events, m_PlayWhileActive;
        //private ReorderableList m_EventList;

        private void OnEnable()
        {
            m_Events = GetSerializedProperty("m_Events");
            m_PlayWhileActive = GetSerializedProperty("m_PlayWhileActive");

            //m_EventList = new ReorderableList(serializedObject, m_Events);
            
            //m_EventList.drawHeaderCallback = DrawEventHeader;
            //m_EventList.elementHeightCallback = EventElementHeight;
            //m_EventList.drawElementCallback = DrawEventElement;
        }

        //private static void DrawEventHeader(Rect rect) => EditorGUI.LabelField(rect, "Events");
        //private float EventElementHeight(int index)
        //{
        //    SerializedProperty element = m_Events.GetArrayElementAtIndex(index);

        //    return PropertyDrawerHelper.GetPropertyHeight(serializedObject, element);
        //    //if (element.isExpanded)
        //    //{
        //    //    if (GetSerializedProperty(element, "m_AudioReference.m_Parameters").isExpanded)
        //    //    {
        //    //        return PropertyDrawerHelper.GetPropertyHeight(14);
        //    //    }

        //    //    return PropertyDrawerHelper.GetPropertyHeight(7);
        //    //}
        //    //return PropertyDrawerHelper.GetPropertyHeight(1);
        //}
        //private void DrawEventElement(Rect rect, int index, bool isActive, bool isFocused)
        //{
        //    rect = PropertyDrawerHelper.FixedIndentForList(rect);
        //    AutoRect autoRect = new AutoRect(rect);

        //    SerializedProperty element = m_Events.GetArrayElementAtIndex(index);

        //    element.isExpanded =
        //        EditorGUI.Foldout(autoRect.Pop(), element.isExpanded, element.displayName, true);
        //    if (!element.isExpanded) return;
        //    autoRect.Indent(14);

        //    element.Next(true);
        //    EditorGUI.PropertyField(autoRect.Pop(), element);

        //    element.Next(false); // audio reference
        //    SerializedProperty ev = GetSerializedProperty(element, "m_Event");
        //    EditorGUI.PropertyField(autoRect.Pop(), ev);

        //    SerializedProperty overrides = GetSerializedProperty(element, "m_AllowFadeOut");
        //    overrides.boolValue = EditorGUI.Toggle(autoRect.Pop(), "Allow Fade-Out", overrides.boolValue);

        //    overrides.Next(false);
        //    EditorGUI.PropertyField(autoRect.Pop(), overrides);

        //    SerializedProperty minDis = GetSerializedProperty(element, "m_OverrideMinDistance");
        //    SerializedProperty maxDis = GetSerializedProperty(element, "m_OverrideMaxDistance");

        //    CoreGUI.MinMaxSlider(autoRect.Pop(), "Override Distance", minDis, maxDis, 1, 300);

        //    SerializedProperty parameters = GetSerializedProperty(element, "m_Parameters");
        //    EditorGUI.PropertyField(autoRect.Pop(), parameters);
        //}

        //public override string GetHeaderName() => "Animation Bind Reference";
        //protected override void OnInspectorGUIContents()
        //{
        //    //m_EventList.DoLayoutList();
        //    EditorGUILayout.PropertyField(m_Events);
        //    EditorGUILayout.PropertyField(m_PlayWhileActive);

        //    EditorGUILayout.Space();

        //    serializedObject.ApplyModifiedProperties();
        //    //base.OnInspectorGUI();
        //}
    }
}

#endif