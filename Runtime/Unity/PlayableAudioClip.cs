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

namespace Point.Audio
{
    [Serializable]
    public class PlayableAudioClip : IValidation
    {
        [SerializeField] private AssetPathField<AudioClip> m_Clip;

        [SerializeField] private int m_TargetChannels;
        [SerializeField] private AudioSample[] m_Volumes = Array.Empty<AudioSample>();

        private AudioSample[] m_EvaluatedVolumes;

        public int TotalSamples
        {
            get
            {
                int sample = m_Clip.Asset.Asset.samples;

                return sample * m_TargetChannels;
            }
        }
        public int Channels => m_TargetChannels;

        public bool IsValid() => !m_Clip.IsEmpty();
        public Promise<AudioClip> GetAudioClip()
        {
            return m_Clip.Asset.LoadAsset();
        }
        public Promise<AudioSample[]> GetVolumes()
        {
            if (m_EvaluatedVolumes == null)
            {
                Promise<AudioClip> clip = GetAudioClip();
                Promise<AudioSample[]> result = new Promise<AudioSample[]>();
                clip.OnCompleted += delegate (AudioClip clip)
                {
                    m_EvaluatedVolumes = DSP.Evaluate(clip, m_Volumes);
                    result.SetValue(m_EvaluatedVolumes);
                };

                return result;
            }
            return m_EvaluatedVolumes;
        }
    }
}