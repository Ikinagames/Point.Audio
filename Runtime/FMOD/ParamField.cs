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

using System;
using System.Reflection;

namespace Point.Audio
{
    [System.Serializable]
    public sealed class ParamField
    {
        [FMODUnity.ParamRef]
        [UnityEngine.SerializeField] private string m_Name;
        [UnityEngine.SerializeField] private float m_Value;
        [UnityEngine.SerializeField] private bool m_IgnoreSeekSpeed = false;

        [UnityEngine.Space]
        [UnityEngine.SerializeField] private bool m_EnableValueReflection = false;
        [UnityEngine.SerializeField] private string m_ValueFieldName = string.Empty;

        [NonSerialized] private FieldInfo m_FieldInfo;

        public ParamReference GetParamReference(object caller)
        {
            float targetValue;
            if (m_EnableValueReflection && caller != null)
            {
                if (m_FieldInfo == null) Lookup(caller.GetType());

                targetValue = (float)m_FieldInfo.GetValue(caller);
            }
            else targetValue = m_Value;

            FMODManager.StudioSystem.getParameterDescriptionByName(m_Name, out var desc);

            return new ParamReference
            {
                id = desc.id,
                value = targetValue,
                ignoreSeekSpeed = m_IgnoreSeekSpeed
            };
        }

        private void Lookup(Type type)
        {
#if DEBUG_MODE
            if (string.IsNullOrEmpty(m_ValueFieldName))
            {
                Collections.Point.LogError(Collections.Point.LogChannel.Audio,
                    $"Field name is cannot be null or empty. " +
                    $"This is not allowed.");

                return;
            }
#endif
            m_FieldInfo = type.GetField(m_ValueFieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }
    }
}
