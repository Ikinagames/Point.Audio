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
    public sealed class FMODAnimationBinder : MonoBehaviour
    {
        [SerializeField] private FMODAnimationBindReference m_BindReference;
        [SerializeField] private FMODAnimationEvent[] m_Events = Array.Empty<FMODAnimationEvent>();

        private Dictionary<Hash, Audio> m_Parsed;

        private void Awake()
        {
            m_Parsed = new Dictionary<Hash, Audio>();

            m_BindReference.AddToHashMap(ref m_Parsed);
            for (int i = 0; i < m_Events.Length; i++)
            {
                m_Parsed.Add(new Hash(m_Events[i].Name), m_Events[i].Audio);
            }
        }

        public void TriggerAction(AnimationEvent ev)
        {
            Hash hash = new Hash(ev.stringParameter);

            if (!m_Parsed.TryGetValue(hash, out Audio audio)) return;

            audio.position = transform.position;
            audio.rotation = transform.rotation;

            audio.Play();
        }
    }
}
