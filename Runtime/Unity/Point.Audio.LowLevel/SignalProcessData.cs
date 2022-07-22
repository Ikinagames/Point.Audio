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

using System;
using UnityEngine;

namespace Point.Audio.LowLevel
{
    public readonly struct SignalProcessData
    {
        public readonly double sampleRate;
        public readonly int dspBufferSize;
        public readonly int channels;

        public static SignalProcessData Current
        {
            get
            {
                int
                    channels = ToChannelCount(UnityEngine.AudioSettings.speakerMode),
                    dspBufferSize = UnityEngine.AudioSettings.GetConfiguration().dspBufferSize;

                return new SignalProcessData(
                    UnityEngine.AudioSettings.outputSampleRate, dspBufferSize, channels
                    );
            }
        }

        private SignalProcessData(double sampleRate, int dspBufferSize, int channels)
        {
            this.sampleRate = sampleRate;
            this.dspBufferSize = dspBufferSize;
            this.channels = channels;
        }
        private static int ToChannelCount(AudioSpeakerMode mode)
        {
            switch (mode)
            {
                case AudioSpeakerMode.Mono:
                    return 1;
                case AudioSpeakerMode.Stereo:
                    return 2;
                case AudioSpeakerMode.Quad:
                    return 4;
                case AudioSpeakerMode.Surround:
                    return 5;
                case AudioSpeakerMode.Mode5point1:
                    return 6;
                case AudioSpeakerMode.Mode7point1:
                    return 8;
                case AudioSpeakerMode.Prologic:
                    return 2;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}