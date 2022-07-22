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
        public AudioSource audioSource => GetComponent<AudioSource>();

        private RuntimeSignalProcessData m_RuntimeSignalProcessData;
        private IRootSignalProcessor m_PlayableAudioClip = new DefaultRootSignalProcessor(0);

        // https://forum.unity.com/threads/dsp-buffer-size-differences-why-isnt-it-a-setting-per-platform.447925/
        public void Play(PlayableAudioClip clip)
        {
            StartCoroutine(PlayCoroutine(clip));
        }
        private IEnumerator PlayCoroutine(PlayableAudioClip clip)
        {
            SignalProcessData currentData = SignalProcessData.Current;
            IRootSignalProcessor root = clip;

            // On Initialize
            {
                root.OnInitialize(currentData);
            }

            // Wait for can process
            {
                while (!root.CanProcess())
                {
                    yield return null;
                }
            }

            audioSource.clip = root.GetRootClip();
            m_RuntimeSignalProcessData 
                = new RuntimeSignalProcessData(currentData, root.GetTargetSamples());

            // Before execute process
            {
                root.BeforeProcess(m_RuntimeSignalProcessData);
            }

            audioSource.Play();
            m_PlayableAudioClip = root;
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            m_PlayableAudioClip.Process(m_RuntimeSignalProcessData, data, channels);

            ref RuntimeSignalProcessData ptr = ref m_RuntimeSignalProcessData;
            ptr.currentSamplePosition = ptr.nextSamplePosition;

            if (ptr.currentSamplePosition >= ptr.targetSamples)
            {
                m_PlayableAudioClip = new DefaultRootSignalProcessor(0);
            }
        }
    }
}