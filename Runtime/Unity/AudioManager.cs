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
        [NonSerialized] private Dictionary<AudioKey, ManagedAudioData> m_CachedManagedDataMap;

        //
        [NonSerialized] AssetBundleInfo m_AudioBundle;
        [NonSerialized] ObjectPool<AudioSource> m_DefaultAudioPool;

        [NonSerialized] readonly Dictionary<Hash, AssetInfo> m_CachedAssetInfoMap = new Dictionary<Hash, AssetInfo>();
        [NonSerialized] readonly Dictionary<Hash, PrefabInfo> m_CachedPrefabInfo = new Dictionary<Hash, PrefabInfo>();

        [NonSerialized] private InternalAudioContainer m_AudioContainer;
        [NonSerialized] private AudioListener m_MainListener = null;
#if UNITY_EDITOR
        private static bool s_AudioBundleIsNotLoadedErrorSended = false;
#endif

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

        //////////////////////////////////////////////////////////////////////////////////////////
        /*                                   Critical Section                                   */
        /*                                       수정금지                                        */
        //////////////////////////////////////////////////////////////////////////////////////////

        #region Initialize

        protected override void OnInitialize()
        {
            m_Settings = AudioSettings.Instance;

            EntryCount = m_Settings.CalculateEntryCount();
            m_FriendlyNameMap = new Dictionary<Hash, Hash>();
            m_DataHashMap = new NativeHashMap<AudioKey, CompressedAudioData>(EntryCount, AllocatorManager.Persistent);
            m_CachedManagedDataMap = new Dictionary<AudioKey, ManagedAudioData>();

            m_Settings.RegisterFriendlyNames(m_FriendlyNameMap);
            foreach (var data in m_Settings.GetAudioData())
            {
                var temp = data.GetAudioData();

                m_DataHashMap.Add(temp.AudioKey, temp);
                m_CachedManagedDataMap.Add(temp.AudioKey, 
                    new ManagedAudioData
                    {
                        audioMixerGroup = data.GetAudioMixerGroup(),
                        onPlayConstAction = data.GetOnPlayConstAction(),

                        childs = data.GetChilds(),
                        playOption = data.GetPlayOption(),
                    });
            }

            EventBroadcaster.AddEvent<PlayAudioEvent>(PlayAudioEventHandler);

            m_AudioContainer = new InternalAudioContainer(1024);
            m_DefaultAudioPool = new ObjectPool<AudioSource>(
                DefaultAudioFactory,
                DefaultAudioOnGet,
                DefaultAudioOnReserve,
                null
                );
            m_DefaultAudioPool.AddObjects(15);
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

        //////////////////////////////////////////////////////////////////////////////////////////
        /*                                End of Critical Section                               */
        //////////////////////////////////////////////////////////////////////////////////////////

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
        public static void ValidateListener()
        {
            if (Instance.m_MainListener != null)
            {
                Instance.m_MainListener.enabled = true;
                return;
            }

            AudioListener[] listeners = FindObjectsOfType<AudioListener>();
            if (listeners.Length == 0)
            {
                GameObject listener = new GameObject();
#if DEBUG_MODE
                listener.name = "Default Listener";
#endif
                Instance.m_MainListener = listener.AddComponent<AudioListener>();

                DontDestroyOnLoad(listener);
                return;
            }
            else if (listeners.Length > 1)
            {
#if DEBUG_MODE
                PointHelper.LogError(Channel.Audio,
                    $"There\'s more then one {nameof(AudioListener)} in current scene. This is not allowed.");
#endif
                for (int i = listeners.Length - 1; i >= 1; i--)
                {
                    Destroy(listeners[i]);
                }
            }

            listeners[0].enabled = true;
            Instance.m_MainListener = listeners[0];
        }

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

            if (!m_CachedAssetInfoMap.TryGetValue(prefabKey, out AssetInfo prefabAsset))
            {
                if (m_AudioBundle.TryLoadAsset(prefabKey, out prefabAsset))
                {
                    m_CachedAssetInfoMap.Add(prefabKey, prefabAsset);
                }
                // 프리팹이 없다?
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
        private static AudioClip GetAudioClip(AudioKey audioKey)
        {
            ManagedAudioData managedData; int index;
            AudioManager ins = Instance;

            //////////////////////////////////////////////////////////////////////////////////////////
            /*                                   Critical Section                                   */
            /*                                       수정금지                                        */
            //////////////////////////////////////////////////////////////////////////////////////////
            if (!audioKey.IsValid())
            {
                return null;
            }
            else if (!ins.m_AudioBundle.IsValid())
            {
#if UNITY_EDITOR
                Hash targetKey = GetConcreteKey(in audioKey);

                if (!s_AudioBundleIsNotLoadedErrorSended)
                {
                    PointHelper.LogError(Channel.Audio,
                        $"Audio AssetBundle is not loaded. This is not allowed. Please register AssetBundle with AudioManager.Initialize(AssetBundle)\nThis request({targetKey}) will be accepted only in Editor with {nameof(UnityEditor.AssetDatabase)}.");

                    s_AudioBundleIsNotLoadedErrorSended = true;
                }

                if (!ins.m_CachedManagedDataMap.TryGetValue(targetKey, out managedData) ||
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
            /*                                End of Critical Section                               */
            //////////////////////////////////////////////////////////////////////////////////////////

            if (!ins.m_CachedAssetInfoMap.TryGetValue(audioKey, out AssetInfo clipAsset))
            {
                clipAsset = ins.m_AudioBundle.LoadAsset(audioKey);
                ins.m_CachedAssetInfoMap.Add(audioKey, clipAsset);
            }

            if (!ins.m_CachedManagedDataMap.TryGetValue(audioKey, out managedData) ||
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
            /// <see cref="AudioList"/> 에 세부 정보가 등록되지 않은 오디오 클립
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

            Dictionary<Hash, AssetInfo> cachedAssetInfo = Instance.m_CachedAssetInfoMap;
            if (!cachedAssetInfo.TryGetValue(prefabKey, out AssetInfo prefabAsset))
            {
                if (audioBundle.TryLoadAsset(prefabKey, out prefabAsset))
                {
                    cachedAssetInfo.Add(prefabKey, prefabAsset);
                }
                // 프리팹이 없다?
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

        private static RESULT IssueAudioSource(in AudioKey audioKey, out AudioSource audioSource,
            out CompressedAudioData data, out ManagedAudioData managedData)
        {
            AudioClip clip = GetAudioClip(audioKey);
            if (clip == null)
            {
                $"Could\'nt find audio clip {audioKey}.".ToLogError();

                audioSource = null;
                data = default(CompressedAudioData);
                managedData = null;
                return RESULT.AUDIOCLIP | RESULT.NOTFOUND;
            }

            /// <see cref="AudioList"/> 에 세부 정보가 등록되지 않은 오디오 클립
            if (!TryGetCompressedAudioData(audioKey, out data))
            {
                audioSource = Instance.m_DefaultAudioPool.Get();
                audioSource.outputAudioMixerGroup = AudioSettings.Instance.DefaultMixerGroup;
                audioSource.clip = clip;

                data = default(CompressedAudioData);
                managedData = null;
                return RESULT.OK;
            }

            managedData = Instance.m_CachedManagedDataMap[data.AudioKey];
            audioSource = Instance.CreateAudioSource(data.PrefabKey);
            //////////////////////////////////////////////////////////////////////////////////////////
            /*                                                                                      */
            audioSource.outputAudioMixerGroup = managedData.audioMixerGroup;
            audioSource.clip = clip;

            audioSource.volume = data.GetVolume();
            audioSource.pitch = data.GetPitch();
            /*                                                                                      */
            //////////////////////////////////////////////////////////////////////////////////////////

            return RESULT.OK;
        }
        //private static AudioSource GetAudio(in AudioKey audioKey, out Audio audio, 
        //    out CompressedAudioData data, out ManagedAudioData managedData)
        //{
        //    audio = default(Audio);
        //    var result = IssueAudioSource(in audioKey, out AudioSource insAudio, out data, out managedData);
        //    if (result == RESULT.OK)
        //    {
        //        audio = Instance.m_AudioContainer.GetAudio(insAudio, audioKey);
        //    }
            
        //    return insAudio;
        //}

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

        private static bool ProcessIsPlayable(in AudioKey audio)
        {
            AudioKey audioKey = GetConcreteKey(in audio);

            if (!Instance.m_CachedManagedDataMap.TryGetValue(audioKey, out ManagedAudioData managedData) ||
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

            if (Instance.m_CachedManagedDataMap.TryGetValue(audioKey, out ManagedAudioData managedData))
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

        internal static RESULT PlayAudio(ref Audio audio)
        {
            AudioSource audioSource;
            if (audio.RequireSetup())
            {
                if (!audio.IsValid())
                {
                    "not valid".ToLogError();
                    return RESULT.AUDIOKEY | RESULT.NOTVALID;
                }

                AudioKey audioKey = audio.m_AudioKey;
                RESULT result = InternalPlay(audioKey, out audio, out AudioSource insAudio);
                if (result != RESULT.OK)
                {
                    return result;
                }

                insAudio.Play();
                //audio = Play(key);
                return RESULT.OK;
            }
            else
            {
                if (!ProcessIsPlayable(in audio.m_AudioKey))
                {
                    //"ignored".ToLog();
                    return RESULT.IGNORED;
                }

                audioSource = GetAudioSource(audio);
                ProcessOnPlay(in audio, audioSource);
            }

            audioSource.Play();
            return RESULT.OK;
        }
        private static RESULT InternalPlay(in AudioKey audioKey, out Audio audio, out AudioSource audioSource)
        {
#if DEBUG_MODE
            const string c_IgnoredLogFormat = "Ignored AudioKey({0})";
#endif
            if (!ProcessIsPlayable(in audioKey))
            {
                PointHelper.Log(Channel.Audio,
                    string.Format(c_IgnoredLogFormat, audioKey.ToString()));

                audio = default(Audio);
                audioSource = null;
                return RESULT.IGNORED;
            }

            RESULT result = IssueAudioSource(in audioKey, out audioSource, 
                out CompressedAudioData data, out ManagedAudioData managedData);
            if (result != RESULT.OK)
            {
                audio = default(Audio);
                return result;   
            }

            audio = Instance.m_AudioContainer.GetAudio(audioSource, audioKey);
            //AudioSource insAudio = GetAudio(in audioKey, out audio, out _, out _);
            //if (insAudio == null) return null;

            ProcessOnPlay(in audio, audioSource);
            return RESULT.OK;
        }

        public static Audio Play(AudioKey audioKey)
        {
#if DEBUG_MODE
            const string c_LogFormat = "Playing AudioClip({0}) with AudioKey({1})";
#endif
            RESULT result = InternalPlay(audioKey, out Audio audio, out AudioSource insAudio);
            if (result != RESULT.OK)
            {
                return Audio.Invalid;
            }

            insAudio.Play();
#if DEBUG_MODE
            PointHelper.Log(Channel.Audio,
                string.Format(c_LogFormat, insAudio.clip.name, audioKey.ToString()));
#endif
            return audio;
        }
        public static Audio Play(AudioKey audioKey, Vector3 position)
        {
#if DEBUG_MODE
            const string c_LogFormat = "Playing AudioClip({0}) at {1} with AudioKey({2})";
#endif
            RESULT result = InternalPlay(audioKey, out Audio audio, out AudioSource insAudio);
            if (result != RESULT.OK)
            {
                return Audio.Invalid;
            }

            insAudio.transform.position = position;
            insAudio.Play();
#if DEBUG_MODE
            PointHelper.Log(Channel.Audio,
                string.Format(c_LogFormat, insAudio.clip.name, position.ToString(), audioKey.ToString()));
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
}