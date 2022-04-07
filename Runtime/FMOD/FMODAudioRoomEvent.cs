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
        private ArrayWrapper<FMODEventReference> m_PlayOnEnter = Array.Empty<FMODEventReference>();

        private FMODAudioRoom m_AudioRoom;
        private IFMODEvent[] m_PlayedEvents;

        #region Monobehaviour Messages

        private void Awake()
        {
            m_AudioRoom = GetComponent<FMODAudioRoom>();

            m_PlayedEvents = new IFMODEvent[m_PlayOnEnter.Length];
        }
        private void OnEnable()
        {
            m_AudioRoom.OnEntered += OnEnteredHandler;
        }
        private void OnDisable()
        {
            m_AudioRoom.OnEntered -= OnEnteredHandler;
        }

        #endregion

        private void OnEnteredHandler()
        {
            m_PlayOnEnter.Play(m_PlayedEvents);
        }
    }
}
