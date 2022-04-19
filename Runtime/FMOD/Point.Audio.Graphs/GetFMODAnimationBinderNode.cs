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

using GraphProcessor;
using Point.Collections.Graphs;
using System;
using UnityEngine;

namespace Point.Audio.Graphs
{
    [Serializable, NodeMenuItem("Logic/Unity/Get FMOD Animation Binder")]
    public sealed class GetFMODAnimationBinderNode : BaseNode
    {
        [Input("Link")]
        public ConditionalLink link;
        [Input("In")]
        public object input;

        [Output("Out")]
        public FMODAnimationBinder output = null;

        protected override void Process()
        {
            if (input != null && input is GameObject gameObject)
            {
                output = gameObject.GetComponentInChildren<FMODAnimationBinder>();
            }
        }
    }
}
