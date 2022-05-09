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
using Point.Collections.Buffer.LowLevel;
using Point.Collections.Events;
using Point.Collections.ResourceControl;
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Jobs;

namespace Point.Audio
{
    [AddComponentMenu("")]
    public sealed class AudioManager : StaticMonobehaviour<AudioManager>, IStaticInitializer
    {
        protected override bool EnableLog => false;
        protected override bool HideInInspector => false;

        [NonSerialized] private AudioSettings m_Settings;
        [NonSerialized] private int EntryCount;
        [NonSerialized] private Dictionary<Hash, Hash> m_FriendlyNameMap;
        [NonSerialized] private NativeHashMap<AudioKey, CompressedAudioData> m_DataHashMap;
        [NonSerialized] private Dictionary<AudioKey, ManagedAudioData> m_GroupMap;

        //
        [NonSerialized] AssetBundleInfo m_AudioBundle;
        [NonSerialized] ObjectPool<AudioSource> m_DefaultAudioPool;

        [NonSerialized] readonly Dictionary<Hash, AssetInfo> m_CachedAssetInfo = new Dictionary<Hash, AssetInfo>();
        [NonSerialized] readonly Dictionary<Hash, PrefabInfo> m_CachedPrefabInfo = new Dictionary<Hash, PrefabInfo>();

        private InternalAudioContainer m_AudioContainer;

        private sealed class InternalAudioContainer : IDisposable
        {
            private AudioSource[] m_Audio;
            private Transform[] m_AudioTransforms;
            private TransformAccessArray m_AudioTransformAccess;
            private UnsafeAllocator<Transformation> transformations;
            private int m_Count;

            private JobHandle m_JobHandle;

            public InternalAudioContainer(int capacity)
            {
                m_Audio = new AudioSource[capacity];
                m_AudioTransforms = new Transform[capacity];
                m_AudioTransformAccess = new TransformAccessArray(capacity);
                transformations = new UnsafeAllocator<Transformation>(capacity, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            }
            public void Dispose()
            {
                m_JobHandle.Complete();

                m_Audio = null;
                m_AudioTransforms = null;
                m_AudioTransformAccess.Dispose();
                transformations.Dispose();
            }

            public AudioSource GetAudioSource(in Audio audio)
            {
                if (!audio.IsValid()) return null;

                return m_Audio[audio.m_Index];
            }
            public Audio GetAudio(AudioSource audioSource, in AudioKey audioKey)
            {
                int index = Array.IndexOf(m_Audio, audioSource);
                if (index < 0)
                {
                    throw new Exception();
                }

                return new Audio(audioKey, index, audioSource.GetInstanceID(), transformations);
            }
            public Audio GetEmptyAudio(in AudioKey audioKey)
            {
                return new Audio(audioKey, transformations);
            }

            public void Register(AudioSource audioSource)
            {
                m_JobHandle.Complete();

                if (m_Count >= m_Audio.Length)
                {
                    int targetLength = m_Audio.Length * 2;
                    Array.Resize(ref m_Audio, targetLength);
                    Array.Resize(ref m_AudioTransforms, targetLength);
                    transformations.Resize(targetLength, NativeArrayOptions.ClearMemory);
                }

                int index = m_Count;
                m_Audio[index] = audioSource;
                m_AudioTransforms[index] = audioSource.transform;
                transformations[index] = new Transformation();

                m_AudioTransformAccess.SetTransforms(m_AudioTransforms);
                m_Count++;

                //return new Audio(index, audioSource.GetInstanceID(), translations);
            }
            //public void Remove(AudioSource audioSource)
            //{
            //    m_JobHandle.Complete();

            //    int index = Array.IndexOf(m_Audio, audioSource);
            //    if (index < 0) return;

            //    UnsafeBufferUtility.RemoveAtSwapBack(m_Audio, index);
            //    UnsafeBufferUtility.RemoveAtSwapBack(m_AudioTransforms, index);

            //    m_AudioTransformAccess.SetTransforms(m_AudioTransforms);
            //    m_Count--;
            //}

            public void Update()
            {
                UpdateJob updateJob = new UpdateJob()
                {
                    transformations = transformations
                };

                m_JobHandle = JobHandle.CombineDependencies(
                    m_JobHandle,
                    updateJob.Schedule(m_AudioTransformAccess, m_JobHandle)
                    );

                JobHandle.ScheduleBatchedJobs();
            }

            [BurstCompile]
            private struct UpdateJob : IJobParallelForTransform
            {
                public UnsafeAllocator<Transformation> transformations;

                public void Execute(int i, TransformAccess transform)
                {
                    transform.localPosition = transformations[i].localPosition;
                    transform.localRotation = transformations[i].localRotation;
                    transform.localScale = transformations[i].localScale;
                }
            }
        }

        #region Initialize

        protected override void OnInitialize()
        {
            m_Settings = AudioSettings.Instance;

            EntryCount = m_Settings.CalculateEntryCount();
            m_FriendlyNameMap = new Dictionary<Hash, Hash>();
            m_DataHashMap = new NativeHashMap<AudioKey, CompressedAudioData>(EntryCount, AllocatorManager.Persistent);
            m_GroupMap = new Dictionary<AudioKey, ManagedAudioData>();

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

            m_AudioContainer = new InternalAudioContainer(1024);
        }
        protected override void OnShutdown()
        {
            m_AudioContainer.Dispose();
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

#if DEBUG_MODE
        private static int s_AudioSourceCounter = 0;
#endif
        private static AudioSource DefaultAudioFactory()
        {
            GameObject obj = new GameObject();
#if DEBUG_MODE
            obj.name = $"Default AudioSource {s_AudioSourceCounter++}";
#endif
            obj.transform.SetParent(Instance.transform);
            AudioSource source = obj.AddComponent<AudioSource>();
            source.playOnAwake = false;

            Instance.m_AudioContainer.Register(source);

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

            t.clip = null;
        }

        #endregion

        #region Monobehaviour Messages

        private void LateUpdate()
        {
            #region Play Checks

#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPaused)
#endif
            {
                for (int i = m_PlayedAudios.Count - 1; i >= 0; i--)
                {
                    Audio audio = m_PlayedAudios[i];
                    AudioSource audioSource = GetAudioSource(audio);

                    if (audioSource.isPlaying) continue;

                    RemoveFromPlaylist(in i, in audio);
                }

                m_AudioContainer.Update();
            }
            
            #endregion
        }

        #endregion

        #region Event Handlers

        private void PlayAudioEventHandler(PlayAudioEvent ev)
        {
            Play(ev.Key).AutoDisposal();
        }

        #endregion

        public static Audio GetAudio(in AudioKey audioKey)
        {
            //GetAudio(in audioKey, out Audio audio, out _, out _);
            return Instance.m_AudioContainer.GetEmptyAudio(in audioKey);
        }
        public static bool IsPlaying(in Audio audio)
        {
            if (!audio.IsValid())
            {
                PointHelper.LogError(Channel.Audio,
                    $"This audio is invalid.");

                return false;
            }

            //return Instance.m_PlayedAudioHashSet.Contains(audio);
            AudioSource audioSource = GetAudioSource(in audio);
            return audioSource.isPlaying;
        }

        #region Internal

        private AudioSource CreateAudioSource(Hash prefabKey)
        {
            if (!m_AudioBundle.IsValid() || prefabKey.IsEmpty())
            {
                return m_DefaultAudioPool.Get();
            }

            if (!m_CachedAssetInfo.TryGetValue(prefabKey, out AssetInfo prefabAsset))
            {
                if (m_AudioBundle.TryLoadAsset(prefabKey, out prefabAsset))
                {
                    m_CachedAssetInfo.Add(prefabKey, prefabAsset);
                }
                // �������� ����?
                else
                {
                    throw new NotImplementedException();
                }
            }

            if (!m_CachedPrefabInfo.TryGetValue(prefabKey, out var info))
            {
                info = new PrefabInfo(prefabAsset);
                m_CachedPrefabInfo.Add(prefabKey, info);
            }

            return info.pool.Get();
        }
        internal static AudioSource GetAudioSource(in Audio audio)
        {
            return Instance.m_AudioContainer.GetAudioSource(in audio);
        }

        private static AudioKey GetConcreteKey(in AudioKey audioKey)
        {
            if (Instance.m_FriendlyNameMap.TryGetValue(audioKey, out var key))
            {
                return key;
            }
            return audioKey;
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
        private static bool TryGetCompressedAudioData(in AudioKey key, out CompressedAudioData data)
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
        private static ObjectPool<AudioSource> GetPool(in AudioKey audioKey)
        {
            /// <see cref="AudioList"/> �� ���� ������ ��ϵ��� ���� ����� Ŭ��
            if (!TryGetCompressedAudioData(audioKey, out CompressedAudioData data))
            {
                return Instance.m_DefaultAudioPool;
            }

            AssetBundleInfo audioBundle = Instance.m_AudioBundle;
            Hash prefabKey = data.PrefabKey;

            if (!audioBundle.IsValid() || prefabKey.IsEmpty())
            {
                return Instance.m_DefaultAudioPool;
            }

            Dictionary<Hash, AssetInfo> cachedAssetInfo = Instance.m_CachedAssetInfo;
            if (!cachedAssetInfo.TryGetValue(prefabKey, out AssetInfo prefabAsset))
            {
                if (audioBundle.TryLoadAsset(prefabKey, out prefabAsset))
                {
                    cachedAssetInfo.Add(prefabKey, prefabAsset);
                }
                // �������� ����?
                else
                {
                    throw new NotImplementedException();
                }
            }

            Dictionary<Hash, PrefabInfo> cachedPrefabInfo = Instance.m_CachedPrefabInfo;
            if (!cachedPrefabInfo.TryGetValue(prefabKey, out var info))
            {
                info = new PrefabInfo(prefabAsset);
                cachedPrefabInfo.Add(prefabKey, info);
            }

            return info.pool;
        }

        private static AudioSource IssueAudioSource(in AudioKey audioKey,
            out CompressedAudioData data, out ManagedAudioData managedData)
        {
            AudioClip clip = GetAudioClip(audioKey);
            if (clip == null)
            {
                $"Could\'nt find audio clip {audioKey}.".ToLogError();

                data = default(CompressedAudioData);
                managedData = null;
                return null;
            }

            AudioSource insAudio;

            /// <see cref="AudioList"/> �� ���� ������ ��ϵ��� ���� ����� Ŭ��
            if (!TryGetCompressedAudioData(audioKey, out data))
            {
                insAudio = Instance.m_DefaultAudioPool.Get();
                insAudio.outputAudioMixerGroup = AudioSettings.Instance.DefaultMixerGroup;
                insAudio.clip = clip;

                data = default(CompressedAudioData);
                managedData = null;
                return insAudio;
            }

            managedData = Instance.m_GroupMap[data.AudioKey];
            insAudio = Instance.CreateAudioSource(data.PrefabKey);
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

            return insAudio;
        }
        private static AudioSource GetAudio(in AudioKey audioKey, out Audio audio, 
            out CompressedAudioData data, out ManagedAudioData managedData)
        {
            audio = default(Audio);
            AudioSource insAudio = IssueAudioSource(in audioKey, out data, out managedData);
            if (insAudio != null)
            {
                audio = Instance.m_AudioContainer.GetAudio(insAudio, audioKey);
            }
            
            return insAudio;
        }

        internal static void StopAudio(in Audio audio)
        {
            AudioSource audioSource = GetAudioSource(audio);
            if (audioSource == null)
            {
                "??".ToLogError();
                return;
            }
            audioSource.Stop();
        }
        internal static void ReserveAudio(ref Audio audio)
        {
            ObjectPool<AudioSource> pool = GetPool(in audio.m_AudioKey);
            AudioSource audioSource = GetAudioSource(audio);

            pool.Reserve(audioSource);
        }

        #endregion

        #region Process

        private static bool ProcessIsPlayable(in Audio audio)
        {
            AudioKey audioKey = GetConcreteKey(in audio.m_AudioKey);

            if (!Instance.m_GroupMap.TryGetValue(audioKey, out ManagedAudioData managedData) ||
                !Instance.m_DataHashMap.TryGetValue(audioKey, out var data))
            {
                return true;
            }

            if (!managedData.lastPlayedTime.IsExceeded(data.IgnoreTime)) return false;

            return true;
        }
        private static void ProcessOnPlay(in Audio audio, AudioSource audioSource)
        {
            AudioKey audioKey = GetConcreteKey(in audio.m_AudioKey);

            if (Instance.m_GroupMap.TryGetValue(audioKey, out ManagedAudioData managedData))
            {
                for (int i = 0; i < managedData.onPlayConstAction.Count; i++)
                {
                    managedData.onPlayConstAction[i].Execute(audioSource);
                }

                managedData.lastPlayedTime = Timer.Start();
            }
            
            AddToPlaylist(audio);
        }

        #endregion

        #region Play

        private readonly List<Audio> m_PlayedAudios = new List<Audio>();
        private readonly HashSet<Audio> m_PlayedAudioHashSet = new HashSet<Audio>();

        private static void AddToPlaylist(in Audio audio)
        {
            if (!audio.IsValid())
            {
                $"Cannot add Audio({audio}) to playlist it\'s invalid.".ToLogError();
                return;
            }

            Instance.m_PlayedAudios.Add(audio);
            Instance.m_PlayedAudioHashSet.Add(audio);
        }
        private static void RemoveFromPlaylist(in int index, in Audio audio)
        {
            Instance.m_PlayedAudios.RemoveAt(index);
            Instance.m_PlayedAudioHashSet.Remove(audio);
        }

        internal static void PlayAudio(ref Audio audio)
        {
            AudioSource audioSource;
            if (audio.RequireSetup())
            {
                if (!audio.IsValid())
                {
                    "not valid".ToLogError();
                    return;
                }

                AudioKey key = audio.m_AudioKey;
                audio = Play(key);
                return;
            }
            else
            {
                if (!ProcessIsPlayable(in audio))
                {
                    //"ignored".ToLog();
                    return;
                }

                audioSource = GetAudioSource(audio);
                ProcessOnPlay(in audio, audioSource);
            }

            audioSource.Play();
        }
        private static AudioSource InternalPlay(in AudioKey audioKey, out Audio audio)
        {
            AudioSource insAudio = GetAudio(in audioKey, out audio, out _, out _);
            if (insAudio == null) return null;
            else if (!ProcessIsPlayable(in audio))
            {
                //"ignored".ToLog();
                return null;
            }

            ProcessOnPlay(in audio, insAudio);
            return insAudio;
        }

        public static Audio Play(AudioKey audioKey)
        {
            AudioSource insAudio = InternalPlay(audioKey, out var audio);
            if (insAudio == null)
            {
                return audio;
            }

            insAudio.Play();
#if UNITY_EDITOR
            $"Playing AudioClip:{insAudio.clip.name} | Key:{audioKey}".ToLog(Channel.Audio);
#endif
            return audio;
        }
        public static Audio Play(AudioKey audioKey, Vector3 position)
        {
            AudioSource insAudio = InternalPlay(audioKey, out var audio);
            if (insAudio == null)
            {
                return Audio.Invalid;
            }

            insAudio.transform.position = position;
            insAudio.Play();
#if UNITY_EDITOR
            $"Playing {audioKey}".ToLog(Channel.Audio);
#endif
            return audio;
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

            public Timer lastPlayedTime;

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
#if DEBUG_MODE
                ins.name = $"Default AudioSource {s_AudioSourceCounter++}";
#endif
                ins.transform.SetParent(Instance.transform);
                AudioSource insAudio = ins.GetComponent<AudioSource>();
                insAudio.playOnAwake = false;

                Instance.m_AudioContainer.Register(insAudio);

                return insAudio;
            }
            private static void OnGet(AudioSource t)
            {

            }
            private static void OnReserve(AudioSource t)
            {
                t.Stop();

                t.clip = null;
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
            // ���� ���� ���
            AudioManager.Initialize(null);

            // ����� ���
            AudioManager.Play(
                ///<see cref="AudioList.FriendlyName"/>
                "Soundtrack01"
                ///<see cref="AudioList.Data"/>
                //"Assets/test/test.wav"
                );
            EventBroadcaster.PostEvent<PlayAudioEvent>(PlayAudioEvent.GetEvent("Soundtrack01"));
        }
    }
}