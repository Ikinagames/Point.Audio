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
using UnityEngine;
using UnityEngine.UIElements;

namespace Point.Audio.Editor
{
    [CustomPropertyDrawer(typeof(PlayableAudioClip))]
    internal sealed class PlayableAudioClipPropertyDrawer : PropertyDrawerUXML<PlayableAudioClip>
    {
        private sealed class PlayableAudioClipView : BindableElement
        {
            private UnityEngine.Object target;

            AssetPathFieldView assetPathView;
            AudioClipTextureView audioClipTextureView;

            public PlayableAudioClipView(SerializedProperty property)
            {
                styleSheets.Add(CoreGUI.VisualElement.DefaultStyleSheet);
                AddToClassList("content-container");

                target = property.serializedObject.targetObject;

                assetPathView = new AssetPathFieldView(property.FindPropertyRelative("m_Clip"))
                {
                    label = property.displayName
                };
                assetPathView.RegisterValueChangedCallback(OnAssetChanged);

                audioClipTextureView = new AudioClipTextureView();

                Add(assetPathView);
                Add(audioClipTextureView);

                audioClipTextureView.generateVisualContent += GenerateVisualContent;
            }

            private void OnAssetChanged(ChangeEvent<string> e)
            {
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(e.newValue);

                audioClipTextureView.audioClip = clip;
            }

            private void GenerateVisualContent(MeshGenerationContext ctx)
            {
                float halfHeight = audioClipTextureView.resolvedStyle.height * .25f;
                float width = audioClipTextureView.resolvedStyle.width;
                CoreGUI.VisualElement.DrawCable(new Vector3[]
                {
                    new Vector3(0, halfHeight),
                    new Vector3(width, halfHeight)
                }, .5f, Color.white, ctx);
            }
        }

        protected override VisualElement CreateVisualElement(SerializedProperty property)
        {
            return new PlayableAudioClipView(property);
            //return base.CreateVisualElement(property);
        }
    }
}

#endif