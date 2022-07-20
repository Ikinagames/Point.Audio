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
using System.Collections.Generic;
using UnityEngine;

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

        public static void Play(in this Audio t, in float delay)
        {
            AudioDelayedPlayer.Instance.Register(in t, in delay);
        }

        #region Debug

        public static bool IsConsiderAsError(this RESULT t)
        {
            if ((t & RESULT.OK) != RESULT.OK &&
                (t & RESULT.IGNORED) != RESULT.IGNORED)
            {
                return true;
            }
            return false;
        }
        public static bool IsRequireLog(this RESULT t)
        {
            RESULT notOk = ~RESULT.OK;
            if ((t & notOk) != 0)
            {
                return true;
            }

            return false;
        }

        private static readonly Dictionary<AudioKey, HashSet<RESULT>> s_OnetimeLogged = new Dictionary<AudioKey, HashSet<RESULT>>();
        private static bool CanLog(in RESULT t, in AudioKey audioKey)
        {
            if ((t & RESULT.AudioClip_NotFound_In_AssetBundle) == RESULT.AudioClip_NotFound_In_AssetBundle)
            {
                if (!s_OnetimeLogged.TryGetValue(audioKey, out var set))
                {
                    set = new HashSet<RESULT>();
                    s_OnetimeLogged.Add(audioKey, set);
                }

                if (set.Contains(t)) return false;

                set.Add(t);
                return true;
            }

            return true;
        }

        private static string GetResultString(in RESULT t)
        {
            if ((t & RESULT.AudioClip_NotFound_In_AssetBundle) == RESULT.AudioClip_NotFound_In_AssetBundle)
            {
                if ((t & RESULT.OK) == RESULT.OK)
                {
                    return $"AudioClip not found in AssetBundle. This will be accepted only in editor with AssetDatabase.";
                }
                return "AudioClip not found in AssetBundle either local. This is not allowed.";
            }

            return t.ToReadableString();
        }
        public static void SendLog(this RESULT t, in AudioKey audioKey)
        {
            if (!CanLog(t, audioKey))
            {
                return;
            }

            const string c_ErrorFormat = "Play AudioClip({0}) request has been falied with {1}";
            PointHelper.LogError(Channel.Audio,
                   string.Format(c_ErrorFormat, audioKey, GetResultString(t)));
        }
        public static void SendLog(this RESULT t, in AudioKey audioKey, in Vector3 position)
        {
            if (!CanLog(t, audioKey))
            {
                return;
            }

            const string c_ErrorFormat = "Play AudioClip({0}) at {1} request has been falied with {2}";
            PointHelper.LogError(Channel.Audio,
                   string.Format(c_ErrorFormat, audioKey, position, GetResultString(t)));
        }
        public static string ToReadableString(this RESULT t)
        {
            return TypeHelper.Enum<RESULT>.ToString(t);
        }

        #endregion
    }
}