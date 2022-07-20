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
        public static Audio Invalid => new Audio(AssetInfo.Invalid, default(AudioKey), -1, -1, default(UnsafeAllocator<Transformation>));

        internal AssetInfo m_AudioClip;
        internal AudioKey m_Parent;
        internal int m_Index, m_InstanceID;
        private UnsafeAllocator<Transformation> m_Allocator;

#pragma warning disable IDE1006 // Naming Styles
        public AudioKey audioKey => m_Parent.IsValid() ? m_Parent : new AudioKey(m_AudioClip.Key);
        [NotBurstCompatible]
        public AudioClip clip
        {
            get
            {
                if (!IsValid())
                {
                    $"?? error".ToLogError();
                    return null;
                }

                AudioSource audioSource = AudioManager.GetAudioSource(in this);
                return audioSource.clip;
            }
            set
            {
                if (!IsValid())
                {
                    "fatal err".ToLogError();
                    Debug.Break();

                    throw new InvalidOperationException();
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
                if (!IsValid())
                {
                    $"?? error".ToLogError();
                    return false;
                }
                //if (!hasAudioSource)
                //{
                //    this = AudioManager.GetAudio(audioKey);
                //    m_AudioClip.AddDebugger();
                //}

                AudioSource audioSource = AudioManager.GetAudioSource(in this);
                return audioSource.spatialize;
            }
        }
        //public bool hasAudioSource => m_Allocator.IsCreated && m_InstanceID != 0;

        public ref Transformation transform
        {
            get
            {
                if (!IsValid())
                {
                    "fatal err".ToLogError();
                    Debug.Break();

                    throw new InvalidOperationException();
                }

                return ref m_Allocator[m_Index];
            }
        }
        public Volume volume
        {
            get
            {
                if (!IsValid())
                {
                    "fatal err".ToLogError();
                    Debug.Break();

                    throw new InvalidOperationException();
                }

                //var audioSource = AudioManager.GetAudioSource(this);
                //return audioSource.volume;
                return new Volume(m_InstanceID);
            }
            set
            {
                if (!IsValid())
                {
                    "fatal err".ToLogError();
                    Debug.Break();

                    throw new InvalidOperationException();
                }

                var audioSource = AudioManager.GetAudioSource(this);
                audioSource.volume = value;
            }
        }
        public Vector3 position
        {
            get
            {
                if (!IsValid())
                {
                    "fatal err".ToLogError();
                    Debug.Break();

                    throw new InvalidOperationException();
                }

                return m_Allocator[m_Index].localPosition;
            }
            set
            {
                if (!IsValid())
                {
                    "fatal err".ToLogError();
                    Debug.Break();

                    throw new InvalidOperationException();
                }

                m_Allocator[m_Index].localPosition = value;
            }
        }
        public quaternion rotation
        {
            get
            {
                if (!IsValid())
                {
                    "fatal err".ToLogError();
                    Debug.Break();

                    throw new InvalidOperationException();
                }

                return m_Allocator[m_Index].localRotation;
            }
            set
            {
                if (!IsValid())
                {
                    "fatal err".ToLogError();
                    Debug.Break();

                    throw new InvalidOperationException();
                }

                m_Allocator[m_Index].localRotation = value;
            }
        }
#pragma warning restore IDE1006 // Naming Styles

        internal Audio(AssetInfo audioKey, AudioKey parent,
            int index, int audioSource, UnsafeAllocator<Transformation> allocator)
        {
            m_AudioClip = audioKey;
            m_Parent = parent;
            m_Index = index;
            m_InstanceID = audioSource;
            m_Allocator = allocator;
        }

        public bool IsValid()
        {
            AudioSource audioSource;
            if (m_InstanceID == 0)
            {
                //"1".ToLog();
                return false;
            }
            else
            {
                if (!m_AudioClip.IsValid() || m_Index < 0)
                {
                    //$"2 {m_AudioClip.IsValid()} : {m_Index}".ToLog();
                    return false;
                }

                audioSource = AudioManager.GetAudioSource(in this);
            }

            if (audioSource == null || audioSource.GetInstanceID() != m_InstanceID)
            {
                //"3".ToLog();
                return false;
            }
            return true;
        }

        [NotBurstCompatible]
        public void Play()
        {
            if (!IsValid())
            {
                PointHelper.LogError(Channel.Audio,
                    $"You\'re trying to play an invalid audio. This is not allowed.");
                return;
            }

            RESULT result = AudioManager.PlayAudio(ref this, out bool initialized);
            if (initialized) m_AudioClip.AddDebugger();

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
            if (!IsValid())
            {
                PointHelper.LogError(Channel.Audio,
                    $"You\'re trying to stop an invalid audio. This is not allowed.");
                return;
            }

            "stop audio".ToLog();
            AudioManager.StopAudio(in this);
        }

        /// <summary>
        /// 오디오 인스턴스를 <see cref="AudioManager"/> 에게 반환합니다.
        /// </summary>
        [NotBurstCompatible]
        public void Reserve()
        {
            if (!IsValid())
            {
                "not valid falid to reserve".ToLog();
                return;
            }

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