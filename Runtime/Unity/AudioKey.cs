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
using System;
using Unity.Collections;

namespace Point.Audio
{
    [BurstCompatible]
    public readonly struct AudioKey : IEquatable<AudioKey>
    {
        private readonly Hash m_Hash;

        public Hash Key => m_Hash;

        public AudioKey(Hash hash)
        {
            m_Hash = hash;
        }

        public bool Equals(AudioKey other) => m_Hash.Equals(other.m_Hash);
        [NotBurstCompatible]
        public override string ToString() => m_Hash.ToString();
        public override int GetHashCode() => m_Hash.GetHashCode();

        public static implicit operator Hash(AudioKey key) => key.m_Hash;

        public static explicit operator AudioKey(Hash hash) => new AudioKey(hash);
        [NotBurstCompatible]
        public static explicit operator AudioKey(string key) => new AudioKey(new Hash(key));
    }
}
