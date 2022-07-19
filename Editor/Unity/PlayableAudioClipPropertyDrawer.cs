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
        private struct xLineComparer : IComparer<Vector3>
        {
            public int Compare(Vector3 x, Vector3 y)
            {
                if (x.x < y.x) return -1;
                else if (x.x > y.x) return 1;
                else return 0;
            }
        }
        private sealed class VolumeSample : PinPoint<AudioSample>, IComparable<VolumeSample>
        {
            public VolumeSample(VisualElement parent, float x, float y) : base(parent)
            {
                value = new AudioSample(x, y);
            }

            public float CalculateHeight(float maxHeight)
            {
                return value.value * maxHeight;
            }

            public int CompareTo(VolumeSample other)
            {
                if (value.position < other.value.position) return -1;
                else if (value.position > other.value.position) return 1;
                return 0;
            }
        }
        
        private sealed class PlayableAudioClipView : BindableElement
        {
            private UnityEngine.Object target;

            AssetPathFieldView assetPathView;

            RulerView rulerView;
            AudioClipTextureView audioClipTextureView;
            List<VolumeSample> m_VolumeSamplePositions = new List<VolumeSample>();

            string m_ClipPath, m_TargetChannelsPath, m_VolumesPath;

            public PlayableAudioClipView(SerializedProperty property)
            {
                target = property.serializedObject.targetObject;
                styleSheets.Add(CoreGUI.VisualElement.DefaultStyleSheet);
                AddToClassList("content-container");

                SerializedProperty
                    clipProp = property.FindPropertyRelative("m_Clip"),
                    volumesProp = property.FindPropertyRelative("m_Volumes");
                m_ClipPath = clipProp.propertyPath;
                m_TargetChannelsPath = property.FindPropertyRelative("m_TargetChannels").propertyPath;
                m_VolumesPath = volumesProp.propertyPath;

                assetPathView = new AssetPathFieldView()
                {
                    objectType = TypeHelper.TypeOf<UnityEngine.AudioClip>.Type,
                    label = property.displayName
                };
                //rulerView = new RulerView();
                audioClipTextureView = new AudioClipTextureView()
                {
                    maxHeight = 1
                };

                Add(assetPathView);
                //Add(rulerView);
                Add(audioClipTextureView);
                //rulerView.Add(new Button());

                //audioClipTextureView.StretchToParentWidth();

                if (SerializedPropertyHelper.GetAssetPathField(clipProp) != null)
                {
                    assetPathView.objectValue = SerializedPropertyHelper.GetAssetPathField(clipProp);
                }

                audioClipTextureView.RegisterCallback<MouseDownEvent>(OnTextureMouseDown);
                audioClipTextureView.contentContainer.generateVisualContent += GenerateVisualContent;
                assetPathView.RegisterValueChangedCallback(OnAssetChanged);

                Button deleteBtt = new Button(ResetButton);
                deleteBtt.text = "Reset";
                Add(deleteBtt);

                Setup(false);
            }
            private void Setup(bool reset)
            {
                audioClipTextureView.Clear();
                m_VolumeSamplePositions.Clear();

                AudioClip clip = null;
                using (SerializedObject obj = new SerializedObject(target))
                {
                    clip = assetPathView.objectValue as AudioClip;
                    SerializedProperty 
                        volumesProp = obj.FindProperty(m_VolumesPath);

                    if (clip == null)
                    {
                        volumesProp.ClearArray();
                    }
                    if (reset && clip != null)
                    {
                        for (int i = 0; i < clip.channels; i++)
                        {
                            volumesProp.InsertArrayElementAtIndex(0);
                            var element = volumesProp.GetArrayElementAtIndex(0);

                            element.FindPropertyRelative("position").intValue = clip.samples;
                            element.FindPropertyRelative("value").floatValue = 1;
                        }
                        for (int i = 0; i < clip.channels; i++)
                        {
                            volumesProp.InsertArrayElementAtIndex(0);
                            var element = volumesProp.GetArrayElementAtIndex(0);

                            element.FindPropertyRelative("position").intValue = 0;
                            element.FindPropertyRelative("value").floatValue = 1;
                        }
                    }

                    obj.ApplyModifiedProperties();
                }

                schedule.Execute(RepaintAudioClipView);
            }
            private void ResetButton()
            {
                using (SerializedObject obj = new SerializedObject(target))
                {
                    SerializedProperty volumesProp = obj.FindProperty(m_VolumesPath);

                    volumesProp.ClearArray();

                    obj.ApplyModifiedProperties();
                }

                Setup(true);
            }
            private void BakeButton()
            {
                AudioClip originalClip = assetPathView.objectValue as AudioClip;

                AudioClip clip = AudioClip.Create(originalClip.name, originalClip.samples, originalClip.channels, originalClip.frequency, false);

                float[] samples = new float[clip.samples * clip.channels];
                originalClip.GetData(samples, 0);
                
                using (SerializedObject obj = new SerializedObject(target))
                {
                    AudioSample[] volumeSamples = SerializedPropertyHelper.ReadArray<AudioSample>(obj.FindProperty(m_VolumesPath));
                    volumeSamples = DSP.Evaluate(clip, volumeSamples);

                    DSP.Volume(samples, clip.channels, volumeSamples, clip.channels);
                    clip.SetData(samples, 0);

                    obj.ApplyModifiedProperties();
                }

                audioClipTextureView.audioClip = clip;
                //audioClipTextureView.MarkDirtyRepaint();
                MarkDirtyRepaint();
            }

            private void Save()
            {
                AudioClip clip = assetPathView.objectValue as AudioClip;
                int packSize = clip.channels;

                using (SerializedObject obj = new SerializedObject(target))
                {
                    SerializedProperty
                        clipProp = obj.FindProperty(m_ClipPath),
                        channelsProp = obj.FindProperty(m_TargetChannelsPath),
                        volumesProp = obj.FindProperty(m_VolumesPath);

                    volumesProp.ClearArray();
                    channelsProp.intValue = packSize;

                    m_VolumeSamplePositions.Sort();
                    for (int i = 0, p = 0; i < m_VolumeSamplePositions.Count; i++, p = i * packSize)
                    {
                        AudioSample sample = m_VolumeSamplePositions[i].value;
                        for (int j = 0; j < packSize; j++)
                        {
                            volumesProp.InsertArrayElementAtIndex(p + j);
                            SerializedProperty element = volumesProp.GetArrayElementAtIndex(p + j);

                            element.FindPropertyRelative(nameof(AudioSample.position))
                                .intValue = sample.position - (sample.position % packSize);
                            element.FindPropertyRelative(nameof(AudioSample.value))
                                .floatValue = sample.value;
                        }
                    }

                    obj.ApplyModifiedProperties();

                    Assert.AreEqual(m_VolumeSamplePositions.Count * packSize, volumesProp.arraySize);
                }

                BakeButton();
            }

            private void OnAssetChanged(ChangeEvent<string> e)
            {
                //AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(e.newValue);
                using (SerializedObject obj = new SerializedObject(target))
                {
                    SerializedPropertyHelper.SetAssetPathFieldPath(obj.FindProperty(m_ClipPath), e.newValue);
                    //$"{e.newValue}".ToLog();
                    obj.ApplyModifiedProperties();
                }

                ResetButton();
            }

            private void OnTextureMouseDown(MouseDownEvent e)
            {
                AudioClip clip = assetPathView.objectValue as AudioClip;

                //if (e.button == 0)
                //{
                //    var query = audioClipTextureView.Query<VolumeSample>().Where(t => t.localBound.Contains(e.localMousePosition));
                //    var found = audioClipTextureView.FindClosestElement(e.localMousePosition, query);

                //    $"{e.localMousePosition} : {found?.transform.position}".ToLog();
                //}
                // right button
                if (e.button == 1)
                {
                    int sampleCount = clip.samples;
                    float 
                        height = audioClipTextureView.height,
                        width = audioClipTextureView.width,
                        samplePerPixel = sampleCount / width;

                    float 
                        targetSamplePosition = e.localMousePosition.x * samplePerPixel,
                        targetVolume = 1 - (e.localMousePosition.y / height);

                    //$"{sampleCount} :: {targetSamplePosition} :: {targetVolume}".ToLog();

                    VolumeSampleFactory(Mathf.RoundToInt(targetSamplePosition), targetVolume);
                    //audioClipTextureView.MarkDirtyRepaint();

                    Save();
                }
            }
            private void OnVolumeSamplePositionMoved(PinPoint<AudioSample> pin, Vector3 pos)
            {
                OnVolumeSamplePositionMoving(pin, pos);

                Save();
            }
            private void OnVolumeSamplePositionMoving(PinPoint<AudioSample> pin, Vector3 pos)
            {
                AudioClip clip = assetPathView.objectValue as AudioClip;

                int sampleCount = clip.samples;
                float
                    height = audioClipTextureView.height,
                    width = audioClipTextureView.width,
                    samplePerPixel = sampleCount / width;

                float
                    targetSamplePosition = Mathf.Clamp(pos.x * samplePerPixel, 0, sampleCount),
                    targetVolume = Mathf.Clamp01(1 - (pos.y / height));

                pin.value = new AudioSample(targetSamplePosition, targetVolume);
                //audioClipTextureView.MarkDirtyRepaint();
            }
            private VolumeSample VolumeSampleFactory(int position, float value)
            {
                VolumeSample volume = new VolumeSample(audioClipTextureView,
                                    position,
                                    value);
                volume.OnDragEnded += OnVolumeSamplePositionMoved;
                volume.OnDrag += OnVolumeSamplePositionMoving;

                audioClipTextureView.Add(volume);
                m_VolumeSamplePositions.Add(volume);

                volume.RegisterCallback<MouseDownEvent>(delegate (MouseDownEvent e)
                {
                    if (e.button != 1) return;

                    audioClipTextureView.Remove(volume);
                    m_VolumeSamplePositions.Remove(volume);

                    //audioClipTextureView.MarkDirtyRepaint();
                    Save();

                    e.StopImmediatePropagation();
                });

                return volume;
            }

            private void RepaintAudioClipView()
            {
                audioClipTextureView.Clear();
                m_VolumeSamplePositions.Clear();

                AudioClip clip = assetPathView.objectValue as AudioClip;
                if (clip == null)
                {
                    audioClipTextureView.audioClip = null;
                    return;
                }

                using (SerializedObject obj = new SerializedObject(target))
                {
                    SerializedProperty volumesProp = obj.FindProperty(m_VolumesPath);
                    
                    int sampleCount = clip.samples;
                    float
                        height = audioClipTextureView.height,
                        width = audioClipTextureView.width,
                        samplePerPixel = sampleCount / width;

                    for (int i = 0; i < volumesProp.arraySize; i += clip.channels)
                    {
                        var element = volumesProp.GetArrayElementAtIndex(i);
                        VolumeSampleFactory(
                            element.FindPropertyRelative("position").intValue,
                            element.FindPropertyRelative("value").floatValue);
                    }

                    //audioClipTextureView.MarkDirtyRepaint();
                    BakeButton();
                }
            }
            private void GenerateVisualContent(MeshGenerationContext ctx)
            {
                float
                    height = audioClipTextureView.height,
                    width = audioClipTextureView.width;
                List<Vector3> positions = new List<Vector3>();

                if (audioClipTextureView.audioClip == null)
                {
                    positions.Insert(0, new Vector3(0, 0));
                    positions.Add(new Vector3(width, 0));
                    CoreGUI.VisualElement.DrawCable(positions.ToArray(), 1, Color.green, ctx);

                    return;
                }

                int sampleCount = audioClipTextureView.audioClip.samples;
                float samplePerPixel = sampleCount / width;

                //$"{m_VolumeSamplePositions.Count}".ToLog();
                for (int i = 0; i < m_VolumeSamplePositions.Count; i++)
                {
                    VolumeSample target = m_VolumeSamplePositions[i];
                    float x = target.value.position / samplePerPixel;

                    positions.Add(new Vector3(x, height - target.CalculateHeight(height)));
                }
                positions.Sort(new xLineComparer());

                positions.Insert(0, new Vector3(0, height));
                positions.Add(new Vector3(width, height));

                CoreGUI.VisualElement.DrawCable(positions.ToArray(), 1, Color.green, ctx);
                audioClipTextureView.schedule.Execute(delegate ()
                {
                    for (int i = 0; i < m_VolumeSamplePositions.Count; i++)
                    {
                        VolumeSample target = m_VolumeSamplePositions[i];
                        float
                            targetX = target.value.position / samplePerPixel,
                            targetY = height - target.value.value * height;

                        target.position = new Vector3(targetX, targetY);
                    }
                });
            }
        }

        protected override VisualElement CreateVisualElement(SerializedProperty property)
        {
            return new PlayableAudioClipView(property);
            //return base.CreateVisualElement(property);
        }
    }

    public class AudioClipGraphView : AudioClipTextureView
    {
        private sealed class SamplePin : PinPoint<AudioSample>, IComparable<SamplePin>
        {
            public SamplePin(VisualElement parent, float x, float y) : base(parent)
            {
                value = new AudioSample(x, y);
            }

            public float CalculateHeight(float maxHeight)
            {
                return value.value * maxHeight;
            }

            public int CompareTo(SamplePin other)
            {
                if (value.position < other.value.position) return -1;
                else if (value.position > other.value.position) return 1;
                return 0;
            }
        }

        public AudioClipGraphView() : base()
        {
            contentContainer.RegisterCallback<MouseDownEvent>(OnTextureMouseDown);
        }

        private void OnTextureMouseDown(MouseDownEvent e)
        {
            // right button
            if (e.button == 1)
            {
                int sampleCount = audioClip.samples;
                float
                    height = this.height,
                    width = this.width,
                    samplePerPixel = sampleCount / width;

                float
                    targetSamplePosition = e.localMousePosition.x * samplePerPixel,
                    targetVolume = 1 - (e.localMousePosition.y / height);

                //$"{sampleCount} :: {targetSamplePosition} :: {targetVolume}".ToLog();

                //VolumeSampleFactory(Mathf.RoundToInt(targetSamplePosition), targetVolume);
                //Save();
            }
        }
    }
}

#endif