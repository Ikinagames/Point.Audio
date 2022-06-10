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

using FMOD.Studio;
using Point.Collections;
using Point.Collections.Timeline;
using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Point.Audio.Timeline
{
    public sealed class SetParameterMarker : Marker, INotification, INotificationExecute
    {
        public PropertyName id { get; }

        [FMODParam(true, true)]
        public ParamField[] m_GlobalParameters = Array.Empty<ParamField>();

        [Space]
        public string m_ExposedEventName = string.Empty;
        [FMODParam(false, true)]
        public ParamField[] m_Parameters = Array.Empty<ParamField>();

        public void Execute(PlayableDirector director, Playable origin, object context)
        {
            "In".ToLog();

            for (int i = 0; i < m_GlobalParameters.Length; i++)
            {
                m_GlobalParameters[i].Execute();
            }

            HandleExposeEvent();
        }
        private void HandleExposeEvent()
        {
            if (m_ExposedEventName.IsNullOrEmpty()) return;

            IFMODEvent ev = FMODManager.Instance[m_ExposedEventName];
            if (ev == null || !ev.IsEvent()) return;

            Audio audio = (Audio)ev;
            if (!audio.IsValid(true)) return;

            for (int i = 0; i < m_Parameters.Length; i++)
            {
                ev.SetParameter(m_Parameters[i].Name, m_Parameters[i].Value);
            }
        }
    }
}
