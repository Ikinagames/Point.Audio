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

using Point.Audio.LowLevel;
using Point.Collections;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using static Unity.Mathematics.math;

namespace Point.Audio
{
    [Serializable]
    public class PlayableAudioClip : IValidation, IRootSignalProcessor
    {
        [SerializeField] private AssetPathField<AudioClip> m_Clip;

        [SerializeField] private int m_TargetChannels;
        [SerializeField] private AudioSample[] m_Volumes = Array.Empty<AudioSample>();

        private Promise<AudioClip> m_AudioClip;
        private Promise<AudioSample[]> m_EvaluatedVolumes;

        public AudioKey Key => m_Clip;
        public int TargetChannels => m_TargetChannels;

        internal Promise<AudioClip> GetAudioClip()
        {
            if (m_AudioClip == null)
            {
                m_AudioClip = m_Clip.Asset.LoadAsset();
            }
            return m_AudioClip;
        }
        internal Promise<AudioSample[]> GetVolumes()
        {
            if (m_EvaluatedVolumes == null)
            {
                Promise<AudioClip> clip = GetAudioClip();
                m_EvaluatedVolumes = new Promise<AudioSample[]>();
                clip.OnCompleted += delegate (AudioClip clip)
                {
                    AudioSample[] volumes = DSP.Evaluate(clip, m_Volumes);
                    m_EvaluatedVolumes.SetValue(volumes);
                };
            }
            return m_EvaluatedVolumes;
        }

        public bool IsValid() => !m_Clip.IsEmpty();

        void ISignalProcessor.OnInitialize(SignalProcessData data)
        {
            GetAudioClip();
            GetVolumes();
        }
        bool ISignalProcessor.CanProcess() => m_AudioClip.HasValue && m_EvaluatedVolumes.HasValue;
        AudioClip IRootSignalProcessor.GetRootClip() => m_AudioClip.Value;
        int IRootSignalProcessor.GetTargetSamples() => m_AudioClip.Value.samples;
        void ISignalProcessor.BeforeProcess(RuntimeSignalProcessData processData)
        {
        }
        void ISignalProcessor.Process(RuntimeSignalProcessData processData, float[] data, int channels)
        {
            // Processors
            {
                // Process Volume
                DSP.Volume(data, channels, m_EvaluatedVolumes.Value, TargetChannels, processData.currentSamplePosition, processData.nextSamplePositionOffset);
            }
        }
    }
}