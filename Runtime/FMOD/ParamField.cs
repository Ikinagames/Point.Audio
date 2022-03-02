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
using System.Reflection;

namespace Point.Audio
{
    [System.Serializable]
    public sealed class ParamField
    {
        [UnityEngine.SerializeField] private bool m_IsGlobal = true;
        [FMODUnity.ParamRef]
        [UnityEngine.SerializeField] private string m_Name;
        [UnityEngine.SerializeField] private float m_Value;
        [UnityEngine.SerializeField] private bool m_IgnoreSeekSpeed = false;

        [UnityEngine.Space]
        [UnityEngine.SerializeField] private bool m_EnableValueReflection = false;
        [UnityEngine.SerializeField] private UnityEngine.Object m_ReferenceObject = null;
        [UnityEngine.SerializeField] private string m_ValueFieldName = string.Empty;

        [NonSerialized] private bool m_Parsed = false;
        [NonSerialized] FMOD.Studio.PARAMETER_DESCRIPTION m_ParsedParameterDescription;
        [NonSerialized] private PropertyInfo m_PropertyInfo;
        [NonSerialized] private FieldInfo m_FieldInfo;

        public ParamReference GetGlobalParamReference()
        {
            float targetValue;
            if (m_EnableValueReflection && m_ReferenceObject != null)
            {
                if (m_FieldInfo == null || m_PropertyInfo == null) Lookup(m_ReferenceObject.GetType());

                targetValue = GetReflectedValue(m_ReferenceObject);
            }
            else targetValue = m_Value;

            if (!m_Parsed)
            {
                FMODManager.StudioSystem.getParameterDescriptionByName(m_Name, out m_ParsedParameterDescription);
            }

            return new ParamReference
            {
                description = m_ParsedParameterDescription,
                value = targetValue,
                ignoreSeekSpeed = m_IgnoreSeekSpeed
            };
        }
        public ParamReference GetParamReference(FMOD.Studio.EventDescription ev)
        {
            float targetValue;
            if (m_EnableValueReflection && m_ReferenceObject != null)
            {
                if (m_FieldInfo == null || m_PropertyInfo == null) Lookup(m_ReferenceObject.GetType());

                targetValue = GetReflectedValue(m_ReferenceObject);
            }
            else targetValue = m_Value;

            if (m_IsGlobal)
            {
                if (!m_Parsed)
                {
                    var result = FMODManager.StudioSystem.getParameterDescriptionByName(m_Name, out m_ParsedParameterDescription);
                    if (result != FMOD.RESULT.OK)
                    {
                        PointHelper.LogError(Channel.Audio,
                            $"Global parameter({m_Name}) could not be found.");
                    }
                }
            }
            else
            {
                var result = ev.getParameterDescriptionByName(m_Name, out m_ParsedParameterDescription);
                if (result != FMOD.RESULT.OK)
                {
                    PointHelper.LogError(Channel.Audio,
                        $"Parameter({m_Name}) could not be found.");
                }
            }

            return new ParamReference
            {
                description = m_ParsedParameterDescription,
                value = targetValue,
                ignoreSeekSpeed = m_IgnoreSeekSpeed
            };
        }

        private void Lookup(Type type)
        {
#if DEBUG_MODE
            if (string.IsNullOrEmpty(m_ValueFieldName))
            {
                PointHelper.LogError(Channel.Audio,
                    $"Field name is cannot be null or empty. " +
                    $"This is not allowed.");

                return;
            }
#endif
            m_FieldInfo = type.GetField(m_ValueFieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            if (m_FieldInfo == null)
            {
                m_PropertyInfo = type.GetProperty(m_ValueFieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            }
#if DEBUG_MODE
            if (m_FieldInfo == null && m_PropertyInfo == null)
            {
                PointHelper.LogError(Channel.Audio,
                    $"Could not found field or property name of ({m_ValueFieldName}) in {type.FullName}.");
            }
#endif
        }
        private float GetReflectedValue(object caller)
        {
#if DEBUG_MODE
            if (m_FieldInfo == null && m_PropertyInfo == null)
            {
                return 0;
            }
#endif
            object value;
            if (m_FieldInfo == null)
            {
                value = m_PropertyInfo.GetGetMethod().Invoke(caller, null);
            }
            else value = m_FieldInfo.GetValue(caller);

            $"{value} {value.GetType()}".ToLog();

            if (value is bool boolen)
            {
                return boolen ? 1 : 0;
            }
            return Convert.ToSingle(value);
        }

        public void Execute()
        {
            ParamReference param;
#if DEBUG_MODE
            if (m_IsGlobal)
#endif
            {
                param = GetGlobalParamReference();
                FMODManager.SetGlobalParameter(param);

                return;
            }
#if DEBUG_MODE
            PointHelper.LogError(Channel.Audio,
                $"Cannot execute parameter field which is not global.");
#endif
        }
        public void Execute(FMOD.Studio.EventInstance ev)
        {
#if DEBUG_MODE
            if (m_IsGlobal)
            {
                Execute();

                PointHelper.LogError(Channel.Audio,
                    $"You can execute this parameter that is global without event description. " +
                    $"This is not allowed at build. Please use Execute without any params.");
                return;
            }
#endif

            FMOD.RESULT result = ev.getDescription(out var description);
#if DEBUG_MODE
            if ((result & FMOD.RESULT.OK) != FMOD.RESULT.OK)
            {
                PointHelper.LogError(Channel.Audio,
                    $"Fatal error. Could\'nt retrieved parameter description from fmod. " +
                    $"This is not allowed.");

                return;
            }
#endif

            ParamReference param = GetParamReference(description);
            result = ev.setParameterByID(param.description.id, m_Value, m_IgnoreSeekSpeed);

#if DEBUG_MODE
            if ((result & FMOD.RESULT.OK) != FMOD.RESULT.OK)
            {
                PointHelper.LogError(Channel.Audio,
                    $"Fatal error. Could\'nt set parameter to fmod. " +
                    $"This is not allowed.");
            }
#endif
        }
    }
}
