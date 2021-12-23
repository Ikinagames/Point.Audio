﻿// Copyright 2021 Ikina Games
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
using Unity.Mathematics;

namespace Point.Audio.UnityFMOD
{
    internal struct AudioHandler : IEmpty
    {
        public FMOD.Studio.EventInstance instance;

        public float3 translation;
        public quaternion rotation;

        public bool IsEmpty() => !instance.isValid();
        public FMOD.ATTRIBUTES_3D Get3DAttributes()
        {
            FMOD.ATTRIBUTES_3D att = new FMOD.ATTRIBUTES_3D();

            float3
                forward = math.mul(rotation, math.forward()),
                up = math.mul(rotation, math.up());

            att.forward = new FMOD.VECTOR
            {
                x = forward.x,
                y = forward.y,
                z = forward.z
            };
            att.up = new FMOD.VECTOR
            {
                x = up.x,
                y = up.y,
                z = up.z
            };
            att.position = new FMOD.VECTOR
            {
                x = translation.x,
                y = translation.y,
                z = translation.z
            };

            return att;
        }
    }
}
