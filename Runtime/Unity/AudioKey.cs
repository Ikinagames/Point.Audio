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
using UnityEngine;

namespace Point.Audio
{
    public struct AudioKey
    {
        private readonly Hash m_Key;

        private AudioKey(Hash hash)
        {
            m_Key = hash;
        }

        public static implicit operator AudioKey(AssetPathField<AudioClip> t)
        {
            return new AudioKey(new Hash(t.AssetPath));
        }
        public static implicit operator AudioKey(string t)
        {
            return new AudioKey(new Hash(t));
        }
        public static implicit operator AudioKey(Hash t)
        {
            return new AudioKey(t);
        }
        public static implicit operator Hash(AudioKey t) => t.m_Key;

        public override string ToString() => m_Key.ToString();
    }
}