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
    public class FMODAnimationBinder : MonoBehaviour
    {
        [SerializeField] private FMODAnimationBindReference m_BindReference;
        [SerializeField] private FMODAnimationEvent[] m_Events = Array.Empty<FMODAnimationEvent>();
        [SerializeField] private UnityEngine.Object m_ReferenceObject;

        private Dictionary<Hash, AudioReference> m_Parsed;

        protected IReadOnlyList<FMODAnimationEvent> Events => m_Events;

        private void Awake()
        {
            m_Parsed = new Dictionary<Hash, AudioReference>();

            m_BindReference.AddToHashMap(ref m_Parsed);
            for (int i = 0; i < m_Events.Length; i++)
            {
                m_Parsed.Add(new Hash(m_Events[i].Name), m_Events[i].AudioReference);
            }
        }

        public void TriggerAction(AnimationEvent ev)
        {
            Hash hash = new Hash(ev.stringParameter);

            if (!m_Parsed.TryGetValue(hash, out AudioReference animEv)) return;

            Audio audio = animEv.GetAudio(m_ReferenceObject, OnProcessParameter);

            audio.position = transform.position;
            audio.rotation = transform.rotation;

            audio.Play();
        }

        /// <summary>
        /// Audio 에 전달할 Parameter 값을 받아 수정할 수 있습니다.
        /// </summary>
        /// <param name="ev"></param>
        /// <param name="existingValue"></param>
        /// <returns>수정된 값을 반환하여 적용시킵니다.</returns>
        protected virtual float OnProcessParameter(
            FMOD.Studio.EventDescription ev, float existingValue) => existingValue;
    }
}
