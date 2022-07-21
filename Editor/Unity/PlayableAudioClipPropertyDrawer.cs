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

using Point.Audio.LowLevel;
using Point.Collections;
using Point.Collections.Editor;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Point.Audio.Editor
{
    [CustomPropertyDrawer(typeof(PlayableAudioClip))]
    internal sealed class PlayableAudioClipPropertyDrawer : PropertyDrawerUXML<PlayableAudioClip>
    {
        protected override VisualElement CreateVisualElement(SerializedProperty property)
        {
            UnityEngine.Object obj = property.serializedObject.targetObject;
            SerializedProperty
                pathProp = property.FindPropertyRelative("m_Clip"),
                volumeSamplesProp = property.FindPropertyRelative("m_Volumes");

            VisualElement root = new VisualElement();
            root.styleSheets.Add(CoreGUI.VisualElement.DefaultStyleSheet);
            root.AddToClassList("content-container");

            AssetPathFieldView pathFieldView = new AssetPathFieldView(pathProp)
            {
                label = pathProp.displayName,
                objectType = TypeHelper.TypeOf<AudioClip>.Type
            };
            root.Add(pathFieldView);
            
            string volumeSamplesPath = volumeSamplesProp.propertyPath;
            AudioSample[] volumeSamples 
                = SerializedPropertyHelper.ReadArray<AudioSample>(volumeSamplesProp);

            FoldoutView foldout = new FoldoutView("Settings");
            AudioClipGraphView clipGraphView = new AudioClipGraphView(volumeSamples);
            clipGraphView.VolumeSampleSetter = (t) =>
            {
                if (obj == null) return;

                using (SerializedObject serialized = new SerializedObject(obj))
                {
                    var prop = serialized.FindProperty(volumeSamplesPath);
                    SerializedPropertyHelper.WriteArray(prop, t, (t, ta) =>
                    {
                        t.FindPropertyRelative("position").intValue = ta.position;
                        t.FindPropertyRelative("value").floatValue = ta.value;
                    });

                    serialized.ApplyModifiedProperties();
                }
            };
            foldout.Add(clipGraphView);
            root.Add(foldout);

            pathFieldView.RegisterValueChangedCallback(t =>
            {
                AudioClip clip = pathFieldView.objectValue as AudioClip;
                clipGraphView.originalClip = clip;
            });

            return root;
        }
    }
}

#endif