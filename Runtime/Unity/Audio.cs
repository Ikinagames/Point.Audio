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
using Point.Collections.Buffer.LowLevel;
using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Point.Audio
{
    [BurstCompatible]
    public struct Audio : IValidation
    {
        public static Audio Invalid => new Audio();

        internal readonly AudioKey m_AudioKey;
        internal int m_Index, m_InstanceID;
        private UnsafeAllocator<Transformation> m_Allocator;

#pragma warning disable IDE1006 // Naming Styles
        [NotBurstCompatible]
        public AudioClip clip
        {
            get
            {
                AudioSource audioSource = AudioManager.GetAudioSource(in this);
                return audioSource.clip;
            }
            set
            {
                AudioSource audioSource = AudioManager.GetAudioSource(in this);
                audioSource.clip = value;
            }
        }
        [NotBurstCompatible]
        public bool isPlaying => AudioManager.IsPlaying(in this);

        public float3 position
        {
            get => m_Allocator[m_Index].localPosition;
            set
            {
                m_Allocator[m_Index].localPosition = value;
            }
        }
        public quaternion rotation
        {
            get => m_Allocator[m_Index].localRotation;
            set
            {
                m_Allocator[m_Index].localRotation = value;
            }
        }
#pragma warning restore IDE1006 // Naming Styles

        public Audio(AudioKey audioKey, UnsafeAllocator<Transformation> allocator)
        {
            this = default(Audio);

            m_AudioKey = audioKey;
            m_Allocator = allocator;
        }
        internal Audio(AudioKey audioKey, int index, int audioSource, UnsafeAllocator<Transformation> allocator)
        {
            m_AudioKey = audioKey;
            m_Index = index;
            m_InstanceID = audioSource;
            m_Allocator = allocator;
        }

        internal bool RequireSetup() => m_InstanceID == 0;
        public bool IsValid() => m_AudioKey.IsValid() && m_Allocator.IsCreated;

        [NotBurstCompatible]
        public void Play()
        {
            AudioManager.PlayAudio(ref this);
        }
        [NotBurstCompatible]
        public void Stop()
        {
            AudioManager.StopAudio(in this);
        }

        /// <summary>
        /// 오디오 인스턴스를 <see cref="AudioManager"/> 에게 반환합니다.
        /// </summary>
        [NotBurstCompatible]
        public void Reserve()
        {
            AudioManager.ReserveAudio(ref this);

            m_Allocator = default(UnsafeAllocator<Transformation>);
        }

        [NotBurstCompatible]
        public override string ToString()
        {
            return m_AudioKey.ToString();
        }
    }

    [Flags]
    public enum RESULT
    {
        OK      =   0,

        //
        AUDIOCLIP       =   0b0001,
        ASSETBUNDLE     =   0b0010,
        AUDIOKEY        =   0b0100,

        //
        NOTFOUND        =   0b0001 << 7,
        NOTLOADED       =   0b0010 << 7,
        NOTVALID        =   0b0100 << 7,

        IGNORED         =   ~0
    }
}