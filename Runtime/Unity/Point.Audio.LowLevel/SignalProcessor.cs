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

namespace Point.Audio.LowLevel
{
    public abstract class SignalProcessor : ISignalProcessor
    {
        void ISignalProcessor.OnInitialize(SignalProcessData data)
        {
            OnInitialize(in data);
        }
        bool ISignalProcessor.CanProcess() => CanProcess();

        void ISignalProcessor.BeforeProcess(RuntimeSignalProcessData processData)
        {
            BeforeProcess(in processData);
        }
        void ISignalProcessor.Process(RuntimeSignalProcessData processData, float[] data, int channels)
        {
            Process(in processData, ref data, in channels);
        }

        protected virtual void OnInitialize(in SignalProcessData data) { }
        protected virtual bool CanProcess() => true;

        protected virtual void BeforeProcess(in RuntimeSignalProcessData processData) { }
        protected virtual void Process(in RuntimeSignalProcessData processData, ref float[] data, in int channels) { }
    }
    //public class DownSampler : SignalProcessor
    //{
    //    private const int c_RampCount = 256;

    //    public float Gain { get; set; }
    //    public int SampleCount { get; set; }
    //    public float Mix { get; set; }

    //    private float m_CurrentGain;
    //    private int m_CurrentRampCount;

    //    protected override void BeforeProcess(in RuntimeSignalProcessData processData)
    //    {
    //        m_CurrentGain = Gain;
    //        m_CurrentRampCount = c_RampCount;
    //    }
    //    protected override void Process(in RuntimeSignalProcessData processData, ref float[] data, in int channels)
    //    {
    //        int normalizedCount = SampleCount * channels;
    //        float delta = (Gain - m_CurrentGain) / m_CurrentRampCount;
    //        for (int i = 0; i < data.Length; i+= normalizedCount)
    //        {

    //        }
    //    }
    //}
}