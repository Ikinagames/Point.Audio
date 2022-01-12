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
using UnityEngine.Audio;

namespace Point.Audio
{
    public sealed class PointAudioSettings : StaticScriptableObject<PointAudioSettings>
    {
        [Tooltip("별도 프리팹이 설정되지 않은 AudioClip에 할당될 기본 프리팹입니다.")]
        [SerializeField] internal AudioSource m_DefaultAudioSourcePrefab;
        [Tooltip("별도 그룹이 설정되지 않은 AudioClip에 할당될 기본 그룹입니다.")]
        [SerializeField] internal AudioMixerGroup m_MasterGroup;
        [SerializeField] internal AudioList[] m_AudioLists = Array.Empty<AudioList>();
    }
}
