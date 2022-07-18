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
        [SerializeField] private AudioClip m_BakedClip;

        [SerializeField] private int m_TargetChannels;
        [SerializeField] private AudioSample[] m_Volumes = Array.Empty<AudioSample>();

        private AudioSample[] m_EvaluatedVolumes;

        public int TotalSamples
        {
            get
            {
                int sample;
                if (m_BakedClip != null) sample = m_BakedClip.samples;
                else sample = m_Clip.Asset.Asset.samples;

                return sample * m_TargetChannels;
            }
        }
        public int Channels => m_TargetChannels;

        public bool IsValid() => !m_Clip.IsEmpty() || m_BakedClip != null;
        public Promise<AudioClip> GetAudioClip()
        {
            if (m_BakedClip != null) return new Promise<AudioClip>(m_BakedClip);

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
                    int 
                        packSize = clip.channels,
                        totalSamples = clip.samples * packSize;
                    List<AudioSample> resultSamples = new List<AudioSample>();

                    float[] currentVolume = new float[packSize];
                    int currentSamplePosition = 0;
                    for (int i = 0; i < m_Volumes.Length; i += packSize)
                    {
                        for (int c = 0; c < packSize; c++)
                        {
                            AudioSample sample = m_Volumes[i + c];

                            for (int j = currentSamplePosition; j < sample.position; j++)
                            {
                                float ratio = 
                                (j - currentSamplePosition) / (float)(sample.position - currentSamplePosition);
                                currentVolume[c] =
                                    math.lerp(currentVolume[c], sample.value, ratio);

                                AudioSample newSample = new AudioSample(
                                    j, currentVolume[c]
                                    );
                                resultSamples.Add(newSample);
                            }
                        }
                        
                        currentSamplePosition += m_Volumes[i].position - currentSamplePosition;
                    }
                    $"{currentSamplePosition} :: {totalSamples}".ToLog();
                    for (int i = currentSamplePosition; i < totalSamples; i += packSize)
                    {
                        for (int c = 0; c < packSize; c++)
                        {
                            float ratio = (i - currentSamplePosition) / (float)(totalSamples - currentSamplePosition);
                            currentVolume[c] = math.lerp(currentVolume[c], 0, ratio);

                            AudioSample newSample = new AudioSample(
                                        i, currentVolume[c]
                                        );
                            resultSamples.Add(newSample);
                        }
                    }

                    Assert.AreEqual(totalSamples, resultSamples.Count);

                    m_EvaluatedVolumes = resultSamples.ToArray();
                    result.SetValue(m_EvaluatedVolumes);
                };

                return result;
            }
            return m_EvaluatedVolumes;
        }
    }

    [Serializable]
    public struct AudioSample
    {
        public int position;
        public float value;

        public AudioSample(int x, float y)
        {
            position = x;
            value = y;
        }
        public AudioSample(float x, float y)
        {
            position = Mathf.RoundToInt(x);
            value = y;
        }
    }
}