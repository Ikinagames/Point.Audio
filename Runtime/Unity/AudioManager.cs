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
using Point.Collections.Buffer;
using Point.Collections.Events;
using Point.Collections.ResourceControl;
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Jobs;

namespace Point.Audio
{
    [AddComponentMenu("")]
    public sealed class AudioManager : StaticMonobehaviour<AudioManager>, IStaticInitializer
    {
        protected override bool EnableLog => base.EnableLog;

        [NonSerialized] private AudioSettings m_Settings;
        [NonSerialized] private int EntryCount;
        [NonSerialized] private Dictionary<Hash, Hash> m_FriendlyNameMap;
        [NonSerialized] private NativeHashMap<Hash, CompressedAudioData> m_DataHashMap;
        [NonSerialized] private Dictionary<Hash, ManagedAudioData> m_GroupMap;

        //
        [NonSerialized] AssetBundleInfo m_AudioBundle;
        [NonSerialized] ObjectPool<AudioSource> m_DefaultAudioPool;

        [NonSerialized] readonly Dictionary<Hash, AssetInfo> m_CachedAssetInfo = new Dictionary<Hash, AssetInfo>();
        [NonSerialized] readonly Dictionary<Hash, PrefabInfo> m_CachedPrefabInfo = new Dictionary<Hash, PrefabInfo>();

        private TransformAccessArray m_PlayedAudioTransforms;

        #region Initialize

        protected override void OnInitialize()
        {
            m_Settings = AudioSettings.Instance;

            EntryCount = m_Settings.CalculateEntryCount();
            m_FriendlyNameMap = new Dictionary<Hash, Hash>();
            m_DataHashMap = new NativeHashMap<Hash, CompressedAudioData>(EntryCount, AllocatorManager.Persistent);
            m_GroupMap = new Dictionary<Hash, ManagedAudioData>();

            m_Settings.RegisterFriendlyNames(m_FriendlyNameMap);
            foreach (var data in m_Settings.GetAudioData())
            {
                var temp = data.GetAudioData();

                m_DataHashMap.Add(temp.AudioKey, temp);
                m_GroupMap.Add(temp.AudioKey, 
                    new ManagedAudioData
                    {
                        audioMixerGroup = data.GetAudioMixerGroup(),
                        onPlayConstAction = data.GetOnPlayConstAction(),

                        childs = data.GetChilds(),
                        playOption = data.GetPlayOption(),
                    });
            }

            m_DefaultAudioPool = new ObjectPool<AudioSource>(
                DefaultAudioFactory,
                DefaultAudioOnGet,
                DefaultAudioOnReserve,
                null
                );

            EventBroadcaster.AddEvent<PlayAudioEvent>(PlayAudioEventHandler);

            m_PlayedAudioTransforms = new TransformAccessArray(1024);
            
        }
        protected override void OnShutdown()
        {
            EventBroadcaster.RemoveEvent<PlayAudioEvent>(PlayAudioEventHandler);
            m_DataHashMap.Dispose();

            if (m_AudioBundle.IsValid())
            {
                m_AudioBundle.Unload(true);
            }
        }

        #endregion

        public static void Initialize(AssetBundle audioBundle)
        {
            Instance.m_AudioBundle = ResourceManager.RegisterAssetBundle(audioBundle);
#if DEBUG_MODE
            foreach (var item in Instance.m_DataHashMap)
            {
                if (!Instance.m_AudioBundle.HasAsset(item.Key))
                {
                    $"?? {item.Value.AudioKey} does not exist from assetbundle {audioBundle.name}".ToLogError();
                }
            }
#endif
        }

        #region Default Pool

        private static AudioSource DefaultAudioFactory()
        {
            GameObject obj = new GameObject();
            AudioSource source = obj.AddComponent<AudioSource>();
            source.playOnAwake = false;

            return source;
        }
        private static void DefaultAudioOnGet(AudioSource t)
        {
            t.volume = 1;
            t.pitch = 1;
            t.outputAudioMixerGroup = AudioSettings.Instance.DefaultMixerGroup;
        }
        private static void DefaultAudioOnReserve(AudioSource t)
        {
            t.volume = 0;
            t.Stop();
        }

        #endregion

        #region Event Handlers

        private void PlayAudioEventHandler(PlayAudioEvent ev)
        {
            Play(ev.Key);
        }

        #endregion

        #region Internal

        private static AssetInfo LoadAsset(Hash key)
        {
            if (!Instance.m_AudioBundle.TryLoadAsset(key, out var asset))
            {

            }
            return asset;
        }
        private static AudioSource GetAudioSource(Hash prefabKey)
        {
            if (!Instance.m_AudioBundle.IsValid() || prefabKey.IsEmpty())
            {
                return Instance.m_DefaultAudioPool.Get();
            }

            if (!Instance.m_CachedAssetInfo.TryGetValue(prefabKey, out AssetInfo prefabAsset))
            {
                if (Instance.m_AudioBundle.TryLoadAsset(prefabKey, out prefabAsset))
                {
                    Instance.m_CachedAssetInfo.Add(prefabKey, prefabAsset);
                }
                // 프리팹이 없다?
                else
                {
                    throw new NotImplementedException();
                }
            }

            if (!Instance.m_CachedPrefabInfo.TryGetValue(prefabKey, out var info))
            {
                info = new PrefabInfo(prefabAsset);
                Instance.m_CachedPrefabInfo.Add(prefabKey, info);
            }

            return info.pool.Get();
        }
        private static AudioClip GetAudioClip(Hash audioKey)
        {
            ManagedAudioData managedData; int index;
            AudioManager ins = Instance;

            //////////////////////////////////////////////////////////////////////////////////////////
            /*                                   Critical Section                                   */
            //////////////////////////////////////////////////////////////////////////////////////////
            if (audioKey.IsEmpty())
            {
                return null;
            }
            else if (!ins.m_AudioBundle.IsValid())
            {
#if UNITY_EDITOR
                Hash targetKey;
                if (!Instance.m_FriendlyNameMap.TryGetValue(audioKey, out targetKey))
                {
                    targetKey = audioKey;
                }
                $"Audio AssetBundle is not loaded. This is not allowed. Please register AssetBundle with AudioManager.Initialize(AssetBundle)\nThis request({targetKey}) will be accepted only in Editor with {nameof(UnityEditor.AssetDatabase)}.".ToLogError(Channel.Audio);

                if (!ins.m_GroupMap.TryGetValue(targetKey, out managedData) ||
                    managedData.childs.Length == 0)
                {
                    return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(targetKey.Key);
                }

                index = managedData.GetIndex();
                if (index == 0) return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(targetKey.Key);

                return GetAudioClip(new Hash(managedData.childs[index - 1].AssetPath));
#else
                throw new Exception("Audio AssetBundle is not loaded. This is not allowed.");
#endif
            }
            //////////////////////////////////////////////////////////////////////////////////////////
            /*                                                                                      */
            //////////////////////////////////////////////////////////////////////////////////////////

            if (!ins.m_CachedAssetInfo.TryGetValue(audioKey, out AssetInfo clipAsset))
            {
                clipAsset = ins.m_AudioBundle.LoadAsset(audioKey);
                ins.m_CachedAssetInfo.Add(audioKey, clipAsset);
            }

            if (!ins.m_GroupMap.TryGetValue(audioKey, out managedData) ||
                managedData.childs.Length == 0)
            {
                return clipAsset.Asset as AudioClip;
            }

            index = managedData.GetIndex();
            if (index == 0) return clipAsset.Asset as AudioClip;

            return GetAudioClip(new Hash(managedData.childs[index - 1].AssetPath));
        }
        private static bool TryGetCompressedAudioData(AudioKey key, out CompressedAudioData data)
        {
            if (Instance.m_DataHashMap.TryGetValue(key, out data))
            {
                return true;
            }
            
            if (!Instance.m_FriendlyNameMap.TryGetValue(key, out Hash temp) ||
                !Instance.m_DataHashMap.TryGetValue(temp, out data))
            {
                return false;
            }
            return true;
        }

        #endregion

        #region Play

        private static AudioSource InternalPlay(AudioKey audioKey)
        {
            AudioClip clip = GetAudioClip(audioKey);
            if (clip == null)
            {
                $"Could\'nt find audio clip {audioKey}.".ToLogError();
                return null;
            }

            AudioSource insAudio;
            
            /// <see cref="AudioList"/> 에 세부 정보가 등록되지 않은 오디오 클립
            if (!TryGetCompressedAudioData(audioKey, out CompressedAudioData data))
            {
                insAudio = Instance.m_DefaultAudioPool.Get();
                insAudio.outputAudioMixerGroup = AudioSettings.Instance.DefaultMixerGroup;
                insAudio.clip = clip;

                return insAudio;
            }

            ManagedAudioData managedData = Instance.m_GroupMap[data.AudioKey];
            insAudio = GetAudioSource(data.PrefabKey);
            //////////////////////////////////////////////////////////////////////////////////////////
            /*                                                                                      */
            //////////////////////////////////////////////////////////////////////////////////////////
            insAudio.outputAudioMixerGroup = managedData.audioMixerGroup;
            insAudio.clip = clip;

            insAudio.volume = data.GetVolume();
            insAudio.pitch = data.GetPitch();
            //////////////////////////////////////////////////////////////////////////////////////////
            /*                                                                                      */
            //////////////////////////////////////////////////////////////////////////////////////////
            for (int i = 0; i < managedData.onPlayConstAction.Count; i++)
            {
                managedData.onPlayConstAction[i].Execute(insAudio);
            }

            return insAudio;
        }
        public static void Play(AudioKey audioKey)
        {
            AudioSource insAudio = InternalPlay(audioKey);
            if (insAudio == null)
            {
                return;
            }

            insAudio.Play();
#if UNITY_EDITOR
            $"Playing AudioClip:{insAudio.clip.name} | Key:{audioKey}".ToLog(Channel.Audio);
#endif
        }
        public static void Play(AudioKey audioKey, Vector3 position)
        {
            AudioSource insAudio = InternalPlay(audioKey);
            if (insAudio == null)
            {
                return;
            }

            insAudio.transform.position = position;
            insAudio.Play();
#if UNITY_EDITOR
            $"Playing {audioKey}".ToLog(Channel.Audio);
#endif
        }

        #endregion

        #region Inner Classes

        private sealed class ManagedAudioData
        {
            public AudioMixerGroup audioMixerGroup;
            public IReadOnlyList<ConstActionReference> onPlayConstAction;

            private int currentIndex = 0;
            public AssetPathField<AudioClip>[] childs;
            public AudioPlayOption playOption;

            public int GetIndex()
            {
                if (playOption == AudioPlayOption.Sequential)
                {
                    currentIndex++;
                    if (currentIndex > childs.Length)
                    {
                        currentIndex = 0;
                    }

                    return currentIndex;
                }

                currentIndex = UnityEngine.Random.Range(0, childs.Length + 1);
                return currentIndex;
            }
        }
        private sealed class PrefabInfo
        {
            public AssetInfo prefab;
            public ObjectPool<AudioSource> pool;

            public PrefabInfo(AssetInfo prefab)
            {
                this.prefab = prefab;
                pool = new ObjectPool<AudioSource>(
                    Factory,
                    OnGet,
                    OnReserve,
                    null
                    );
            }

            private AudioSource Factory()
            {
                AudioSource prefabAudio = prefab.Asset as AudioSource;

                GameObject ins = Instantiate(prefabAudio.gameObject);
                AudioSource insAudio = ins.GetComponent<AudioSource>();
                insAudio.playOnAwake = false;

                return insAudio;
            }
            private static void OnGet(AudioSource t)
            {

            }
            private static void OnReserve(AudioSource t)
            {
                t.Stop();
            }
        }

        #endregion
    }

    [Obsolete("", true)]
    public static class AudioTestUsages
    {
        [Obsolete("", true)]
        public static void Test()
        {
            // 에셋 번들 등록
            AudioManager.Initialize(null);

            // 오디오 재생
            AudioManager.Play(
                ///<see cref="AudioList.FriendlyName"/>
                "Soundtrack01"
                ///<see cref="AudioList.Data"/>
                //"Assets/test/test.wav"
                );
            EventBroadcaster.PostEvent<PlayAudioEvent>(PlayAudioEvent.GetEvent("Soundtrack01"));
        }
    }

    public struct Audio : IDisposable
    {
        private readonly int m_Index;

        public void Dispose()
        {
        }
    }
}