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

        //private void Start()
        //{
        //    if (m_AudioClip != null)
        //    {
        //        CurrentClip = m_AudioClip.GetAudioClip().Value;
        //        Play();
        //    }
        //}

        public void Play()
        {            
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

            // Processors
            {
                // Process Volume
                Process(
                    in m_CurrentSamplePosition, in dataPerChannel, in movedSampleOffset,
                    data, in channels,
                    m_VolumeSamples.Value, m_AudioClip.Channels, DSP.Volume
                    );
            }

            m_CurrentSamplePosition = nextSamplePosition;

            if (m_CurrentSamplePosition >= m_TargetSamples)
            {
                $"end {m_TargetSamples} >= {m_CurrentSamplePosition}".ToLog();
                isPlaying = false;
            }
        }
        
        public static void Process(
            in int currentSamplePosition, in int dataPerChannel, in int processSampleOffset,
            float[] data, in int channels, 
            in AudioSample[] audioSamples, in int audioSampleChannels,
            Action<float[], int, AudioSample[], int> dsp)
        {
            AudioSample[] array
#if SYSTEM_BUFFER
                = s_AudioSampleArrayPool.Rent(dataPerChannel * audioSampleChannels);
#else
                = new AudioSample[dataPerChannel * audioSampleChannels];
#endif
            Array.Copy(audioSamples, currentSamplePosition, array, 0, processSampleOffset);

            // process
            {
                dsp.Invoke(data, channels, array, audioSampleChannels);
            }
#if SYSTEM_BUFFER
            s_AudioSampleArrayPool.Return(array);
#endif
        }
    }
}