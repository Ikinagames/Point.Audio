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
    [Serializable]
    public class PlayableAudioClip
    {
        [Serializable]
        public struct Sample
        {
            public int position;
            public float value;

            public Sample(int x, float y)
            {
                position = x;
                value = y;
            }
            public Sample(float x, float y)
            {
                position = Mathf.RoundToInt(x);
                value = y;
            }
        }

        [SerializeField] private AssetPathField<AudioClip> m_Clip;
        [SerializeField] private AudioClip m_BakedClip;

        [SerializeField] private Sample[] m_Volumes = Array.Empty<Sample>();
    }
}