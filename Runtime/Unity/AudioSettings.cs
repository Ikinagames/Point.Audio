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

using Point.Collections;
using Point.Collections.Buffer.LowLevel;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

namespace Point.Audio
{
    [PreferBinarySerialization]
    public sealed class AudioSettings : StaticScriptableObject<AudioSettings>
    {
        [SerializeField]
        private AudioMixerGroup m_DefaultMixerGroup;
        [SerializeField] 
        private AudioList[] m_AudioLists = Array.Empty<AudioList>();

        public IReadOnlyList<AudioList> AudioLists => m_AudioLists;
        public AudioMixerGroup DefaultMixerGroup => m_DefaultMixerGroup;

        public bool HasAudioList(AudioList list) => m_AudioLists.Contains(list);
        public void RemoveAudioList(AudioList list)
        {
            int index = Array.IndexOf(m_AudioLists, list);
            if (index < 0) return;

            m_AudioLists.RemoveAtSwapBack(index);
            Array.Resize(ref m_AudioLists, m_AudioLists.Length - 1);
        }
        public void AddAudioList(AudioList list)
        {
            if (HasAudioList(list)) return;

            Array.Resize(ref m_AudioLists, m_AudioLists.Length + 1);
            m_AudioLists[m_AudioLists.Length - 1] = list;
        }

        public int CalculateEntryCount()
        {
            int count = 0;
            for (int i = 0; i < m_AudioLists.Length; i++)
            {
                count += m_AudioLists[i].Count;
            }
            return count;
        }
        public AudioList.Data FindData(AudioKey key)
        {
            for (int i = 0; i < m_AudioLists.Length; i++)
            {
                AudioList list = m_AudioLists[i];
                var result = list.Find(key);
                if (result == null) continue;

                return result;
            }

            return null;
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