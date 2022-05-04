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

using Point.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Point.Audio
{
    public sealed class AudioSettings : StaticScriptableObject<AudioSettings>
    {
        [SerializeField]
        private AudioMixerGroup m_DefaultMixerGroup;
        [SerializeField] 
        private AudioList[] m_AudioLists = Array.Empty<AudioList>();

        public AudioMixerGroup DefaultMixerGroup => m_DefaultMixerGroup;

        public int CalculateEntryCount()
        {
            int count = 0;
            for (int i = 0; i < m_AudioLists.Length; i++)
            {
                count += m_AudioLists[i].Count;
            }
            return count;
        }

        public void RegisterFriendlyNames(Dictionary<Hash, Hash> map)
        {
            for (int i = 0; i < m_AudioLists.Length; i++)
            {
                m_AudioLists[i].RegisterFriendlyNames(map);
            }
        }
        public IEnumerable<AudioList.Data> GetAudioData()
        {
            for (int i = 0; i < m_AudioLists.Length; i++)
            {
                foreach (var item in m_AudioLists[i].GetAudioData())
                {
                    yield return item;
                }
            }
        }
    }
}