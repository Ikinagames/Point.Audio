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

using FMODUnity;
using Point.Collections;
using System;
using UnityEngine;

namespace Point.Audio
{
    [AddComponentMenu("Point/FMOD/Audio Room Event")]
    [RequireComponent(typeof(FMODAudioRoom))]
    public sealed class FMODAudioRoomEvent : FMODBehaviour
    {
        [SerializeField]
        private string m_DebugName = string.Empty;
        [SerializeField]
        private ArrayWrapper<FMODEventReference> m_PlayOnEnter = Array.Empty<FMODEventReference>();

        private FMODAudioRoom m_AudioRoom;
        private IFMODEvent[] m_PlayedEvents = Array.Empty<IFMODEvent>();

        #region Monobehaviour Messages

        private void Awake()
        {
            m_AudioRoom = GetComponent<FMODAudioRoom>();
        }
        private void OnEnable()
        {
            m_PlayedEvents = new IFMODEvent[m_PlayOnEnter.Length];

            m_AudioRoom.OnEntered += OnEnteredHandler;
            m_AudioRoom.OnExited += OnExitedHandler;
        }

        private void OnDisable()
        {
            m_AudioRoom.OnEntered -= OnEnteredHandler;
            m_AudioRoom.OnExited -= OnExitedHandler;
        }

        #endregion

        private void OnEnteredHandler()
        {
            m_PlayOnEnter.Play(m_PlayedEvents);
#if DEBUG_MODE
            PointHelper.Log(Channel.Audio,
                $"Play events({m_PlayedEvents.Length}) on enter at {(string.IsNullOrEmpty(m_DebugName) ? gameObject.name : m_DebugName)}");
#endif
        }
        private void OnExitedHandler()
        {
            for (int i = 0; i < m_PlayedEvents.Length; i++)
            {
                if (m_PlayedEvents[i].IsValid())
                {
                    m_PlayedEvents[i].Stop();
                }
            }
#if DEBUG_MODE
            PointHelper.Log(Channel.Audio,
                $"Stop events({m_PlayedEvents.Length}) on enter at {(string.IsNullOrEmpty(m_DebugName) ? gameObject.name : m_DebugName)}");
#endif
        }
    }
}
