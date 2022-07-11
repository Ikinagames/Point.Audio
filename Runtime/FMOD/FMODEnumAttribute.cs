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

namespace Point.Audio
{
    //////////////////////////////////////////////////////////////////////////////////////////
    /*                                   Critical Section                                   */
    /*                                       수정금지                                        */
    /*                                                                                      */
    /*                               Do not modify this script                              */
    /*                              Unless know what you doing.                             */
    //////////////////////////////////////////////////////////////////////////////////////////

    [System.AttributeUsage(System.AttributeTargets.Enum, AllowMultiple = false)]
    public class FMODEnumAttribute : System.Attribute
    {
        /// <summary>
        /// <see langword="null"/> 이 아닐 경우, 이 타입의
        /// <see cref="System.Type.FullName"/> 이 아닌 이 string 을 사용합니다.
        /// </summary>
        public string Name;

        public FMODEnumAttribute(string name)
        {
            Name = name;
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////
    /*                                End of Critical Section                               */
    //////////////////////////////////////////////////////////////////////////////////////////
}
