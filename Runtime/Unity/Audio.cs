// Copyright 2021 Ikina Games
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
using Unity.Mathematics;

namespace Point.Audio
{
    public struct Audio : IValidation, IDisposable
    {
        internal readonly UnsafeReference<KeyValue<RuntimeAudioKey, UnsafeAudio>> m_AudioPointer;
        internal readonly RuntimeAudioKey m_Key;

        internal Audio(UnsafeReference<KeyValue<RuntimeAudioKey, UnsafeAudio>> p)
        {
            m_AudioPointer = p;
            m_Key = p.Value.Key;
        }

        public void Play() { }
        public void Stop() { }
        public void Destroy()
        {
            if (!IsValid())
            {
                return;
            }

            m_AudioPointer.Value.Value.beingUsed = false;
        }

        public bool IsValid()
        {
            if (m_AudioPointer.Value.Key.Equals(m_Key) && m_AudioPointer.Value.Value.beingUsed)
            {
                return true;
            }
            return false;
        }
        void IDisposable.Dispose()
        {
            Destroy();
        }
    }
    internal struct UnsafeAudio
    {
        public bool beingUsed;
        //public readonly RuntimeAudioKey key;

        public readonly AssetInfo audio;
        public readonly RuntimeAudioSetting audioSetting;

        public float3 translation;
        public quaternion rotation;

        public UnsafeAudio(/*RuntimeAudioKey key,*/ RuntimeAudioSetting setting, AssetInfo asset)
        {
            this = default(UnsafeAudio);

            this.beingUsed = true;
            //this.key = key;
            this.audioSetting = setting;
            this.audio = asset;
        }
    }
}
