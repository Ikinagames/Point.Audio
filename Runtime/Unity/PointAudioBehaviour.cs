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
using UnityEngine;

namespace Point.Audio
{
    public class PointAudioBehaviour : PointMonobehaviour
    {
        public enum PlayOption
        {
            None = 0,

            OnEnable,
        }
        [Flags]
        public enum StopOption
        {
            None = 0,

            OnDisable = 0b0001,
            OnDestroy = 0b0010,
        }
        public enum StopParameter
        {
            StopImmediate,
            FadeOut
        }

        [SerializeField]
        protected PlayOption m_PlayOption = PlayOption.OnEnable;
        [SerializeField]
        protected StopOption m_StopOption = StopOption.OnDisable | StopOption.OnDestroy;
        [SerializeField]
        protected AdditionalAudioOptions m_PlayParameter = default(AdditionalAudioOptions);
        [SerializeField]
        protected StopParameter m_StopParameter = StopParameter.StopImmediate;
        [SerializeField]
        protected float m_FadeTime = .1f;

        [Space]
        [SerializeField]
        protected PlayableAudioClip m_Clip;

        [NonSerialized]
        private Audio m_Audio;

        protected virtual void OnEnable()
        {
            if (m_PlayOption != PlayOption.OnEnable) return;

            Play();
        }
        protected virtual void OnDisable()
        {
            if ((m_StopOption & StopOption.OnDisable) != StopOption.OnDisable) return;

            Stop();
        }
        protected virtual void OnDestroy()
        {
            if ((m_StopOption & StopOption.OnDestroy) != StopOption.OnDestroy) return;

            Stop();
        }

        public void Play()
        {
            if (m_Audio.IsValid())
            {
                Stop();
            }

            m_Audio = AudioManager.Play(m_Clip, m_PlayParameter);
        }
        public void Stop()
        {
            if (!m_Audio.IsValid()) return;

            switch (m_StopParameter)
            {
                default:
                case StopParameter.StopImmediate:
                    m_Audio.Reserve();

                    break;
                case StopParameter.FadeOut:
                    m_Audio.volume.Fade(0, m_FadeTime);
                    m_Audio.Callback(m_FadeTime + Mathf.Epsilon, ReserveAudio);

                    break;
            }
        }

        private static void ReserveAudio(Audio audio)
        {
            audio.Reserve();
        }
    }
}