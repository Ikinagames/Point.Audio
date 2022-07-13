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
        private sealed class VolumeSample : PinPoint<PlayableAudioClip.Sample>
        {
            public VolumeSample(VisualElement parent, float x, float y) : base(parent)
            {
                value = new PlayableAudioClip.Sample(x, y);
            }

            public float CalculateHeight(float maxHeight)
            {
                return value.value * maxHeight;
            }
        }
        
        private sealed class PlayableAudioClipView : BindableElement
        {
            private UnityEngine.Object target;

            AssetPathFieldView assetPathView;
            AudioClipTextureView audioClipTextureView;
            List<VolumeSample> m_VolumeSamplePositions = new List<VolumeSample>();

            string m_ClipPath, m_VolumesPath;

            public PlayableAudioClipView(SerializedProperty property)
            {
                target = property.serializedObject.targetObject;
                styleSheets.Add(CoreGUI.VisualElement.DefaultStyleSheet);
                AddToClassList("content-container");

                SerializedProperty
                    clipProp = property.FindPropertyRelative("m_Clip"),
                    volumesProp = property.FindPropertyRelative("m_Volumes");
                m_ClipPath = clipProp.propertyPath;
                m_VolumesPath = volumesProp.propertyPath;

                assetPathView = new AssetPathFieldView()
                {
                    objectType = TypeHelper.TypeOf<UnityEngine.AudioClip>.Type,
                    label = property.displayName
                };
                audioClipTextureView = new AudioClipTextureView();

                Add(assetPathView);
                Add(audioClipTextureView);

                if (SerializedPropertyHelper.GetAssetPathField(clipProp) != null)
                {
                    assetPathView.objectValue = SerializedPropertyHelper.GetAssetPathField(clipProp);
                }

                audioClipTextureView.RegisterCallback<MouseDownEvent>(OnTextureMouseDown);
                audioClipTextureView.generateVisualContent += GenerateVisualContent;
                assetPathView.RegisterValueChangedCallback(OnAssetChanged);

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
                    SerializedProperty volumesProp = obj.FindProperty(m_VolumesPath);

                    if (clip == null)
                    {
                        volumesProp.ClearArray();
                    }
                    if (reset && clip != null)
                    {
                        volumesProp.InsertArrayElementAtIndex(0);
                        volumesProp.InsertArrayElementAtIndex(1);
                        SerializedProperty
                            firstElement = volumesProp.GetArrayElementAtIndex(0),
                            lastElement = volumesProp.GetArrayElementAtIndex(1);

                        firstElement.FindPropertyRelative("position").intValue = 1;
                        firstElement.FindPropertyRelative("value").floatValue = 1;
                        lastElement.FindPropertyRelative("position").intValue = clip.samples - 1;
                        lastElement.FindPropertyRelative("value").floatValue = 1;
                    }

                    obj.ApplyModifiedProperties();
                }

                audioClipTextureView.audioClip = clip;

                if (clip != null)
                {
                    audioClipTextureView.schedule.Execute(delegate ()
                    {
                        using (SerializedObject obj = new SerializedObject(target))
                        {
                            SerializedProperty volumesProp = obj.FindProperty(m_VolumesPath);

                            int sampleCount = clip.samples;
                            float
                                height = audioClipTextureView.resolvedStyle.height,
                                width = audioClipTextureView.resolvedStyle.width,
                                samplePerPixel = sampleCount / width;

                            for (int i = 0; i < volumesProp.arraySize; i++)
                            {
                                var element = volumesProp.GetArrayElementAtIndex(i);
                                VolumeSample volume = new VolumeSample(audioClipTextureView,
                                    element.FindPropertyRelative("position").intValue,
                                    element.FindPropertyRelative("value").floatValue);
                                volume.OnDragEnded += OnVolumeSamplePositionMoved;
                                volume.OnDrag += OnVolumeSamplePositionMoving;
                                audioClipTextureView.Add(volume);
                                m_VolumeSamplePositions.Add(volume);

                                float
                                    targetX = volume.value.position / samplePerPixel,
                                    targetY = height - volume.value.value * height;

                                volume.position = new Vector3(targetX, targetY);

                                $"{volume.value.value} , {volume.value.position} :: {width} , {sampleCount}  {volume.position}".ToLog();
                            }

                            audioClipTextureView.MarkDirtyRepaint();
                        }
                    });

                }

                audioClipTextureView.MarkDirtyRepaint();
            }
            private void Save()
            {
                using (SerializedObject obj = new SerializedObject(target))
                {
                    SerializedProperty
                        clipProp = obj.FindProperty(m_ClipPath),
                        volumesProp = obj.FindProperty(m_VolumesPath);

                    volumesProp.ClearArray();
                    for (int i = 0; i < m_VolumeSamplePositions.Count; i++)
                    {
                        volumesProp.InsertArrayElementAtIndex(i);
                        SerializedProperty element = volumesProp.GetArrayElementAtIndex(i);

                        element.FindPropertyRelative("position").intValue = m_VolumeSamplePositions[i].value.position;
                        element.FindPropertyRelative("value").floatValue = m_VolumeSamplePositions[i].value.value;
                    }

                    obj.ApplyModifiedProperties();
                }
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

                Setup(true);
            }

            private void OnTextureMouseDown(MouseDownEvent e)
            {
                //if (e.button == 0)
                //{
                //    var query = audioClipTextureView.Query<VolumeSample>().Where(t => t.localBound.Contains(e.localMousePosition));
                //    var found = audioClipTextureView.FindClosestElement(e.localMousePosition, query);

                //    $"{e.localMousePosition} : {found?.transform.position}".ToLog();
                //}
                // right button
                if (e.button == 1)
                {
                    int sampleCount = audioClipTextureView.audioClip.samples;
                    float 
                        height = audioClipTextureView.resolvedStyle.height,
                        width = audioClipTextureView.resolvedStyle.width,
                        samplePerPixel = sampleCount / width;

                    float 
                        targetSamplePosition = e.localMousePosition.x * samplePerPixel,
                        targetVolume = 1 - (e.localMousePosition.y / height);

                    $"{sampleCount} :: {targetSamplePosition} :: {targetVolume}".ToLog();

                    VolumeSample volume = new VolumeSample(audioClipTextureView, targetSamplePosition, targetVolume);
                    volume.OnDrag += OnVolumeSamplePositionMoving;
                    volume.OnDragEnded += OnVolumeSamplePositionMoved;
                    audioClipTextureView.Add(volume);
                    volume.position = e.localMousePosition;

                    m_VolumeSamplePositions.Add(volume);
                    audioClipTextureView.MarkDirtyRepaint();

                    Save();
                }
            }
            private void OnVolumeSamplePositionMoved(PinPoint<PlayableAudioClip.Sample> pin, Vector3 pos)
            {
                OnVolumeSamplePositionMoving(pin, pos);

                Save();
            }
            private void OnVolumeSamplePositionMoving(PinPoint<PlayableAudioClip.Sample> pin, Vector3 pos)
            {
                int sampleCount = audioClipTextureView.audioClip.samples;
                float
                    height = audioClipTextureView.resolvedStyle.height,
                    width = audioClipTextureView.resolvedStyle.width,
                    samplePerPixel = sampleCount / width;

                float
                    targetSamplePosition = pos.x * samplePerPixel,
                    targetVolume = 1 - (pos.y / height);

                pin.value = new PlayableAudioClip.Sample(targetSamplePosition, targetVolume);
                audioClipTextureView.MarkDirtyRepaint();
            }

            private void GenerateVisualContent(MeshGenerationContext ctx)
            {
                float
                    height = audioClipTextureView.resolvedStyle.height,
                    width = audioClipTextureView.resolvedStyle.width;
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
                    float x = m_VolumeSamplePositions[i].value.position / samplePerPixel;
                    positions.Add(new Vector3(x, height - m_VolumeSamplePositions[i].CalculateHeight(height)));
                }
                positions.Sort(new xLineComparer());

                positions.Insert(0, new Vector3(0, height));
                positions.Add(new Vector3(width, height));

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