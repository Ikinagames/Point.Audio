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
    public static class AudioExtensions
    {
        /// <summary>
        /// 오디오가 재생이 완료되면 자동으로 <see cref="AudioManager"/> 에게 반환합니다.
        /// </summary>
        /// <param name="t"></param>
        public static void AutoDisposal(in this Audio t)
        {
            AudioAutomaticDisposer.Instance.Register(t);
        }
    }
}