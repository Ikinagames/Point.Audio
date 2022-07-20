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

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !POINT_DISABLE_CHECKS
#define DEBUG_MODE
#endif

#if SYSTEM_BUFFER
using System.Buffers;
#endif

using Point.Collections;
using System;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using static Unity.Mathematics.math;

namespace Point.Audio.LowLevel
{
    [RequireComponent(typeof(AudioSource))]
    internal unsafe sealed class UnsafeAudioSource : PointMonobehaviour
    {
#if SYSTEM_BUFFER
        private static ArrayPool<AudioSample> s_AudioSampleArrayPool;
        static UnsafeAudioSource()
        {
            s_AudioSampleArrayPool = ArrayPool<AudioSample>.Create();
        }
#endif

        public PlayableAudioClip playableAudioClip
        {
            set
            {
                isPlaying = false;
                m_AudioClip = value;
                m_TargetAudioClip = value.GetAudioClip();
            }
        }
        public AudioClip clip
        {
            get => m_TargetAudioClip?.Value;
            set
            {
                isPlaying = false;
                m_TargetAudioClip = value;
            }
        }
        public bool isPlaying { get; set; }

        [SerializeField] private PlayableAudioClip m_AudioClip;

        private AudioSource m_AudioSource;
        private Promise<AudioClip> m_TargetAudioClip;
        private Promise<AudioSample[]> m_VolumeSamples;

        private int m_TargetChannelSamples, m_CurrentChannelSamplePosition = 0;

        private AudioSource AudioSource
        {
            get
            {
                if (m_AudioSource == null)
                {
                    m_AudioSource = GetComponent<AudioSource>();
                }
                return m_AudioSource;
            }
        }
        private double SampleRate => UnityEngine.AudioSettings.outputSampleRate;

        public void Play()
        {            
            m_VolumeSamples = m_AudioClip.GetVolumes();
            
            m_CurrentChannelSamplePosition = 0;

            StartCoroutine(PlayCoroutine());
        }
        private IEnumerator PlayCoroutine()
        {
            yield return m_TargetAudioClip;
            yield return m_VolumeSamples;

            m_TargetChannelSamples = m_VolumeSamples.Value.Length / m_AudioClip.TargetChannels;

            AudioSource.clip = m_TargetAudioClip.Value;
            AudioSource.Play();
            isPlaying = true;
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (!isPlaying) return;

            int 
                dataPerChannel = data.Length / channels,
                nextSamplePosition = clamp(m_CurrentChannelSamplePosition + dataPerChannel, 0, m_TargetChannelSamples),
                movedSampleOffset = nextSamplePosition - m_CurrentChannelSamplePosition;

            // Processors
            {
                // Process Volume
                DSP.Volume(data, channels, m_VolumeSamples.Value, m_AudioClip.TargetChannels, m_CurrentChannelSamplePosition, movedSampleOffset);
            }

            m_CurrentChannelSamplePosition = nextSamplePosition;

            if (m_CurrentChannelSamplePosition >= m_TargetChannelSamples)
            {
                //$"end {m_TargetChannelSamples} >= {m_CurrentChannelSamplePosition}".ToLog();
                isPlaying = false;
            }
        }
    }
}