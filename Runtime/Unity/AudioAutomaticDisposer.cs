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

using UnityEngine;
using Point.Collections;
using System.Collections.Generic;

namespace Point.Audio
{
    [AddComponentMenu("")]
    internal sealed class AudioAutomaticDisposer : StaticMonobehaviour<AudioAutomaticDisposer>
    {
        protected override bool EnableLog => false;
        protected override bool HideInInspector => true;

        private readonly List<Audio> m_Audios = new List<Audio>();

        public void Register(in Audio audio)
        {
            m_Audios.Add(audio);
        }

        private void LateUpdate()
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPaused) return;
#endif
            for (int i = m_Audios.Count - 1; i >= 0; i--)
            {
                if (m_Audios[i].isPlaying) continue;

                m_Audios[i].Reserve();
                m_Audios.RemoveAt(i);
            }
        }
        private void OnDestroy()
        {
            for (int i = 0; i < m_Audios.Count; i++)
            {
                m_Audios[i].Reserve();
            }
            m_Audios.Clear();
        }
    }
}