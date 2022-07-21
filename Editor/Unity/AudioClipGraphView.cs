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
using Point.Collections.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Point.Audio.Editor
{
    public class AudioClipGraphView : AudioClipTextureView
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
        private sealed class SamplePin : PinPoint<AudioSample>, IComparable<SamplePin>
        {
            private AudioClipGraphView root => manipulator.root as AudioClipGraphView;

            public SamplePin(AudioClipGraphView root, float x, float y) : base(root)
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

            public void OnMouseDownEventHandler(MouseDownEvent e)
            {
                if (e.button != 1) return;

                RemoveFromHierarchy();
                root.m_VolumeSamplePositions.Remove(this);

                root.Save();

                e.StopImmediatePropagation();
            }
        }

        private AudioClip m_ModifiedClip;
        private List<SamplePin> m_VolumeSamplePositions;

        public Action<AudioSample[]> VolumeSampleSetter;

        public AudioClip originalClip 
        { 
            get => m_AudioClip;
            set
            {
                m_AudioClip = value;
                UpdateAudioClip();
            }
        }
        public override AudioClip audioClip { get => m_ModifiedClip; set => m_ModifiedClip = value; }

        public AudioSample[] volumeSamples => m_VolumeSamplePositions.Select(t => t.value).ToArray();

        public AudioClipGraphView(AudioSample[] volumes) : base()
        {
            contentContainer.RegisterCallback<MouseDownEvent>(OnTextureMouseDown);
            contentContainer.generateVisualContent += GenerateVisualContent;

            m_VolumeSamplePositions = volumes.Select(CreateSamplePin).ToList();
            for (int i = 0; i < m_VolumeSamplePositions.Count; i++)
            {
                Add(m_VolumeSamplePositions[i]);
            }
        }

        public void Save()
        {
            VolumeSampleSetter?.Invoke(volumeSamples);
        }

        private void OnTextureMouseDown(MouseDownEvent e)
        {
            // right button
            if (e.button == 1)
            {
                int sampleCount = m_ModifiedClip.samples;
                float
                    samplePerPixel = sampleCount / width;

                float
                    targetSamplePosition = e.localMousePosition.x * samplePerPixel,
                    targetVolume = 1 - (e.localMousePosition.y / height);

                //$"{sampleCount} :: {targetSamplePosition} :: {targetVolume}".ToLog();

                VolumeSampleFactory(Mathf.RoundToInt(targetSamplePosition), targetVolume);
                Save();
                UpdateAudioClip();
            }
        }
        private void OnVolumeSamplePositionMoved(PinPoint<AudioSample> pin, Vector3 pos)
        {
            OnVolumeSamplePositionMoving(pin, pos);

            Save();
            UpdateAudioClip();
        }
        private void OnVolumeSamplePositionMoving(PinPoint<AudioSample> pin, Vector3 pos)
        {
            int sampleCount = audioClip.samples;
            float
                samplePerPixel = sampleCount / width;

            float
                targetSamplePosition = Mathf.Clamp(pos.x * samplePerPixel, 0, sampleCount),
                targetVolume = Mathf.Clamp01(1 - (pos.y / height));

            pin.value = new AudioSample(targetSamplePosition, targetVolume);
        }
        private SamplePin CreateSamplePin(AudioSample sample)
        {
            SamplePin volume = new SamplePin(this,
                               sample.position,
                               sample.value);
            volume.OnDragEnded += OnVolumeSamplePositionMoved;
            volume.OnDrag += OnVolumeSamplePositionMoving;

            volume.RegisterCallback<MouseDownEvent>(volume.OnMouseDownEventHandler);

            return volume;
        }
        private SamplePin VolumeSampleFactory(int position, float value)
        {
            SamplePin volume = CreateSamplePin(new AudioSample(position, value));

            Add(volume);
            m_VolumeSamplePositions.Add(volume);
            return volume;
        }

        private void UpdateAudioClip()
        {
            AudioSample[] volumeSamples = this.volumeSamples;

            //Clear();
            //m_VolumeSamplePositions.Clear();

            if (m_AudioClip == null)
            {
                m_ModifiedClip = null;
                return;
            }

            {
                //for (int i = 0; i < volumeSamples.Length; i += originalClip.channels)
                //{
                //    VolumeSampleFactory(volumeSamples[i].position, volumeSamples[i].value);
                //}

                AudioClip clip = AudioClip.Create(m_AudioClip.name, m_AudioClip.samples, m_AudioClip.channels, m_AudioClip.frequency, false);

                float[] samples = new float[clip.samples * clip.channels];
                m_AudioClip.GetData(samples, 0);

                volumeSamples = DSP.Evaluate(clip, volumeSamples);

                DSP.Volume(samples, clip.channels, volumeSamples, clip.channels);
                clip.SetData(samples, 0);

                m_ModifiedClip = clip;
            }
        }
        private void GenerateVisualContent(MeshGenerationContext ctx)
        {
            List<Vector3> positions = new List<Vector3>();

            if (m_ModifiedClip == null)
            {
                positions.Insert(0, new Vector3(0, 0));
                positions.Add(new Vector3(width, 0));
                CoreGUI.VisualElement.DrawCable(positions.ToArray(), 1, Color.green, ctx);

                return;
            }

            int sampleCount = m_ModifiedClip.samples;
            float samplePerPixel = sampleCount / width;

            //$"{m_VolumeSamplePositions.Count}".ToLog();
            for (int i = 0; i < m_VolumeSamplePositions.Count; i++)
            {
                SamplePin target = m_VolumeSamplePositions[i];
                float x = target.value.position / samplePerPixel;

                positions.Add(new Vector3(x, height - target.CalculateHeight(height)));
            }
            positions.Sort(new xLineComparer());

            positions.Insert(0, new Vector3(0, height));
            positions.Add(new Vector3(width, height));

            CoreGUI.VisualElement.DrawCable(positions.ToArray(), 1, Color.green, ctx);
            schedule.Execute(delegate ()
            {
                for (int i = 0; i < m_VolumeSamplePositions.Count; i++)
                {
                    SamplePin target = m_VolumeSamplePositions[i];
                    float
                        targetX = target.value.position / samplePerPixel,
                        targetY = height - target.value.value * height;

                    target.position = new Vector3(targetX, targetY);
                }
            });
        }
    }
}

#endif