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
using Point.Collections.ResourceControl;
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Point.Audio
{
    [BurstCompatible, Serializable]
    public struct Audio : IValidation, ICloneable
    {
        public static Audio Invalid => new Audio(AssetInfo.Invalid, -1, -1, default(UnsafeAllocator<Transformation>));

        //[SerializeField] private AudioKey m_AudioKey;
        internal AssetInfo m_AudioClip;
        internal int m_Index, m_InstanceID;
        private UnsafeAllocator<Transformation> m_Allocator;

#pragma warning disable IDE1006 // Naming Styles
        public AudioKey audioKey => m_AudioClip.Key;
        [NotBurstCompatible]
        public AudioClip clip
        {
            get
            {
                if (!hasAudioSource)
                {
                    this = AudioManager.GetAudio(audioKey);
                    m_AudioClip.AddDebugger();
                }

                AudioSource audioSource = AudioManager.GetAudioSource(in this);
                return audioSource.clip;
            }
            set
            {
                if (!hasAudioSource)
                {
                    this = AudioManager.GetAudio(audioKey);
                    m_AudioClip.AddDebugger();
                }

                AudioSource audioSource = AudioManager.GetAudioSource(in this);
                audioSource.clip = value;
            }
        }
        [NotBurstCompatible]
        public bool isPlaying => AudioManager.IsPlaying(in this);
        public bool is3D
        {
            get
            {
                if (!hasAudioSource)
                {
                    this = AudioManager.GetAudio(audioKey);
                    m_AudioClip.AddDebugger();
                }

                AudioSource audioSource = AudioManager.GetAudioSource(in this);
                return audioSource.spatialize;
            }
        }
        public bool hasAudioSource => m_Allocator.IsCreated;

        public Vector3 position
        {
            get
            {
                if (!hasAudioSource)
                {
                    this = AudioManager.GetAudio(audioKey);
                    m_AudioClip.AddDebugger();
                }

                return m_Allocator[m_Index].localPosition;
            }
            set
            {
                if (!m_Allocator.IsCreated)
                {
                    this = AudioManager.GetAudio(audioKey);
                    m_AudioClip.AddDebugger();
                }

                m_Allocator[m_Index].localPosition = value;
            }
        }
        public quaternion rotation
        {
            get
            {
                if (!m_Allocator.IsCreated)
                {
                    this = AudioManager.GetAudio(audioKey);
                    m_AudioClip.AddDebugger();
                }

                return m_Allocator[m_Index].localRotation;
            }
            set
            {
                if (!m_Allocator.IsCreated)
                {
                    this = AudioManager.GetAudio(audioKey);
                    m_AudioClip.AddDebugger();
                }

                m_Allocator[m_Index].localRotation = value;
            }
        }
#pragma warning restore IDE1006 // Naming Styles

        public Audio(AudioKey audioKey)
        {
            this = default(Audio);

            m_AudioClip = new AssetInfo(audioKey);
        }
        internal Audio(AssetInfo audioKey, int index, int audioSource, UnsafeAllocator<Transformation> allocator)
        {
            m_AudioClip = audioKey;
            m_Index = index;
            m_InstanceID = audioSource;
            m_Allocator = allocator;
        }

        internal bool RequireSetup() => m_InstanceID == 0;
        public bool IsValid() => m_AudioClip.IsValid() && m_InstanceID != 0;

        [NotBurstCompatible]
        public void Play()
        {
            RESULT result = AudioManager.PlayAudio(ref this);
            m_AudioClip.AddDebugger();
            if (result.IsConsiderAsError())
            {
                result.SendLog(m_AudioClip.Key);
            }
            else if (result.IsRequireLog())
            {
                result.SendLog(m_AudioClip.Key);
            }
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

            if (!m_AudioClip.IsValid())
            {
                // TODO
                return;
            }

            m_AudioClip.Reserve();
            m_AudioClip = AssetInfo.Invalid;
        }

        [NotBurstCompatible]
        public override string ToString()
        {
            return m_AudioClip.Key.ToString();
        }

        [NotBurstCompatible]
        object ICloneable.Clone()
        {
            Audio audio = AudioManager.GetAudio(audioKey);

            return audio;
        }
    }
}