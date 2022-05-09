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

using Point.Collections;
using Point.Collections.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

namespace Point.Audio
{
    [CreateAssetMenu(menuName = "Point/Audio/Create Audio List")]
    public sealed class AudioList : PointScriptableObject
    {
        [Serializable]
        public sealed class FriendlyName
        {
            [SerializeField]
            private string m_FriendlyName;
            [SerializeField]
            private AssetPathField<AudioClip> m_AudioClip;

            public void Register(Dictionary<Hash, Hash> map)
            {
                map.Add(new Hash(m_FriendlyName), new Hash(m_AudioClip.AssetPath));
            }
        }
        [Serializable]
        public sealed class Data
        {
            [SerializeField]
            private AssetPathField<AudioClip> m_AudioClip;

            [Space, Header("Default")]
            [SerializeField]
            private AssetPathField<AudioSource> m_Prefab;
            [SerializeField]
            private AudioMixerGroup m_Group;

            [Space, Header("Options")]
            [SerializeField]
            private float m_IgnoreTime = 0.2f;

            [Space]
            [SerializeField]
            [Tooltip(
                "arg0 = AudioSource")]
            private ConstActionReference[] m_OnPlayConstAction = ArrayWrapper<ConstActionReference>.Empty;
            [SerializeField]
            private AssetPathField<AudioClip>[] m_Childs = Array.Empty<AssetPathField<AudioClip>>();
            [SerializeField]
            private AudioPlayOption m_PlayOption = AudioPlayOption.Sequential;

            [Space]
            [SerializeField, Decibel]
            private float m_MasterVolume = 1;
            [SerializeField, Decibel]
            private MinMaxFloatField m_Volume = new MinMaxFloatField(1);
            [SerializeField]
            private MinMaxFloatField m_Pitch = new MinMaxFloatField(1);

            public AudioMixerGroup GetAudioMixerGroup() => m_Group;
            public IReadOnlyList<ConstActionReference> GetOnPlayConstAction() => m_OnPlayConstAction;
            public AssetPathField<AudioClip>[] GetChilds() => m_Childs;
            public AudioPlayOption GetPlayOption() => m_PlayOption;

            public CompressedAudioData GetAudioData() => new CompressedAudioData(
                m_AudioClip, m_Prefab, m_IgnoreTime, m_MasterVolume,
                m_Volume.Min, m_Volume.Max, m_Pitch.Min, m_Pitch.Max);
        }

        [SerializeField]
        private FriendlyName[] m_FriendlyNames = Array.Empty<FriendlyName>();

        [SerializeField]
        private Data[] m_Data = Array.Empty<Data>();

        public int Count => m_Data.Length;

        public IEnumerable<Data> GetAudioData() => m_Data;
        public void RegisterFriendlyNames(Dictionary<Hash, Hash> map)
        {
            for (int i = 0; i < m_FriendlyNames.Length; i++)
            {
                m_FriendlyNames[i].Register(map);
            }
        }
    }
}