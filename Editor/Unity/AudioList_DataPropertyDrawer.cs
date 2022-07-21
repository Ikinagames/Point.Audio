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

using Point.Collections.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Point.Audio.Editor
{
    [CustomPropertyDrawer(typeof(AudioList.Data))]
    internal sealed class AudioList_DataPropertyDrawer : PropertyDrawerUXML<AudioList.Data>
    {
        public sealed class AudioList_DataView : VisualElement
        {
            private UnityEngine.Object target;
            private string propertyPath;
            FoldoutView m_Top;

            public FoldoutView top => m_Top;

            public AudioList_DataView(SerializedProperty property)
            {
                target = property.serializedObject.targetObject;
                propertyPath = property.propertyPath;

                var asset = AssetHelper.LoadAsset<VisualTreeAsset>("Uxml AudioList_Data", "PointEditor");
                asset.CloneTree(this);

                FoldoutView top = this.Q<FoldoutView>();
                top.label = property.displayName;

                top.Q<AssetPathFieldView>("AudioClipField").BindProperty(property.FindPropertyRelative("m_AudioClip"));
                top.Q<AssetPathFieldView>("PrefabField").BindProperty(property.FindPropertyRelative("m_Prefab"));
                top.Q<BindableElement>("GroupField").BindProperty(property.FindPropertyRelative("m_Group"));
                top.Q<BindableElement>("IgnoreTimeField").BindProperty(property.FindPropertyRelative("m_IgnoreTime"));

                top.Q<PropertyField>("OnPlayConstActionField").BindProperty(property.FindPropertyRelative("m_OnPlayConstAction"));
                top.Q<BindableElement>("PlayOptionField").BindProperty(property.FindPropertyRelative("m_PlayOption"));
                top.Q<PropertyField>("ChildsField").BindProperty(property.FindPropertyRelative("m_Childs"));

                top.Q<PropertyField>("MasterVolumeField").BindProperty(property.FindPropertyRelative("m_MasterVolume"));
                top.Q<PropertyField>("VolumeField").BindProperty(property.FindPropertyRelative("m_Volume"));
                top.Q<PropertyField>("PitchField").BindProperty(property.FindPropertyRelative("m_Pitch"));

                m_Top = top;
                m_Top.isExpanded = property.isExpanded;

                m_Top.onExpand += M_Top_onExpand;
            }

            private void M_Top_onExpand(bool expanded)
            {
                using (SerializedObject obj = new SerializedObject(target))
                {
                    obj.FindProperty(propertyPath).isExpanded = expanded;

                    obj.ApplyModifiedProperties();
                }
            }
        }

        public static AudioList_DataView Factory(SerializedProperty property)
        {
            return new AudioList_DataView(property);
        }

        protected override VisualElement CreateVisualElement(SerializedProperty property)
        {
            return Factory(property);
        }
    }
}

#endif