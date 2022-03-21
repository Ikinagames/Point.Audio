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

using FMOD.Studio;

namespace Point.Audio
{
    /// <summary>
    /// FMOD
    /// </summary>
    [UnityEngine.Scripting.RequireImplementors]
    public interface IUserPropertyProcessor
    {
        void OnProcess(ref Audio audio, in FMOD.Studio.USER_PROPERTY property);
    }

    //[Point.Collections.InternalIgnoreType]
    //public struct TestUserPropertyProcessor : IUserPropertyProcessor
    //{
    //    public void OnProcess(ref Audio audio, in USER_PROPERTY property)
    //    {
    //        USER_PROPERTY_TYPE t = property.type;
    //    }
    //}
}
