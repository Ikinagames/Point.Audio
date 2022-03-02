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
using System;
using Unity.Mathematics;
using UnityEngine;

namespace Point.Audio
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Point/FMOD/Audio Room")]
    public sealed class FMODAudioRoom : MonoBehaviour
    {
        [SerializeField] private string m_RoomName;
        [SerializeField] private AABB m_AABB;

        [NonSerialized] private Hash m_NameHash;
    }

    public struct AudioRoom : IEquatable<AudioRoom>
    {
        private Hash m_Hash;
        private AABB m_AABB;

        public AudioRoom(Hash hash, AABB aabb)
        {
            m_Hash = hash;
            m_AABB = aabb;
        }

        #region Bounds

        public void Encapsulate(float3 point) => m_AABB.Encapsulate(point);
        public void Encapsulate(AABB aabb) => m_AABB.Encapsulate(aabb);

        #endregion

        public bool Equals(AudioRoom other) => m_Hash.Equals(other.m_Hash);
    }
}
