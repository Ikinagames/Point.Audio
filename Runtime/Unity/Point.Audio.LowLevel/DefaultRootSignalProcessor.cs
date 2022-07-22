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
using UnityEngine;

namespace Point.Audio.LowLevel
{
    internal struct DefaultRootSignalProcessor : IRootSignalProcessor
    {
        private SignalProcessData m_CurrentData;

        public float Gain { get; set; }

        public DefaultRootSignalProcessor(float gain)
        {
            this = default(DefaultRootSignalProcessor);

            Gain = gain;
        }

        public void OnInitialize(SignalProcessData data)
        {
            m_CurrentData = data;
        }
        public bool CanProcess() => false;
        public AudioClip GetRootClip() => null;
        public int GetTargetSamples() => m_CurrentData.dspBufferSize;

        public void BeforeProcess(RuntimeSignalProcessData processData)
        {
        }
        public void Process(RuntimeSignalProcessData processData, float[] data, int channels)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] *= Gain;
            }
        }
    }
}