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
using Point.Collections;

namespace Point.Audio
{
    //////////////////////////////////////////////////////////////////////////////////////////
    /*                                   Critical Section                                   */
    /*                                       수정금지                                        */
    /*                                                                                      */
    /*                               Do not modify this script                              */
    /*                              Unless know what you doing.                             */
    //////////////////////////////////////////////////////////////////////////////////////////

    [CreateAssetMenu(menuName = "Point/Audio/Create Event Reference Asset")]
    public sealed class FMODEventReferenceAsset : ScriptableObject, IFMODEventReference
    {
        [SerializeField] private EventReference m_Event;
        [FMODParam(false, DisableReflection = true)]
        [SerializeField] private ArrayWrapper<ParamField> m_Parameters = Array.Empty<ParamField>();

        [Space]
        [SerializeField] private bool m_AllowFadeOut = true;
        [SerializeField] private bool m_OverrideAttenuation;
        [SerializeField] private float m_OverrideMinDistance = -1, m_OverrideMaxDistance = -1;

        [Space]
        [SerializeField] private bool m_ExposeGlobalEvent = false;
        [SerializeField] private string m_ExposedName = string.Empty;

        public void SetExposedEvent(IFMODEvent ev)
        {
            if (!m_ExposeGlobalEvent) return;

            FMODManager.Instance[m_ExposedName] = ev;
        }
        public IFMODEvent GetEvent()
        {
            if (m_Event.IsSnapshot()) return GetSnapshot();
            else if (m_Event.IsEvent()) return GetAudio();

            return null;
        }

        private ParamField FindParamField(FMOD.Studio.PARAMETER_DESCRIPTION desc)
        {
            for (int i = 0; i < m_Parameters.Length; i++)
            {
                if (m_Parameters[i].Name.Equals(desc.name.ToString()))
                {
                    return m_Parameters[i];
                }
            }
            return null;
        }

        public Snapshot GetSnapshot()
        {
            Snapshot snapshot = new Snapshot(FMODManager.GetEventDescription(m_Event));
            return snapshot;
        }
        public Audio GetAudio()
        {
            Audio boxed = FMODManager.GetAudio(m_Event);
            boxed.AllowFadeout = m_AllowFadeOut;
            boxed.OverrideAttenuation = m_OverrideAttenuation;
            boxed.OverrideMinDistance = m_OverrideMinDistance;
            boxed.OverrideMaxDistance = m_OverrideMaxDistance;

            for (int i = 0; i < m_Parameters.Length; i++)
            {
                var param = m_Parameters[i].GetParamReference(boxed.eventDescription);

                boxed.SetParameter(param);
            }

            return boxed;
        }
        public Audio GetAudio(Func<FMOD.Studio.PARAMETER_DESCRIPTION, float, float> onProcessParam)
        {
            Audio boxed = FMODManager.GetAudio(m_Event);
            boxed.AllowFadeout = m_AllowFadeOut;
            boxed.OverrideAttenuation = m_OverrideAttenuation;
            boxed.OverrideMinDistance = m_OverrideMinDistance;
            boxed.OverrideMaxDistance = m_OverrideMaxDistance;

            var desc = boxed.eventDescription;
            desc.getParameterDescriptionCount(out int count);
            for (int i = 0; i < count; i++)
            {
                desc.getParameterDescriptionByIndex(i, out var paramDesc);
                if ((paramDesc.flags & FMOD.Studio.PARAMETER_FLAGS.READONLY) != 0 ||
                    (paramDesc.flags & FMOD.Studio.PARAMETER_FLAGS.GLOBAL) != 0 ||
                    (paramDesc.flags & FMOD.Studio.PARAMETER_FLAGS.AUTOMATIC) != 0)
                {
                    //string name = paramDesc.name;
                    //$"? {name} {TypeHelper.Enum<FMOD.Studio.PARAMETER_FLAGS>.ToString(paramDesc.flags)}".ToLog();
                    continue;
                }

                var existingParamField = FindParamField(paramDesc);
                if (existingParamField != null) continue;

                float value = onProcessParam.Invoke(paramDesc, paramDesc.defaultvalue);
                boxed.SetParameter(paramDesc, value);
            }

            for (int i = 0; i < m_Parameters.Length; i++)
            {
                var param = m_Parameters[i].GetParamReference(desc);
                param.value = onProcessParam.Invoke(param.description, m_Parameters[i].Value);

                boxed.SetParameter(param);
            }

            return boxed;
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////
    /*                                End of Critical Section                               */
    //////////////////////////////////////////////////////////////////////////////////////////
}
