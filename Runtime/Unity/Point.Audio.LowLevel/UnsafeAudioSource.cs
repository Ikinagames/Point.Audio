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
        public PlayableAudioClip playableAudioClip
        {
            set
            {
                isPlaying = false;
                m_PlayableAudioClip = value;
            }
        }
        public AudioSource audioSource => GetComponent<AudioSource>();
        public bool isPlaying { get; set; }

        private RuntimeSignalProcessData m_RuntimeSignalProcessData;
        private IRootSignalProcessor m_PlayableAudioClip;

        // https://forum.unity.com/threads/dsp-buffer-size-differences-why-isnt-it-a-setting-per-platform.447925/
        public void Play()
        {
            StartCoroutine(PlayCoroutine());
        }
        private IEnumerator PlayCoroutine()
        {
            SignalProcessData currentData = SignalProcessData.Current;

            // On Initialize
            {
                m_PlayableAudioClip.OnInitialize(currentData);
            }

            // Wait for can process
            {
                while (!m_PlayableAudioClip.CanProcess())
                {
                    yield return null;
                }
            }

            audioSource.clip = m_PlayableAudioClip.GetRootClip();
            m_RuntimeSignalProcessData 
                = new RuntimeSignalProcessData(currentData, m_PlayableAudioClip.GetTargetSamples());

            // Before execute process
            {
                m_PlayableAudioClip.BeforeProcess(m_RuntimeSignalProcessData);
            }

            audioSource.Play();
            isPlaying = true;
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (!isPlaying) return;

            m_PlayableAudioClip.Process(m_RuntimeSignalProcessData, data, channels);

            ref RuntimeSignalProcessData ptr = ref m_RuntimeSignalProcessData;
            ptr.currentSamplePosition = ptr.nextSamplePosition;

            if (ptr.currentSamplePosition > ptr.targetSamples)
            {
                isPlaying = false;
            }
        }
    }
}