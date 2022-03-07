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

using Point.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Point.Audio
{
    public sealed class FMODRuntimeVariables : StaticScriptableObject<FMODRuntimeVariables>
    {
        [Serializable]
        internal sealed class SceneContainer
        {
            [SerializeField] private string m_SceneName;

            [SerializeField] public ParamField[] m_GlobalParameters = Array.Empty<ParamField>();
            [SerializeField] public AudioReference[] m_GlobalAudios = Array.Empty<AudioReference>();

            public bool IsTargetScene(Scene scene)
            {
                return m_SceneName.Equals(scene.name);
            }
        }

        [SerializeField] private AudioReference[] m_PlayOnStart = Array.Empty<AudioReference>();
        [SerializeField] private SceneContainer[] m_SceneDependencies = Array.Empty<SceneContainer>();

        private readonly List<Audio> m_GlobalAudios = new List<Audio>();

        public void Initialize()
        {
            for (int i = 0; i < m_PlayOnStart.Length; i++)
            {
                m_PlayOnStart[i].GetAudio().Play();
            }
        }
        public void StopGlobalAudios()
        {
            for (int i = 0; i < m_GlobalAudios.Count; i++)
            {
                m_GlobalAudios[i].Stop();
            }
            m_GlobalAudios.Clear();
        }
        public void StartSceneDependencies(Scene scene)
        {
            for (int i = 0; i < m_SceneDependencies.Length; i++)
            {
                if (!m_SceneDependencies[i].IsTargetScene(scene))
                {
                    continue;
                }

                foreach (var param in m_SceneDependencies[i].m_GlobalParameters)
                {
                    FMODManager.SetGlobalParameter(param.GetGlobalParamReference());
                }
                foreach (var audioRef in m_SceneDependencies[i].m_GlobalAudios)
                {
                    Audio audio = audioRef.GetAudio();
                    audio.Play();

                    m_GlobalAudios.Add(audio);
                }
            }
        }
    }
}
