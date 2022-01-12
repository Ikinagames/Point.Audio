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
using System.IO;
using UnityEngine;
using UnityEngine.Audio;

namespace Point.Audio
{
    [DisallowMultipleComponent]
    [CreateAssetMenu(menuName = "AudioList")]
    public sealed class AudioList : ScriptableObject
    {
        [Flags]
        public enum AudioOptions
        {
            /// <summary>
            /// 순차 재생을 합니다.
            /// </summary>
            Sequence,
            /// <summary>
            /// 무작위 재생을 합니다.
            /// </summary>
            Random
        }
        /// <summary>
        /// 각종 오디오에 필요한 정보를 담는 오브젝트입니다.
        /// </summary>
        [Serializable]
        public sealed class AudioSetting
        {
            [SerializeField] private AudioClipPathField m_AudioPath;
            [Tooltip("AudioClip 을 출력시킬 그룹입니다.")]
            [SerializeField] private AudioMixerGroup m_Group;
            [Tooltip("AudioClip을 재생할 프리팹입니다.")]
            [SerializeField] private AudioSource m_AudioSourcePrefab;
            [Tooltip("AudioClip 의 기본 볼륨입니다.")]
            [SerializeField] private float m_Volume = 1;

            [Space]
            [Range(-3, 3)]
            [SerializeField] public float m_MinPitch = 1;
            [Range(-3, 3)]
            [SerializeField] public float m_MaxPitch = 1;

            [Space]
            [Tooltip("AudioClip 이 동시에 최대로 재생될 수 있는 갯수 입니다.")]
            [SerializeField] private int m_MaximumPlayCount = 32;
            [Tooltip("Variations 에 하나 이상의 AudioClip 을 포함한다면, 어떻게 재생할 것인지에 대한 설정입니다.")]
            [SerializeField] private AudioOptions m_AudioOptions = AudioOptions.Sequence;
            [SerializeField] private AudioClipPathField[] m_Variations = Array.Empty<AudioClipPathField>();

            [NonSerialized] private AudioKey[] m_Keys = null;
            [NonSerialized] private int m_CurrentIndex = 0;

#if UNITY_EDITOR
            /// <summary>
            /// Editor only
            /// </summary>
            /// <remarks>
            /// Runtime 에서 빌드되지 않습니다.
            /// </remarks>
            public AudioClip EditorAudioClip
            {
                get => m_AudioPath.EditorAsset;
                set => m_AudioPath.EditorAsset = value;
            }
            /// <summary>
            /// Editor only
            /// </summary>
            /// <remarks>
            /// Runtime 에서 빌드되지 않습니다.
            /// </remarks>
            public AudioClipPathField[] EditorVariations
            {
                get => m_Variations;
                set => m_Variations = value;
            }
#endif

            public AudioOptions Options
            {
                get => m_AudioOptions;
                set => m_AudioOptions = value;
            }
            public int CurrentIndex => m_CurrentIndex;
            public AudioKey[] Keys
            {
                get
                {
                    if (m_Keys == null)
                    {
                        m_Keys = new AudioKey[m_Variations.Length + 1];
                        m_Keys[0] = (AudioKey)(Path.GetFileNameWithoutExtension(m_AudioPath.AssetPath));

                        for (int i = 0; i < m_Variations.Length; i++)
                        {
                            m_Keys[i + 1] = (AudioKey)(Path.GetFileNameWithoutExtension(m_Variations[i].AssetPath));
                        }
                    }

                    return m_Keys;
                }
            }

            public string AudioClipPath
            {
                get => m_AudioPath.AssetPath;
                set => m_AudioPath.AssetPath = value;
            }
            public AudioKey Key
            {
                get
                {
                    if (m_Keys == null)
                    {
                        m_Keys = new AudioKey[m_Variations.Length + 1];
                        m_Keys[0] = (AudioKey)(Path.GetFileNameWithoutExtension(m_AudioPath.AssetPath));

                        for (int i = 0; i < m_Variations.Length; i++)
                        {
                            m_Keys[i + 1] = (AudioKey)(Path.GetFileNameWithoutExtension(m_Variations[i].AssetPath));
                        }
                    }

                    return m_Keys[0];
                }
            }
            public AudioMixerGroup Group
            {
                get => m_Group;
                set => m_Group = value;
            }
            public AudioSource Prefab
            {
                get => m_AudioSourcePrefab;
                set => m_AudioSourcePrefab = value;
            }

            public float Volume
            {
                get => m_Volume;
                set => m_Volume = value;
            }
            public float Pitch
            {
                get => UnityEngine.Random.Range(m_MinPitch, m_MaxPitch);
            }
            public int MaximumPlayCount
            {
                get => m_MaximumPlayCount;
                set => m_MaximumPlayCount = value;
            }

            public AudioSetting(string clipPath, AudioMixerGroup group, AudioSource prefab)
            {
                m_AudioPath = new AudioClipPathField(clipPath);
                m_Group = group;
                m_AudioSourcePrefab = prefab;

                m_Volume = 1;
                m_MaximumPlayCount = 32;
            }

            public void IncrementIndex()
            {
                if (m_Variations.Length == 0) return;

                if (m_AudioOptions == AudioOptions.Random)
                {
                    int next = UnityEngine.Random.Range(0, m_Variations.Length + 1);
                    if (next != m_CurrentIndex)
                    {
                        m_CurrentIndex = next;
                        return;
                    }

                    return;
                }

                m_CurrentIndex++;
                if (m_CurrentIndex >= Keys.Length)
                {
                    m_CurrentIndex = 0;
                }
            }
        }

        [Tooltip("별도 프리팹이 설정되지 않은 AudioClip에 할당될 기본 프리팹입니다.")]
        [SerializeField] private AudioSource m_DefaultAudioSourcePrefab;
        [Tooltip("별도 그룹이 설정되지 않은 AudioClip에 할당될 기본 그룹입니다.")]
        [SerializeField] private AudioMixerGroup m_MasterGroup;
        [SerializeField] private AudioSetting[] m_AudioSettings = Array.Empty<AudioSetting>();

        private readonly Dictionary<AudioKey, AudioSetting>
            m_AudioSettingHashMap = new Dictionary<AudioKey, AudioSetting>();

        public void Initialize()
        {
            m_AudioSettingHashMap.Clear();

            for (int i = 0; i < m_AudioSettings.Length; i++)
            {
#if UNITY_EDITOR
                if (m_AudioSettingHashMap.ContainsKey(m_AudioSettings[i].Key))
                {
                    PointHelper.LogError(Channel.Audio,
                        $"{m_AudioSettings[i].EditorAudioClip.name} is duplicated.");

                    continue;
                }
#endif
                m_AudioSettingHashMap.Add(m_AudioSettings[i].Key, m_AudioSettings[i]);

                PointHelper.Log(Channel.Audio,
                    $"Audio list has been initialized.");
            }
        }

        public AudioSetting GetAudioSetting(in string key) => GetAudioSetting((AudioKey)key);
        public AudioSetting GetAudioSetting(in AudioKey key)
        {
#if UNITY_EDITOR
            if (!m_AudioSettingHashMap.ContainsKey(key))
            {
                PointHelper.LogError(Channel.Audio,
                    $"You are trying to get an invalid audio setting(clipHash: \"{key}\"). This is not allowed.");

                return null;
            }
#endif
            AudioSetting setting = m_AudioSettingHashMap[key];
            setting.IncrementIndex();

            if (setting.CurrentIndex > 0 && setting.Keys.Length > 1)
            {
                AudioKey newKey = setting.Keys[setting.CurrentIndex];
                setting = m_AudioSettingHashMap[newKey];
            }

            return setting;
        }
    }
}
