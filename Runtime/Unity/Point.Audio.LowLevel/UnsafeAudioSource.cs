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

namespace Point.Audio.LowLevel
{
    internal unsafe sealed class UnsafeAudioSource : PointMonobehaviour
    {
        public PlayableAudioClip audioClip { get; set; }
        public bool isPlaying { get; set; }

        private double SampleRate => UnityEngine.AudioSettings.outputSampleRate;

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (!isPlaying) return;

            for (int i = 0; i < data.Length; i++)
            {

            }
        }
    }
}