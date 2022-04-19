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
using Point.Collections.Graphs;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Point.Audio
{
    /// <summary>
    /// <see cref="Animator"/> 에서 재생되고 있는 <see cref="AnimationClip"/> 의 
    /// <see cref="AnimationEvent"/> 정보로 FMOD event 를 재생하는 컴포넌트입니다.
    /// </summary>
    /// <remarks>
    /// <see cref="AnimationEvent"/> 의 FunctionName 은 TriggerAction 이어야 동작합니다.
    /// </remarks>
    [RequireComponent(typeof(Animator))]
    [AddComponentMenu("Point/FMOD/Animation Binder")]
    public class FMODAnimationBinder : AnimationEventBinder
    {
        [SerializeField] private FMODAnimationBindReference m_BindReference;
        [SerializeField] private FMODAnimationEvent[] m_Events = Array.Empty<FMODAnimationEvent>();

        [Space]
        [SerializeField] private GameObject m_OverrideRoot = null;

        // Parameter 가 많을 경우, 배열을 전체 탐색하기 보다는 Hashing 을 통해 속도 개선을 합니다.
        private Dictionary<Hash, FMODAnimationEvent> m_Parsed;
        private VisualLogicGraph m_LogicGraph;

        /// <summary>
        /// 등록된 FMOD 애니메이션 이벤트 배열입니다.
        /// </summary>
        protected IReadOnlyList<FMODAnimationEvent> Events => m_Events;

        private void Awake()
        {
            m_Parsed = new Dictionary<Hash, FMODAnimationEvent>();

            if (m_BindReference != null)
            {
                m_BindReference.AddToHashMap(ref m_Parsed);
            }
            
            for (int i = 0; i < m_Events.Length; i++)
            {
                m_Parsed.Add(new Hash(m_Events[i].Name), m_Events[i]);
            }
        }

        /// <summary>
        /// 이 메소드는 <see cref="AnimationClip"/> 내 <see cref="AnimationEvent"/> 호출용 메소드입니다.
        /// </summary>
        /// <param name="ev"></param>
        [Obsolete("Do not use. This method is intended to use only at AnimationClip events.", true)]
        public override sealed void TriggerAction(AnimationEvent ev)
        {
            Hash hash = new Hash(ev.stringParameter);

            if (!m_Parsed.TryGetValue(hash, out FMODAnimationEvent animEv)) return;

            // Visual Logic Graph
            {
                animEv.VisualGraph.Execute(m_OverrideRoot != null ? m_OverrideRoot : gameObject);
            }

            Audio audio = animEv.AudioReference.GetAudio(OnProcessParameter);

            audio.position = transform.position;
            audio.rotation = transform.rotation;

            audio.Play();

            audio.bindTransform = transform;
        }

        /// <summary>
        /// Audio 에 전달할 Parameter 값을 받아 수정할 수 있습니다.
        /// </summary>
        /// <param name="ev"></param>
        /// <param name="existingValue"></param>
        /// <returns>수정된 값을 반환하여 적용시킵니다.</returns>
        protected virtual float OnProcessParameter(
            FMOD.Studio.PARAMETER_DESCRIPTION ev, float existingValue) => existingValue;
    }
}
