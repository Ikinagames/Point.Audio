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
using Point.Collections.ResourceControl;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace Point.Audio
{
    [BurstCompatible]
    [StructLayout(LayoutKind.Sequential)]
    public struct CompressedAudioData
    {
        private AudioKey m_AudioClipPath;
        private AssetRuntimeKey m_PrefabPath;

        public float
            ignoreTime, masterVolume,
            minVolume, maxVolume,
            minPitch, maxPitch;

        public AudioKey AudioKey => m_AudioClipPath;
        public AssetRuntimeKey PrefabKey => m_PrefabPath;

        //public float ignoreTime => m_IgnoreTime;

        internal CompressedAudioData(AudioKey key)
        {
            this = default(CompressedAudioData);

            m_AudioClipPath = key;
        }
        [NotBurstCompatible]
        internal CompressedAudioData(
            AssetPathField<AudioClip> audioClip,
            AssetPathField<AudioSource> prefab,
            
            float ignoreTime,
            
            float masterVolume,
            float minVolume, float maxVolume,
            float minPitch, float maxPitch)
        {
            this.ignoreTime = ignoreTime;
            this.masterVolume = masterVolume;
            this.minVolume = minVolume;
            this.maxVolume = maxVolume;
            this.minPitch = minPitch;
            this.maxPitch = maxPitch;
            
            m_AudioClipPath = new AssetRuntimeKey(audioClip);
            m_PrefabPath = new AssetRuntimeKey(prefab);
        }

        public float GetVolume() => UnityEngine.Random.Range(minVolume, maxVolume);
        public float GetPitch() => UnityEngine.Random.Range(minPitch, maxPitch);
    }
}