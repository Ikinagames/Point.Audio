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

using Point.Collections;
using System;
using Unity.Mathematics;

namespace Point.Audio
{
    public struct Volume : IFadeable<float>, IValidation, IEquatable<Volume>, IEquatable<float>
    {
        private int m_InstanceID;
        private float m_Value;

        public float value
        {
            get
            {
                return m_Value;
            }
            set
            {
                if (!IsValid())
                {
                    "fatal error. cannot set volume".ToLog();
                    return;
                }

                AudioManager.GetAudioSource(in m_InstanceID).volume = value;
                m_Value = value;
            }
        }
        object IFadeable.value => AudioManager.GetAudioSource(in m_InstanceID).volume;
        float IFadeable<float>.value => AudioManager.GetAudioSource(in m_InstanceID).volume;

        internal Volume(int instanceID)
        {
            m_InstanceID = instanceID;
            m_Value = AudioManager.GetAudioSource(in m_InstanceID).volume;
        }

        public bool IsValid() => m_InstanceID != 0 && AudioManager.GetAudioSource(in m_InstanceID) != null;

        void IFadeable.SetValue(object value, object targetValue, float t)
        {
            ((IFadeable<float>)this).SetValue((float)value, (float)targetValue, t);
        }
        void IFadeable<float>.SetValue(float value, float targetValue, float t)
        {
            float result = math.lerp(value, targetValue, t);

            this.value = result;
        }

        public bool Equals(Volume other) => m_InstanceID.Equals(other.m_InstanceID);
        public bool Equals(float other) => value.Equals(other);

        public static implicit operator float(Volume t) => t.value;
    }
}