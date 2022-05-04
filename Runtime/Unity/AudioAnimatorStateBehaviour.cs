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
using Point.Collections.Events;
using System;
using UnityEngine;

namespace Point.Audio
{
    public sealed class AudioAnimatorStateBehaviour : StateMachineBehaviour
    {
        [Flags]
        public enum PlayOptions
        {
            None            =   0,

            OnStateEnter    =   0b0001,
            OnStateExit     =   0b0010,
        }

        [SerializeField]
        private AssetPathField<AudioClip>[] m_Key = Array.Empty<AssetPathField<AudioClip>>();

        [Space]
        [SerializeField]
        private PlayOptions m_PlayOptions = PlayOptions.OnStateEnter;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            Play(PlayOptions.OnStateEnter, animator);
        }
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            Play(PlayOptions.OnStateExit, animator);
        }

        private void Play(PlayOptions playOptions, Animator animator)
        {
            if ((m_PlayOptions & playOptions) != playOptions) return;

            for (int i = 0; i < m_Key.Length; i++)
            {
                AudioManager.Play(m_Key[i], animator.transform.position);
            }
        }
    }
}