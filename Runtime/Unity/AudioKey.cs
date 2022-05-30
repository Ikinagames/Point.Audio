﻿// Copyright 2022 Ikina Games
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
using System;
using Unity.Collections;
using UnityEngine;

namespace Point.Audio
{
    [BurstCompatible, Serializable]
    public struct AudioKey : IValidation, IEquatable<AudioKey>
    {
        [SerializeField]
        private AssetRuntimeKey m_Key;

        public AudioKey(AssetRuntimeKey hash)
        {
            m_Key = hash;
        }
        public AudioKey(Hash hash)
        {
            m_Key = new AssetRuntimeKey(hash);
        }

        [NotBurstCompatible]
        public static implicit operator AudioKey(AssetPathField<AudioClip> t)
        {
            return new AudioKey(new Hash(t.AssetPath.ToLowerInvariant()));
        }
        [NotBurstCompatible]
        public static implicit operator AudioKey(string t)
        {
            return new AudioKey(new Hash(t));
        }
        public static implicit operator AudioKey(Hash t)
        {
            return new AudioKey(t);
        }
        public static implicit operator AudioKey(AssetRuntimeKey t)
        {
            return new AudioKey(t);
        }
        public static implicit operator AssetRuntimeKey(AudioKey t) => t.m_Key;
        public static implicit operator Hash(AudioKey t) => t.m_Key.Key;

        [NotBurstCompatible]
        public override string ToString() => m_Key.ToString();

        public bool IsValid() => !m_Key.IsEmpty();
        public bool Equals(AudioKey other) => m_Key.Equals(other.m_Key);
    }
}