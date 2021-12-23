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

namespace Point.Audio.UnityFMOD
{
    [BurstCompatible]
    public struct ParamReference : IEquatable<ParamReference>, IEquatable<FMOD.Studio.PARAMETER_ID>
    {
        // = 13 bytes
        // 8 bytes
        public FMOD.Studio.PARAMETER_ID id;
        // 4 bytes
        public float value;
        // 1 bytes
        public bool ignoreSeekSpeed;

        public FMOD.Studio.PARAMETER_DESCRIPTION description
        {
            get
            {
                FMODManager.StudioSystem.getParameterDescriptionByID(id, out var description);
                return description;
            }
        }

        public ParamReference(string name)
        {
            FMODManager.StudioSystem.getParameterDescriptionByName(name, out var description);
            id = description.id;
            value = 0;
            ignoreSeekSpeed = false;
        }
        public ParamReference(string name, float value)
        {
            FMODManager.StudioSystem.getParameterDescriptionByName(name, out var description);
            id = description.id;
            this.value = value;
            ignoreSeekSpeed = false;
        }



        public bool Equals(ParamReference other) => id.data1.Equals(other.id.data1) && id.data2.Equals(other.id.data2);
        public bool Equals(FMOD.Studio.PARAMETER_ID other) => id.data1.Equals(other.data1) && id.data2.Equals(other.data2);
    }
}
