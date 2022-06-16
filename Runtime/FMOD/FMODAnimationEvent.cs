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

#if ENABLE_NODEGRAPH
using Point.Audio.Graphs;
#endif

using Point.Collections.Graphs;
using System;
using UnityEngine;

namespace Point.Audio
{
    [Serializable]
    public sealed class FMODAnimationEvent : ICloneable
    {
        [Tooltip("애니메이션의 키")]
        [SerializeField] private string m_Name;
        [SerializeField] private FMODEventReference m_AudioReference;

#if ENABLE_NODEGRAPH
        [Space]
        [SerializeField] private VisualLogicGraph m_VisualGraph;
        [NonSerialized] internal VisualGraphLogicProcessor m_Processor = null;
#endif

        public string Name => m_Name;
        public FMODEventReference AudioReference => m_AudioReference;
#if ENABLE_NODEGRAPH
        public VisualLogicGraph VisualGraph => m_VisualGraph;
#endif

        public void Initialize()
        {
#if ENABLE_NODEGRAPH
            if (m_Processor == null && m_VisualGraph != null)
            {
                m_Processor = new VisualGraphLogicProcessor(m_VisualGraph);
            }
#endif
        }

        public object Clone()
        {
            FMODAnimationEvent ev = (FMODAnimationEvent)MemberwiseClone();

            ev.m_Name = string.Copy(m_Name);
            ev.m_AudioReference = (FMODEventReference)m_AudioReference.Clone();

#if ENABLE_NODEGRAPH
            if (m_VisualGraph != null)
            {
                ev.m_VisualGraph = UnityEngine.Object.Instantiate(m_VisualGraph);
                ev.m_Processor = new VisualGraphLogicProcessor(ev.m_VisualGraph);
            }
#endif
            return ev;
        }
    }
}
