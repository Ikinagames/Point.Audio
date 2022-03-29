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

namespace Point.Audio
{
    /// <summary>
    /// <see cref="ParamField"/> 에 대한 LowLevel 컨트롤 Attribute 입니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class FMODParamAttribute : Attribute
    {
        /// <summary>
        /// 전역 Parameter 임을 명시할 것인지 설정합니다. 
        /// <see langword="true"/> 일 경우, 인스펙터에서 <see langword="false"/>로 수정될 수 없습니다.
        /// </summary>
        public bool GlobalParameter = false;
        public bool DisableReflection = false;

        public FMODParamAttribute(bool isGlobalParameter)
        {
            GlobalParameter = isGlobalParameter;
        }
    }
}
