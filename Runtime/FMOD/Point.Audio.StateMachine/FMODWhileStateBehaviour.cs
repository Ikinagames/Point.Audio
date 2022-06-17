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
using System;
using System.Reflection;
using UnityEngine;

namespace Point.Audio.StateMachine
{
    public class FMODWhileStateBehaviour : FMODStateBehaviour
    {
        [SerializeField] private FMODEventReference m_AudioReference;

        [Space]
        [SerializeField] private ArrayWrapper<ParamField> m_OnExitParameters = Array.Empty<ParamField>();
        [SerializeField] private bool m_StopAudioOnExit = false;

        [NonSerialized] private Audio m_Audio;
        [NonSerialized] private bool m_StateExitFired = false;
        
        protected Audio CurrentAudio => m_Audio;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnStateEnter(animator, stateInfo, layerIndex);

            m_StateExitFired = false;
            m_Audio = m_AudioReference.GetAudio();

            bool is3D = m_Audio.Is3D;
            if (is3D)
            {
                var tr = animator.transform;
                m_Audio.position = tr.position;
                m_Audio.rotation = tr.rotation;
            }

            m_Audio.Play();
#if DEBUG_MODE
            m_Audio.EventDescription.getPath(out var path);
            PointHelper.Log(Channel.Audio,
                $"Playing audio({path} :: {m_Audio.hash.Value}:: {m_Audio.audioHandler.Value.instanceHash.Value})");
#endif

            if (is3D) m_Audio.bindTransform = animator.transform;
        }
        protected override void OnExitTransition()
        {
            base.OnExitTransition();

            if (!m_StateExitFired)
            {
                for (int i = 0; i < m_OnExitParameters.Length; i++)
                {
                    var param = m_OnExitParameters[i].GetParamReference(m_Audio.eventDescription);
                    m_Audio.SetParameter(param);
                }

                if (m_StopAudioOnExit)
                {
                    StopAudio();
                }

                m_StateExitFired = true;
            }
        }
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnStateExit(animator, stateInfo, layerIndex);

            if (!m_StateExitFired)
            {
                for (int i = 0; i < m_OnExitParameters.Length; i++)
                {
                    var param = m_OnExitParameters[i].GetParamReference(m_Audio.eventDescription);
                    m_Audio.SetParameter(param);
                }

                if (m_StopAudioOnExit)
                {
                    StopAudio();
                }

                m_StateExitFired = true;
            }
        }

        private void OnDisable()
        {
            StopAudio();
        }
        private void OnDestroy()
        {
            StopAudio();
        }
        private void StopAudio()
        {
            if (!m_Audio.IsValid(true))
            {
                "not valid retn".ToLog();
                return;
            }

            m_Audio.Stop();
#if DEBUG_MODE
            m_Audio.EventDescription.getPath(out var path);
            PointHelper.Log(Channel.Audio,
                $"Stop audio({path})");
#endif
        }

        protected ParamReference GetParamReference<TEnum>()
            where TEnum : struct, IConvertible
        {
#if DEBUG_MODE
            if (!FMODExtensions.IsFMODEnum<TEnum>())
            {
                PointHelper.LogError(Channel.Audio,
                    $"Enum({TypeHelper.TypeOf<TEnum>.ToString()}) is not fmod enum which has {nameof(FMODEnumAttribute)}.");

                return default(ParamReference);
            }
#endif
            return m_Audio.GetParameter(FMODExtensions.ConvertToName<TEnum>());
        }
        protected ParamReference GetParamReference(string name)
        {
            if (!m_Audio.IsValidID())
            {
                PointHelper.LogError(Channel.Audio,
                    $"This audio is invalid.");

                return default(ParamReference);
            }

            return m_Audio.GetParameter(name);
        }
    }
}
