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
using Unity.Collections;

namespace Point.Audio
{
    /// <summary>
    /// FMOD 의 로컬 Event parameter, 혹은 global parameter 입니다.
    /// </summary>
    [BurstCompatible]
    public struct ParamReference : 
        IEquatable<ParamReference>, 
        IEquatable<FMOD.Studio.PARAMETER_ID>,
        IEquatable<string>
    {
        // = 13 bytes
        // 8 bytes
        public FMOD.Studio.PARAMETER_DESCRIPTION description;
        // 4 bytes
        public float value;
        // 1 bytes
        public bool ignoreSeekSpeed;
        public bool isGlobal;

        public ParamReference(FMOD.Studio.EventDescription ev, string name)
        {
            ev.getParameterDescriptionByName(name, out var description);
            this.description = description;
            value = 0;
            ignoreSeekSpeed = false;
            isGlobal = false;
        }
        /// <summary>
        /// global parameter
        /// </summary>
        /// <param name="name"></param>
        public ParamReference(string name)
        {
            FMODManager.StudioSystem.getParameterDescriptionByName(name, out var description);
            this.description = description;
            value = 0;
            ignoreSeekSpeed = false;
            isGlobal = true;
        }
        public ParamReference(FMOD.Studio.EventDescription ev, string name, float value)
        {
            ev.getParameterDescriptionByName(name, out var description);
            this.description = description;
            this.value = value;
            ignoreSeekSpeed = false;
            isGlobal = false;
        }
        /// <summary>
        /// global parameter
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public ParamReference(string name, float value)
        {
            FMODManager.StudioSystem.getParameterDescriptionByName(name, out var description);
            this.description = description;
            this.value = value;
            ignoreSeekSpeed = false;
            isGlobal = true;
        }

        public bool Equals(ParamReference other) => description.id.data1.Equals(other.description.id.data1) && description.id.data2.Equals(other.description.id.data2) && isGlobal == other.isGlobal;
        public bool Equals(FMOD.Studio.PARAMETER_ID other) => description.id.data1.Equals(other.data1) && description.id.data2.Equals(other.data2);
        [NotBurstCompatible]
        public bool Equals(string parameterName)
        {
            string thisName = (string)description.name;
            return parameterName.Equals(thisName);
        }

        [NotBurstCompatible]
        public override string ToString()
        {
            const string c_Format = "{0}: {1}";
            return string.Format(c_Format, (string)description.name, value);
        }
    }
}
