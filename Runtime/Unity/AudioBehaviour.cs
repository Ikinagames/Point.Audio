﻿// Copyright 2022 Ikina Games
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
    public class AudioBehaviour : PointMonobehaviour
    {
        public enum PlayOption
        {
            None = 0,

            OnEnable,
        }
        public enum StopOption
        {
            None = 0,
            
            StopImmediate,
            FadeOut
        }

        [SerializeField]
        private PlayOption m_PlayOption = PlayOption.OnEnable;
        [SerializeField]
        private StopOption m_StopOption = StopOption.FadeOut;
        [SerializeField]
        private AssetPathField<AudioClip> m_Clip;

        [NonSerialized]
        private Audio m_Audio;

        private void OnEnable()
        {
            if (m_PlayOption != PlayOption.OnEnable) return;

            m_Audio = AudioManager.Play(m_Clip);
        }
        private void OnDisable()
        {
            if (!m_Audio.hasAudioSource) return;

            m_Audio.Reserve();
        }
    }
}