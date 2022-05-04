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
        [MarshalAs(UnmanagedType.U1)]
        private readonly bool m_EnableStealing;
        private readonly float
            m_IgnoreTime, m_MasterVolume,
            m_MinVolume, m_MaxVolume,
            m_MinPitch, m_MaxPitch;
        private readonly Hash 
            m_AudioClipPath, m_PrefabPath;

        public Hash AudioKey => m_AudioClipPath;
        public Hash PrefabKey => m_PrefabPath;

        [NotBurstCompatible]
        internal CompressedAudioData(
            AssetPathField<AudioClip> audioClip,
            AssetPathField<AudioSource> prefab,
            AudioMixerGroup group,
            
            bool enableStealing,
            float ignoreTime,
            
            float masterVolume,
            float minVolume, float maxVolume,
            float minPitch, float maxPitch)
        {
            m_EnableStealing = enableStealing;
            m_IgnoreTime = ignoreTime;
            m_MasterVolume = masterVolume;
            m_MinVolume = minVolume;
            m_MaxVolume = maxVolume;
            m_MinPitch = minPitch;
            m_MaxPitch = maxPitch;
            
            m_AudioClipPath = new Hash(audioClip.AssetPath);
            m_PrefabPath = new Hash(prefab.AssetPath);
        }

        public float GetVolume() => UnityEngine.Random.Range(m_MinVolume, m_MaxVolume);
        public float GetPitch() => UnityEngine.Random.Range(m_MinPitch, m_MaxPitch);
    }
}