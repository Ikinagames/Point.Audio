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
using UnityEngine;

namespace Point.Audio
{
    public sealed class AudioList : StaticScriptableObject<AudioList>
    {
        [Serializable]
        internal sealed class SceneDependenceParam
        {
            [SerializeField] private string m_SceneName;

            [FMODParam(GlobalParameter = true, DisableReflection = true)]
            [SerializeField] private ParamField m_Parameter;

            public string TargetSceneName => m_SceneName;
            public ParamReference GetParamReference() => m_Parameter.GetGlobalParamReference();
        }

        [SerializeField] 
        internal AudioReference[] m_StartOnPlay = Array.Empty<AudioReference>();

        [Space]
        [SerializeField]
        internal SceneDependenceParam[] m_OnSceneLoadedParams = Array.Empty<SceneDependenceParam>();
    }
}
