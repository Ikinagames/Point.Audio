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

#if UNITY_2019_1_OR_NEWER
#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !POINT_DISABLE_CHECKS
#define DEBUG_MODE
#endif
#define UNITYENGINE

using Point.Collections;
using Point.Collections.Editor;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Point.Audio.Editor
{
    [CustomPropertyDrawer(typeof(PlayableAudioClip))]
    internal sealed class PlayableAudioClipPropertyDrawer : PropertyDrawerUXML<PlayableAudioClip>
    {
        private struct xLineComparer : IComparer<Vector3>
        {
            public int Compare(Vector3 x, Vector3 y)
            {
                if (x.x < y.x) return -1;
                else if (x.x > y.x) return 1;
                else return 0;
            }
        }
        private sealed class VolumeSample
        {
            private PinPoint m_Pin;
            public float samplePosition;
            public float volume;

            public PinPoint Pin => m_Pin;

            public VolumeSample(VisualElement parent, float x, float y)
            {
                m_Pin = new PinPoint();
                parent.Add(m_Pin);

                samplePosition = x;
                volume = y;
            }

            public float CalculateHeight(float maxHeight)
            {
                return volume * maxHeight;
            }
        }
        private sealed class PinPoint : VisualElement
        {
            public Vector3 position
            {
                get => new Vector3(style.translate.value.x.value + (size.x * .5f), style.translate.value.y.value + (size.y * .5f));
                set
                {
                    style.translate = new StyleTranslate(new Translate(
                        value.x - 7.5f, value.y - 7.5f, 0));
                }
            }
            private Vector2 size
            {
                get => resolvedStyle.scale.value;
            }

            public PinPoint()
            {
                styleSheets.Add(CoreGUI.VisualElement.IconStyleSheet);
                this.AddToClassList("dot-image");
                this.AddToClassList("dot-fill");

                style.position = Position.Absolute;
            }
        } 
        private sealed class PlayableAudioClipView : BindableElement
        {
            private UnityEngine.Object target;

            AssetPathFieldView assetPathView;
            AudioClipTextureView audioClipTextureView;
            List<VolumeSample> m_VolumeSamplePositions = new List<VolumeSample>();

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

                audioClipTextureView.RegisterCallback<MouseDownEvent>(OnTextureMouseDown);
                audioClipTextureView.generateVisualContent += GenerateVisualContent;
            }

            private void OnAssetChanged(ChangeEvent<string> e)
            {
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(e.newValue);

                audioClipTextureView.audioClip = clip;
            }

            
            //private int FindClosestVolumePosition(Vector2 localPoint)
            //{
            //    for (int i = 0; i < m_VolumeSamplePositions.Count; i++)
            //    {
            //        if (m_VolumeSamplePositions[i].)
            //    }
            //}

            private void OnTextureMouseDown(MouseDownEvent e)
            {
                $"{e.localMousePosition}".ToLog();

                if (e.button == 0)
                {

                }
                // right button
                else if (e.button == 1)
                {
                    int sampleCount = audioClipTextureView.audioClip.samples;
                    float 
                        height = audioClipTextureView.resolvedStyle.height,
                        width = audioClipTextureView.resolvedStyle.width,
                        samplePerPixel = sampleCount / width;

                    float 
                        targetSamplePosition = e.localMousePosition.x * samplePerPixel,
                        targetVolume = e.localMousePosition.y / height;

                    $"{sampleCount} :: {targetSamplePosition} :: {targetVolume}".ToLog();

                    VolumeSample volume = new VolumeSample(audioClipTextureView, targetSamplePosition, targetVolume);
                    volume.Pin.position = e.localMousePosition;

                    m_VolumeSamplePositions.Add(volume);
                    audioClipTextureView.MarkDirtyRepaint();
                }
            }
            private void GenerateVisualContent(MeshGenerationContext ctx)
            {
                //float halfHeight = audioClipTextureView.resolvedStyle.height * .25f;
                int sampleCount = audioClipTextureView.audioClip.samples;
                float
                    height = audioClipTextureView.resolvedStyle.height,
                    width = audioClipTextureView.resolvedStyle.width,
                    samplePerPixel = sampleCount / width;

                List<Vector3> positions = new List<Vector3>();
                for (int i = 0; i < m_VolumeSamplePositions.Count; i++)
                {
                    float x = m_VolumeSamplePositions[i].samplePosition / samplePerPixel;
                    positions.Add(new Vector3(x, m_VolumeSamplePositions[i].CalculateHeight(height)));
                }
                positions.Sort(new xLineComparer());

                positions.Insert(0, new Vector3(0, 0));
                positions.Add(new Vector3(width, 0));

                CoreGUI.VisualElement.DrawCable(positions.ToArray(), 1, Color.green, ctx);
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