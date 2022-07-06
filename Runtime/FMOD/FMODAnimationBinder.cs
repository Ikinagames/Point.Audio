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
using UnityEngine.Timeline;

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
        [SerializeField] private ArrayWrapper<FMODAnimationBindReference> m_BindReferences = ArrayWrapper<FMODAnimationBindReference>.Empty;
        [SerializeField] private ArrayWrapper<FMODAnimationEvent> m_Events = Array.Empty<FMODAnimationEvent>();

        [Space]
        [SerializeField] private GameObject m_OverrideRoot = null;

        // Parameter 가 많을 경우, 배열을 전체 탐색하기 보다는 Hashing 을 통해 속도 개선을 합니다.
        private Dictionary<Hash, FMODAnimationEvent> m_Parsed;
        
        private Audio m_CurrentAudio;
        private IFMODEvent[] m_PlayedWhileActives = Array.Empty<IFMODEvent>();

        /// <summary>
        /// 등록된 FMOD 애니메이션 이벤트 배열입니다.
        /// </summary>
        protected IReadOnlyList<FMODAnimationEvent> Events => m_Events;

        protected void Awake()
        {
            m_Parsed = new Dictionary<Hash, FMODAnimationEvent>();

            if (m_BindReference != null)
            {
                m_BindReference.AddToHashMap(ref m_Parsed);
            }
            for (int i = 0; i < m_BindReferences.Count; i++)
            {
                if (m_BindReferences[i] == null) continue;

                m_BindReferences[i].AddToHashMap(ref m_Parsed);
            }
            
            for (int i = 0; i < m_Events.Length; i++)
            {
                m_Events[i].Initialize();

                m_Parsed.Add(new Hash(m_Events[i].Name), m_Events[i]);
            }
        }
        protected void OnEnable()
        {
            ResumeWhileEvents();
        }
        protected void OnDisable()
        {
            StopAllWhileEvents();
        }

        public void AddBindReferences(IEnumerable<FMODAnimationBindReference> iter)
        {
            foreach (var item in iter)
            {
                item.AddToHashMap(ref m_Parsed);
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

            m_CurrentAudio = animEv.AudioReference.GetAudio(OnProcessParameter);

            m_CurrentAudio.position = transform.position;
            m_CurrentAudio.rotation = transform.rotation;

            // Visual Logic Graph
#if ENABLE_NODEGRAPH
            if (animEv.VisualGraph != null)
            {
                animEv.VisualGraph.Execute(
                    m_OverrideRoot != null ? m_OverrideRoot : gameObject, animEv.m_Processor);
            }
#endif

            m_CurrentAudio.Play();

            m_CurrentAudio.bindTransform = transform;
        }

        public IFMODEvent GetPlayedEvent(int index)
        {
            return m_PlayedWhileActives[index];
        }
        public IEnumerable<IFMODEvent> GetPlayedEvents(FMOD.GUID guid)
        {
            List<IFMODEvent> ev = new List<IFMODEvent>();
            for (int i = 0; i < m_PlayedWhileActives.Length; i++)
            {
                m_PlayedWhileActives[i].EventDescription.getID(out var id);
                if (id.Equals(guid))
                {
                    ev.Add(m_PlayedWhileActives[i]);
                }
            }

            return ev;
        }

        /// <summary>
        /// 오브젝트가 활성화되있는 동안 재생되는 이벤트 전부를 다시 시작합니다.
        /// </summary>
        /// <remarks>
        /// 만약 이벤트가 이미 재생중이라면 요청을 무시합니다.
        /// </remarks>
        public void ResumeWhileEvents()
        {
            if (m_PlayedWhileActives.Length != 0)
            {
                return;
            }

            List<IFMODEvent> playWhileEvents = new List<IFMODEvent>();
            if (m_BindReference != null)
            {
                playWhileEvents.AddRange(m_BindReference.PlayWhileActive(transform));
            }

            for (int i = 0; i < m_BindReferences.Count; i++)
            {
                if (m_BindReferences[i] == null) continue;

                playWhileEvents.AddRange(m_BindReferences[i].PlayWhileActive(transform));
            }

            m_PlayedWhileActives = playWhileEvents.ToArray();
        }
        /// <summary>
        /// 오브젝트가 활성화되있는 동안 재생되는 이벤트 전부를 정지합니다.
        /// </summary>
        public void StopAllWhileEvents()
        {
            for (int i = 0; i < m_PlayedWhileActives.Length; i++)
            {
                m_PlayedWhileActives[i].Stop();
            }
            m_PlayedWhileActives = Array.Empty<IFMODEvent>();
        }

        #region Inhert

        /// <summary>
        /// Audio 에 전달할 Parameter 값을 받아 수정할 수 있습니다.
        /// </summary>
        /// <remarks>
        /// 이벤트가 참조중인 모든 변수를 프로세싱합니다. 
        /// <seealso cref="ParamField"/> 에 값이 있을 경우에 기본값이 <paramref name="existingValue"/> 에 들어오는 것이 아닌,
        /// <seealso cref="ParamField.Value"/> 가 들어옵니다.
        /// </remarks>
        /// <param name="ev"></param>
        /// <param name="existingValue"></param>
        /// <returns>수정된 값을 반환하여 적용시킵니다.</returns>
        protected virtual float OnProcessParameter(
            FMOD.Studio.PARAMETER_DESCRIPTION ev, float existingValue) => existingValue;

        #endregion
    }
}
