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

using Point.Collections;
using System;
using System.Buffers;
using UnityEngine;

namespace Point.Audio.LowLevel
{
    [RequireComponent(typeof(AudioSource))]
    internal unsafe sealed class UnsafeAudioSource : PointMonobehaviour
    {
        private static ArrayPool<AudioSample> s_AudioSampleArrayPool;
        static UnsafeAudioSource()
        {
            s_AudioSampleArrayPool = ArrayPool<AudioSample>.Create();
        }

        public PlayableAudioClip playableAudioClip
        {
            set
            {
                isPlaying = false;
                m_TargetAudioClip = value.GetAudioClip();
                m_VolumeSamples = value.GetVolumes();
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
        private AudioSample[] m_VolumeSamples = Array.Empty<AudioSample>();

        private int m_VolumeIndex = 0;
        private int m_TargetSamples, m_CurrentSamplePosition = 0;

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
        private AudioClip CurrentClip
        {
            get => AudioSource.clip;
            set => AudioSource.clip = value;
        }
        private double SampleRate => UnityEngine.AudioSettings.outputSampleRate;
        private int PackSize => CurrentClip.channels;

        private void Start()
        {
            if (m_AudioClip != null)
            {
                CurrentClip = m_AudioClip.GetAudioClip().Value;
                Play();
            }
        }

        public void Play()
        {
            m_VolumeSamples = m_AudioClip.GetVolumes();
            m_TargetSamples = CurrentClip.samples;
            m_CurrentSamplePosition = 0;
            m_VolumeIndex = 0;

            isPlaying = true;
        }
        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (!isPlaying) return;

            for (int i = 0; i < data.Length; i += channels)
            {
                for (int j = 0; j < channels; j++)
                {
                }
            }
            m_CurrentSamplePosition += data.Length;

            if (m_CurrentSamplePosition >= m_TargetSamples)
            {
                isPlaying = false;
            }
        }
    }
}