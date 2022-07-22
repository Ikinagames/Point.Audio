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
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Point.Audio.Editor
{
    public class AudioClipGraphView : AudioClipTextureView
    {
        private struct xLineComparer : IComparer<SamplePin>
        {
            public int Compare(SamplePin x, SamplePin y)
            {
                if (x.value.position < y.value.position) return -1;
                else if (x.value.position > y.value.position) return 1;
                else return 0;
            }
        }
        private sealed class SamplePin : PinPoint<AudioSample>, IComparable<SamplePin>
        {
            private AudioClipGraphView root;

            public SamplePin(AudioClipGraphView root, float x, float y) : base(root.contentContainer)
            {
                this.root = root;
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
                root.RepaintTexture();

                e.StopImmediatePropagation();
            }
        }

        private AudioClip m_OriginalClip;
        private List<SamplePin> m_VolumeSamplePositions = new List<SamplePin>();

        public Action<AudioSample[]> VolumeSampleSetter;

        public AudioClip originalClip 
        { 
            get => m_OriginalClip;
            set
            {
                m_OriginalClip = value;
                audioClip = value;

                if (value == null)
                {
                    volumeSamples = null;
                }
                else
                {
                    int channel = value.channels;
                    int length = value.samples;
                    AudioSample[] temp = new AudioSample[2]
                    {
                        new AudioSample(0, 1),
                        new AudioSample(length, 1),
                    };

                    volumeSamples = temp;
                }

                Save();
                RepaintTexture();
            }
        }
        
        public AudioSample[] volumeSamples
        {
            get => m_VolumeSamplePositions == null || m_VolumeSamplePositions.Count == 0 ? Array.Empty<AudioSample>() : m_VolumeSamplePositions.Select(t => t.value).ToArray();
            set
            {
                Clear();
                if (value.IsNullOrEmpty()) return;

                m_VolumeSamplePositions = value.Select(CreateSamplePin).ToList();
                for (int i = 0; i < m_VolumeSamplePositions.Count; i++)
                {
                    Add(m_VolumeSamplePositions[i]);
                }
            }
        }

        public AudioClipGraphView(AudioClip clip, AudioSample[] volumes) : base()
        {
            contentContainer.RegisterCallback<MouseDownEvent>(OnTextureMouseDown);
            contentContainer.generateVisualContent += GenerateVisualContent;

            m_OriginalClip = clip;
            maxHeight = 1;

            volumeSamples = volumes;

            RepaintTexture();
            //m_VolumeSamplePositions = volumes.Select(CreateSamplePin).ToList();
            //for (int i = 0; i < m_VolumeSamplePositions.Count; i++)
            //{
            //    Add(m_VolumeSamplePositions[i]);
            //}
        }

        public void Save()
        {
            m_VolumeSamplePositions.Sort(new xLineComparer());
            VolumeSampleSetter?.Invoke(volumeSamples);
        }

        private void OnTextureMouseDown(MouseDownEvent e)
        {
            // right button
            if (e.button == 1)
            {
                Vector3 pos = contentContainer.WorldToLocal(e.mousePosition);
                int sampleCount = audioClip.samples;
                float
                    samplePerPixel = sampleCount / width;

                float
                    targetSamplePosition = Mathf.Clamp(pos.x * samplePerPixel, 0, sampleCount),
                    targetVolume = Mathf.Clamp01(1 - (pos.y / height));

                //$"{sampleCount} :: {targetSamplePosition} :: {targetVolume}".ToLog();

                VolumeSampleFactory(Mathf.RoundToInt(targetSamplePosition), targetVolume);
                Save();
                RepaintTexture();
            }
        }
        private void OnVolumeSamplePositionMoved(PinPoint<AudioSample> pin, Vector3 pos)
        {
            OnVolumeSamplePositionMoving(pin, pos);

            Save();
            RepaintTexture();
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

        public override void RepaintTexture()
        {
            base.RepaintTexture();

            AudioSample[] volumeSamples = this.volumeSamples;

            if (originalClip == null)
            {
                audioClip = null;

                Clear();
                m_VolumeSamplePositions.Clear();
                return;
            }

            {
                if (audioClip != null && audioClip != originalClip)
                {
                    UnityEngine.Object.DestroyImmediate(audioClip);
                }

                AudioClip clip = AudioClip.Create(originalClip.name, originalClip.samples, originalClip.channels, originalClip.frequency, false);

                float[] samples = new float[clip.samples * clip.channels];
                originalClip.GetData(samples, 0);

                volumeSamples = DSP.Evaluate(clip, volumeSamples);

                DSP.Volume(samples, clip.channels, volumeSamples, clip.channels, 0, clip.samples);
                clip.SetData(samples, 0);

                audioClip = clip;
            }
        }
        private void GenerateVisualContent(MeshGenerationContext ctx)
        {
            List<Vector3> positions = new List<Vector3>();

            if (audioClip == null)
            {
                positions.Insert(0, new Vector3(0, 0));
                positions.Add(new Vector3(width, 0));
                CoreGUI.VisualElement.DrawCable(positions.ToArray(), 1, Color.green, ctx);

                return;
            }

            int sampleCount = audioClip.samples;
            float samplePerPixel = sampleCount / width;

            //$"{m_VolumeSamplePositions.Count}".ToLog();
            for (int i = 0; i < m_VolumeSamplePositions?.Count; i++)
            {
                SamplePin target = m_VolumeSamplePositions[i];
                float x = target.value.position / samplePerPixel;

                positions.Add(new Vector3(x, height - target.CalculateHeight(height)));
            }
            //positions.Sort(new xLineComparer());

            positions.Insert(0, new Vector3(0, height));
            positions.Add(new Vector3(width, height));

            CoreGUI.VisualElement.DrawCable(positions.ToArray(), 1, Color.green, ctx);
            schedule.Execute(delegate ()
            {
                for (int i = 0; i < m_VolumeSamplePositions?.Count; i++)
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