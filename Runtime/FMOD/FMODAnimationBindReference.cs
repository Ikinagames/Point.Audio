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
using System.Collections.Generic;
using UnityEngine;

namespace Point.Audio
{
    [CreateAssetMenu(menuName = "Point/Audio/Create Animation Bind Reference", fileName = "NewAnimBindRef")]
    public sealed class FMODAnimationBindReference : ScriptableObject
    {
        [SerializeField]
        private ArrayWrapper<FMODAnimationEvent> m_Events = Array.Empty<FMODAnimationEvent>();

        internal void AddToHashMap(ref Dictionary<Hash, FMODAnimationEvent> hashMap)
        {
            for (int i = 0; i < m_Events.Length; i++)
            {
                m_Events[i].Initialize();

                hashMap.Add(new Hash(m_Events[i].Name), (FMODAnimationEvent)m_Events[i].Clone());
            }
        }
    }
}
