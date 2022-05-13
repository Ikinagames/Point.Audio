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

#if UNITY_2019_1_OR_NEWER
#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !POINT_DISABLE_CHECKS
#define DEBUG_MODE
#endif
#define UNITYENGINE

using Point.Collections;
using Point.Collections.Editor;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Point.Audio.Editor
{
    [CustomEditor(typeof(AudioList))]
    internal sealed class AudioListEditor : InspectorEditor<AudioList>
    {
        SerializedProperty
            m_FriendlyNamesProperty, m_DataProperty;
        List<FriendlyName> m_FriendlyNames;
        List<Data> m_Data;

        private sealed class FriendlyName
        {
            public static GUIContent Header = new GUIContent("Friendly Names");
            public static string SearchText = string.Empty;

            private readonly SerializedProperty m_Property,
                m_FriendlyNameProperty, m_ClipProperty;

            //AnimFloat m_Height;

            public FriendlyName(SerializedProperty property)
            {
                m_Property = property;

                m_FriendlyNameProperty = property.FindPropertyRelative("m_FriendlyName");
                m_ClipProperty = property.FindPropertyRelative("m_AudioClip");
            }

            public string Name
            {
                get => m_FriendlyNameProperty.stringValue;
                set => m_FriendlyNameProperty.stringValue = value;
            }
            public AudioClip AudioClip
            {
                get
                {
                    return SerializedPropertyHelper.GetAssetPathField<AudioClip>(m_ClipProperty);
                }
                set
                {
                    SerializedPropertyHelper.SetAssetPathField(m_ClipProperty, value);
                }
            }

            public bool OnGUI()
            {
                if (!SearchText.IsNullOrEmpty())
                {
                    if (!Name.ToLower().Contains(SearchText.ToLower())) return false;
                }

                using (new EditorGUI.IndentLevelScope())
                using (new EditorGUILayout.HorizontalScope())
                {
                    Name = EditorGUILayout.TextField(GUIContent.none, Name);

                    AudioClip = (AudioClip)EditorGUILayout.ObjectField(GUIContent.none, AudioClip, TypeHelper.TypeOf<AudioClip>.Type, false);
                }

                return true;
            }
        }
        private sealed class Data
        {
            public static GUIContent Header = new GUIContent("Data");
            public static string SearchText = string.Empty;

            private readonly SerializedProperty m_Property,
                m_AudioClipProperty;

            public Data(SerializedProperty property)
            {
                m_Property = property;

                m_AudioClipProperty = property.FindPropertyRelative("m_AudioClip");
            }

            public string AudioClipPath
            {
                get => SerializedPropertyHelper.GetAssetPathFieldPath(m_AudioClipProperty);
                set => SerializedPropertyHelper.SetAssetPathFieldPath(m_AudioClipProperty, value);
            }
            public AudioClip AudioClip
            {
                get => SerializedPropertyHelper.GetAssetPathField<AudioClip>(m_AudioClipProperty);
                set => SerializedPropertyHelper.SetAssetPathField(m_AudioClipProperty, value);
            }

            public bool OnGUI()
            {
                if (!SearchText.IsNullOrEmpty())
                {
                    if (!AudioClipPath.ToLower().Contains(SearchText.ToLower())) return false;
                }

                m_Property.isExpanded = CoreGUI.LabelToggle(m_Property.isExpanded, AudioClipPath, 12, TextAnchor.MiddleLeft);

                if (!m_Property.isExpanded)
                {
                    return true;
                }

                EditorGUI.indentLevel++;
                using (new CoreGUI.BoxBlock(Color.blue))
                {
                    foreach (var item in m_Property.ForEachChild())
                    {
                        EditorGUILayout.PropertyField(item);
                    }
                }
                EditorGUI.indentLevel--;

                return true;
            }
        }

        private SearchField 
            m_FriendlyNameSearchField, m_DataSearchField;

        private void OnEnable()
        {
            m_FriendlyNamesProperty = GetSerializedProperty("m_FriendlyNames");
            m_DataProperty = GetSerializedProperty("m_Data");

            //
            m_FriendlyNames = new List<FriendlyName>();
            for (int i = 0; i < m_FriendlyNamesProperty.arraySize; i++)
            {
                m_FriendlyNames.Add(new FriendlyName(m_FriendlyNamesProperty.GetArrayElementAtIndex(i)));
            }
            m_FriendlyNameSearchField = new SearchField();
            //
            m_Data = new List<Data>();
            for (int i = 0; i < m_DataProperty.arraySize; i++)
            {
                m_Data.Add(new Data(m_DataProperty.GetArrayElementAtIndex(i)));
            }
            m_DataSearchField = new SearchField();
        }

        protected override void OnInspectorGUIContents()
        {
            EditorGUILayout.Space();

            using (new CoreGUI.BoxBlock(Color.black))
            {
                m_FriendlyNamesProperty.isExpanded 
                    = CoreGUI.LabelToggle(m_FriendlyNamesProperty.isExpanded, FriendlyName.Header, 15, TextAnchor.MiddleCenter);

                if (m_FriendlyNamesProperty.isExpanded)
                {
                    FriendlyName.SearchText = m_FriendlyNameSearchField.OnGUI(FriendlyName.SearchText);

                    for (int i = 0; i < m_FriendlyNames.Count; i++)
                    {
                        if (m_FriendlyNames[i].OnGUI() && i + 1 < m_FriendlyNames.Count)
                        {
                            CoreGUI.Line();
                        }
                    }
                }
            }

            CoreGUI.Line();

            using (new CoreGUI.BoxBlock(Color.black))
            {
                m_DataProperty.isExpanded
                    = CoreGUI.LabelToggle(m_DataProperty.isExpanded, Data.Header, 15, TextAnchor.MiddleCenter);

                if (m_DataProperty.isExpanded)
                {
                    Data.SearchText = m_DataSearchField.OnGUI(Data.SearchText);

                    for (int i = 0; i < m_Data.Count; i++)
                    {
                        if (m_Data[i].OnGUI() && i + 1 < m_FriendlyNames.Count)
                        {
                            CoreGUI.Line();
                        }
                    }
                }
            }

            CoreGUI.Line();

            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif