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

using static Unity.Mathematics.math;

namespace Point.Audio.LowLevel
{
    public struct RuntimeSignalProcessData
    {
        public readonly SignalProcessData signalProcessData;
        public readonly int targetSamples;
        public int currentSamplePosition;

        public int nextSamplePosition
            => clamp(currentSamplePosition + signalProcessData.dspBufferSize, 0, targetSamples);
        public int nextSamplePositionOffset
            => nextSamplePosition - currentSamplePosition;

        internal RuntimeSignalProcessData(SignalProcessData data, int targetSamples)
        {
            this = default(RuntimeSignalProcessData);

            signalProcessData = data;
            this.targetSamples = targetSamples;
        }
    }
}