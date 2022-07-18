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
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using static Unity.Mathematics.math;

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
        private int OutputChannels
        {
            get
            {
                var mode = UnityEngine.AudioSettings.speakerMode;
                if (mode == AudioSpeakerMode.Mono) return 1;
                else if (mode == AudioSpeakerMode.Stereo) return 2;

                throw new NotImplementedException();
            }
        }
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
            //var volumeSampleArray = m_AudioClip.GetVolumes();
            //if (OutputChannels == m_AudioClip.Channels)
            //{
            //    m_VolumeSamples = volumeSampleArray;
            //}
            //else if (m_AudioClip.Channels == 1 && OutputChannels > m_AudioClip.Channels)
            //{
            //    var array = m_VolumeSamples.Value;
            //    int targetChannels = OutputChannels;

            //    for (int i = 0; i < targetChannels; i++)
            //    {

            //    }
            //}
            //else throw new NotImplementedException();
            
            m_VolumeSamples = m_AudioClip.GetVolumes();
            m_TargetSamples = m_AudioClip.TotalSamples;
            m_CurrentSamplePosition = 0;
            m_VolumeIndex = 0;

            isPlaying = true;
            AudioSource.Play();
        }
        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (!isPlaying) return;

            int 
                dataPerChannel = data.Length / channels,
                nextSamplePosition = clamp(m_CurrentSamplePosition + (dataPerChannel * m_AudioClip.Channels), 0, m_TargetSamples),
                movedSampleOffset = nextSamplePosition - m_CurrentSamplePosition;
            //$"{data.Length} :: {m_VolumeSamples.Value.Length} :: {m_CurrentSamplePosition} :: next ({nextSamplePosition})".ToLog();
            //$"{channels}".ToLog();

            AudioSample[] volumeArray = s_AudioSampleArrayPool.Rent(dataPerChannel * m_AudioClip.Channels);
            {
                $"{volumeArray.Length} :: {data.Length}".ToLog();
                Array.Copy(m_VolumeSamples.Value, m_CurrentSamplePosition, volumeArray, 0, movedSampleOffset);
                Volume(data, channels, volumeArray, m_AudioClip.Channels);
            }
            s_AudioSampleArrayPool.Return(volumeArray);

            m_CurrentSamplePosition = nextSamplePosition;

            if (m_CurrentSamplePosition >= m_TargetSamples)
            {
                $"end {m_TargetSamples} >= {m_CurrentSamplePosition}".ToLog();
                isPlaying = false;
            }
        }
        private static void Volume(float[] data, int channels, AudioSample[] samples, int sampleChannels)
        {
            for (int i = 0, x = 0; i < data.Length; i += channels, x += sampleChannels)
            {
                for (int j = 0, y = 0; j < channels; j++, y = clamp(y + 1, 0, sampleChannels - 1))
                {
                    //$"{i} : {j} : {a} :: {i + j} , {i + a}".ToLog();
                    data[i + j] = lerp(0, data[i + j], samples[x + y].value);
                }
            }
        }
    }
}