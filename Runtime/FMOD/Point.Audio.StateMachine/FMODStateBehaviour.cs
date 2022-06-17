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
using UnityEngine;
using UnityEngine.Animations;

namespace Point.Audio.StateMachine
{
    public abstract class FMODStateBehaviour : StateMachineBehaviour
    {
        public struct StateInfo
        {
            public Animator animator;
            public AnimatorStateInfo stateInfo;
            public int layerIndex;
        }

        [NonSerialized] private StateInfo m_CurrentStateInfo;
        [NonSerialized] private bool m_ExitTransitionFired = false;

        protected StateInfo CurrentStateInfo => m_CurrentStateInfo;
        protected AnimatorTransitionInfo TransitionInfo => CurrentStateInfo.animator.GetAnimatorTransitionInfo(CurrentStateInfo.layerIndex);

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnStateEnter(animator, stateInfo, layerIndex);

            m_ExitTransitionFired = false;
        }
        public override void OnStateUpdate(
            Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnStateUpdate(animator, stateInfo, layerIndex);

            m_CurrentStateInfo = new StateInfo
            {
                animator = animator,
                stateInfo = stateInfo,
                layerIndex = layerIndex
            };

            if (!m_ExitTransitionFired &&
                IsInTransition() && 
                !animator.GetNextAnimatorStateInfo(layerIndex).Equals(stateInfo))
            {
                OnExitTransition();

                m_ExitTransitionFired = true;
            }
        }

        protected virtual void OnExitTransition() { }

        protected bool IsInTransition()
        {
            return CurrentStateInfo.animator.IsInTransition(CurrentStateInfo.layerIndex);
        }
        protected bool IsInTransition(string from, string to)
        {
            const string c_Format = "{0} -> {1}";

            AnimatorTransitionInfo transitionInfo = TransitionInfo;
            if (transitionInfo.duration > 0 &&
                transitionInfo.IsName(string.Format(c_Format, from, to)))
            {
                return true;
            }
            return false;
        }
    }
}
