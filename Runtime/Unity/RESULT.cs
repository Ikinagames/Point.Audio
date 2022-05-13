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

using System;

namespace Point.Audio
{
    [Flags]
    public enum RESULT
    {
        INVALID         =   0,
        OK              =   0b0001 << 0,
        IGNORED         =   0b0010 << 0,

        //
        AUDIOCLIP       =   0b0001 << 4,
        ASSETBUNDLE     =   0b0010 << 4,
        AUDIOKEY        =   0b0100 << 4,

        //
        NOTFOUND        =   0b0001 << 8,
        NOTLOADED       =   0b0010 << 8,
        NOTVALID        =   0b0100 << 8,

        AudioClip_NotFound_In_AssetBundle = AUDIOCLIP | NOTFOUND | ASSETBUNDLE,
    }
}