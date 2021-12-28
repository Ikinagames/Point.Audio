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

using UnityEngine;
using System;
using FMODUnity;
using Unity.Mathematics;

namespace Point.Audio
{
    [Serializable]
    public sealed class AudioReference
    {
        [SerializeField] private EventReference m_Event;
        [SerializeField] private ParamField[] m_Parameters = Array.Empty<ParamField>();
        [SerializeField] private Audio m_AudioSettings = new Audio
        {
            AllowFadeout = true,
            _rotation = quaternion.identity,
        };

        public Audio GetAudio()
        {
            Audio boxed = m_AudioSettings;
            FMODManager.GetAudio(m_Event, ref boxed);

            for (int i = 0; i < m_Parameters.Length; i++)
            {
                var param = m_Parameters[i].GetParamReference(boxed.eventDescription);
                
                boxed.SetParameter(param);
            }

            return boxed;
        }
        public Audio GetAudio(Func<FMOD.Studio.PARAMETER_DESCRIPTION, float, float> onProcessParam)
        {
            Audio boxed = m_AudioSettings;
            FMODManager.GetAudio(m_Event, ref boxed);

            for (int i = 0; i < m_Parameters.Length; i++)
            {
                var param = m_Parameters[i].GetParamReference(boxed.eventDescription);
                param.value = onProcessParam.Invoke(param.description, param.value);

                boxed.SetParameter(param);
            }

            return boxed;
        }
    }
}
