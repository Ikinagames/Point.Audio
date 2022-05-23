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
using System.IO;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Jobs;

namespace Point.Audio
{
    [AddComponentMenu("")]
    [XmlSettings(PropertyName = "Audio", SaveToDisk = false)]
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

        [NonSerialized] readonly Dictionary<Hash, IPrefabInfo> m_CachedPrefabInfo = new Dictionary<Hash, IPrefabInfo>();

        [NonSerialized] private InternalAudioContainer m_AudioContainer;
        [NonSerialized] private AudioListener m_MainListener = null;

#if UNITY_EDITOR
        private static bool s_AudioBundleIsNotLoadedErrorSended = false;
#endif

        // Runtime Settings
        [SerializeField, XmlField(PropertyName = "MasterVolume")]
        [Decibel]
        private float m_MasterVolume = 1.0f;
        [SerializeField, XmlField(PropertyName = "Mute")]
        private bool m_Mute = false;
        [XmlField(PropertyName = "UserFloats")]
        private Dictionary<string, float> m_UserFloatHashMap = new Dictionary<string, float>();

        //////////////////////////////////////////////////////////////////////////////////////////
        /*                                   Critical Section                                   */
        /*                                       수정금지                                        */
        //////////////////////////////////////////////////////////////////////////////////////////

        #region Critical Section

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

            // Load Runtime Settings
            XmlSettings.LoadSettings(this);
            AudioMixer audioMixer = AudioSettings.Instance.DefaultMixerGroup.audioMixer;
            List<string> invalidKeys = new List<string>();
            foreach (var item in m_UserFloatHashMap)
            {
                if (!audioMixer.GetFloat(item.Key, out float currentValue))
                {
                    invalidKeys.Add(item.Key);
                    continue;
                }

                audioMixer.SetFloat(item.Key, item.Value);
            }
            for (int i = 0; i < invalidKeys.Count; i++)
            {
                m_UserFloatHashMap.Remove(invalidKeys[i]);
            }

            if (m_Mute) AudioListener.volume = 0;
            else AudioListener.volume = m_MasterVolume;
        }
        protected override void OnShutdown()
        {
            // Save Runtime Settings
            AudioMixer audioMixer = AudioSettings.Instance.DefaultMixerGroup.audioMixer;
            foreach (var item in m_UserFloatHashMap.Keys.ToArray())
            {
                if (!audioMixer.GetFloat(item, out float currentValue))
                {
                    continue;
                }

                m_UserFloatHashMap[item] = currentValue;
            }
            XmlSettings.SaveSettings(this);

            foreach (var item in m_CachedPrefabInfo.Values)
            {
                item.Dispose();
            }
            m_CachedPrefabInfo.Clear();

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
            t.outputAudioMixerGroup = null;

            t.transform.SetParent(Instance.transform);
        }

        #endregion

        #region Monobehaviour Messages

        private void LateUpdate()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPaused)
#endif
            {
                m_AudioContainer.Update();
            }
        }

        #endregion

        #region Event Handlers

        private void PlayAudioEventHandler(PlayAudioEvent ev)
        {
            Audio audio;
            if (ev.HasPosition) audio = Play(ev.Key, ev.Position);
            else audio = Play(ev.Key);

            audio.AutoDisposal();
        }

        #endregion

        #region Internal

        private static ObjectPool<AudioSource> GetPool(in Hash prefabKey)
        {
            AssetBundleInfo audioBundle = Instance.m_AudioBundle;

            if (!audioBundle.IsValid() || prefabKey.IsEmpty())
            {
                return Instance.m_DefaultAudioPool;
            }

            //Dictionary<Hash, AssetInfo> cachedAssetInfo = Instance.m_CachedAssetInfoMap;
            IPrefabInfo info;
            if (!audioBundle.TryLoadAsset<AudioSource>(prefabKey, out AssetInfo<AudioSource> prefabAsset))
            // 프리팹이 없다?
            {
#if UNITY_EDITOR
                string editorPrefabKey = prefabKey.Key;
                if (editorPrefabKey.StartsWith("assets/resources"))
                {
                    editorPrefabKey = editorPrefabKey.Replace("assets/resources", String.Empty);
                }

                AudioSource prefab = Resources.Load<AudioSource>(editorPrefabKey);
                if (prefab == null)
                {
                    PointHelper.LogError(Channel.Audio,
                        $"There\'s no prefab({prefabKey}) in {nameof(AssetBundle)}({Instance.m_AudioBundle.AssetBundle.name}) either Resources folder. This is not allowed.");
                    return Instance.m_DefaultAudioPool;
                }

                if (!Instance.m_CachedPrefabInfo.TryGetValue(prefabKey, out info))
                {
                    info = new PreloadedPrefabInfo(prefab);
                    Instance.m_CachedPrefabInfo.Add(prefabKey, info);
                }

                PointHelper.LogError(Channel.Audio,
                    $"There\'s no prefab({prefab.gameObject.name}) in {nameof(AssetBundle)}({Instance.m_AudioBundle.AssetBundle.name}) but Resources folder. This is not allowed in runtime.");
                return info.Pool;
#else
                    throw new InvalidOperationException($"Prefab asset for audio is not available in currently registered audio {nameof(AssetBundle)}({audioBundle.AssetBundle.name}). This is not allowed cannot play requested audio.");
#endif
            }

            Dictionary<Hash, IPrefabInfo> cachedPrefabInfo = Instance.m_CachedPrefabInfo;
            if (!cachedPrefabInfo.TryGetValue(prefabKey, out info))
            {
                info = new PrefabInfo(prefabAsset);
                cachedPrefabInfo.Add(prefabKey, info);
            }

            return info.Pool;
        }
        private static ObjectPool<AudioSource> GetPool(in AudioKey audioKey)
        {
            /// <see cref="AudioList"/> 에 세부 정보가 등록되지 않은 오디오 클립
            if (!TryGetCompressedAudioData(audioKey, out CompressedAudioData data))
            {
                return Instance.m_DefaultAudioPool;
            }

            return GetPool(data.PrefabKey);
        }
        internal static AudioSource GetAudioSource(in Audio audio)
        {
            return Instance.m_AudioContainer.GetAudioSource(in audio);
        }

        internal static AudioKey GetConcreteKey(in AudioKey audioKey)
        {
            if (Instance.m_FriendlyNameMap.TryGetValue(audioKey, out var key))
            {
                return key;
            }
            return audioKey;
        }
        private static RESULT GetAudioClip(in AudioKey audioKey, out AssetInfo clipAsset, out AudioClip audioClip)
        {
            if (!audioKey.IsValid())
            {
                clipAsset = default(AssetInfo);
                audioClip = null;

                return RESULT.AUDIOKEY | RESULT.NOTVALID;
            }

            ManagedAudioData managedData; int index;
            AudioManager ins = Instance;
            AudioKey targetKey = GetConcreteKey(in audioKey);

            //////////////////////////////////////////////////////////////////////////////////////////
            /*                                   Critical Section                                   */
            /*                                       수정금지                                        */
            //////////////////////////////////////////////////////////////////////////////////////////
            if (!ins.m_AudioBundle.IsValid())
            {
#if UNITY_EDITOR
                if (!s_AudioBundleIsNotLoadedErrorSended)
                {
                    PointHelper.LogError(Channel.Audio,
                        $"Audio AssetBundle is not loaded. This is not allowed. Please register AssetBundle with AudioManager.Initialize(AssetBundle)\nThis request({targetKey}) will be accepted only in Editor with {nameof(UnityEditor.AssetDatabase)}.");

                    s_AudioBundleIsNotLoadedErrorSended = true;
                }

                if (!ins.m_CachedManagedDataMap.TryGetValue(targetKey, out managedData) ||
                    managedData.childs.Length == 0)
                {
                    clipAsset = AssetInfo.Invalid;
                    audioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(((Hash)targetKey).Key);

                    if (audioClip == null)
                    {
                        return RESULT.INVALID;
                    }
                    return RESULT.OK | RESULT.ASSETBUNDLE | RESULT.NOTLOADED;
                }

                index = managedData.GetIndex();
                if (index == 0)
                {
                    clipAsset = AssetInfo.Invalid;
                    audioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(((Hash)targetKey).Key);

                    if (audioClip == null)
                    {
                        return RESULT.INVALID;
                    }
                    return RESULT.OK | RESULT.ASSETBUNDLE | RESULT.NOTLOADED;
                }

                return GetAudioClip(new Hash(managedData.childs[index - 1].AssetPath.ToLowerInvariant()),   out clipAsset, out audioClip);
#else
                throw new Exception("Audio AssetBundle is not loaded. This is not allowed.");
#endif
            }
            //////////////////////////////////////////////////////////////////////////////////////////
            /*                                End of Critical Section                               */
            //////////////////////////////////////////////////////////////////////////////////////////

            if (!ins.m_CachedManagedDataMap.TryGetValue(targetKey, out managedData) ||
                managedData.childs.Length == 0)
            {
                if (!ins.m_AudioBundle.TryLoadAssetAsync(targetKey, out clipAsset))
                {
                    clipAsset = AssetInfo.Invalid;
#if UNITY_EDITOR
                    audioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(((Hash)targetKey).Key);
                    if (audioClip != null)
                    {
                        return RESULT.OK | RESULT.AudioClip_NotFound_In_AssetBundle;
                    }
#endif
                    audioClip = null;
                    return RESULT.AudioClip_NotFound_In_AssetBundle;
                }

                clipAsset.AddDebugger();
                audioClip = (AudioClip)clipAsset.Asset;
                return RESULT.OK;
            }

            index = managedData.GetIndex();
            if (index == 0)
            {
                if (!ins.m_AudioBundle.TryLoadAssetAsync(targetKey, out clipAsset))
                {
#if UNITY_EDITOR
                    audioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(((Hash)targetKey).Key);
                    if (audioClip != null)
                    {
                        return RESULT.OK | RESULT.AudioClip_NotFound_In_AssetBundle;
                    }
#endif
                    audioClip = null;
                    return RESULT.AUDIOCLIP | RESULT.NOTFOUND;
                }

                clipAsset.AddDebugger();
                audioClip = (AudioClip)clipAsset.Asset;
                return RESULT.OK;
            }

            RESULT result = GetAudioClip(new Hash(managedData.childs[index - 1].AssetPath.ToLowerInvariant()), out clipAsset, out audioClip);

            if ((result & RESULT.OK) == RESULT.OK) clipAsset.AddDebugger();

            return result;
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

        private static RESULT IssueAudioSource(in AudioKey audioKey, out AssetInfo clipInfo, out AudioSource audioSource,
            out CompressedAudioData data, out ManagedAudioData managedData)
        {
            RESULT result = GetAudioClip(audioKey, out clipInfo, out AudioClip clip);
            if ((result & RESULT.OK) != RESULT.OK)
            {
                //$"Could\'nt find audio clip {audioKey}.".ToLogError();

                audioSource = null;
                data = default(CompressedAudioData);
                managedData = null;
                return result;
            }

            /// <see cref="AudioList"/> 에 세부 정보가 등록되지 않은 오디오 클립
            if (!TryGetCompressedAudioData(audioKey, out data))
            {
                audioSource = Instance.m_DefaultAudioPool.Get();
                audioSource.outputAudioMixerGroup = AudioSettings.Instance.DefaultMixerGroup;
                audioSource.clip = clip;

                data = default(CompressedAudioData);
                managedData = null;

                clipInfo.AddDebugger();
                return RESULT.OK | result;
            }

            managedData = Instance.m_CachedManagedDataMap[data.AudioKey];
            audioSource = GetPool(data.PrefabKey).Get();
            //////////////////////////////////////////////////////////////////////////////////////////
            /*                                                                                      */
            audioSource.outputAudioMixerGroup = managedData.audioMixerGroup;
            audioSource.clip = clip;

            audioSource.volume = data.GetVolume();
            audioSource.pitch = data.GetPitch();
            /*                                                                                      */
            //////////////////////////////////////////////////////////////////////////////////////////

            clipInfo.AddDebugger();
            return RESULT.OK | result;
        }
        
        private static RESULT InternalPlay(in AudioKey audioKey,
            out Audio audio, out AudioSource audioSource)
        {
#if DEBUG_MODE
            const string c_IgnoredLogFormat = "Ignored AudioKey({0})";
#endif
            AudioKey concreteKey = GetConcreteKey(in audioKey);
            if (!ProcessIsPlayable(in concreteKey))
            {
#if DEBUG_MODE
                PointHelper.Log(Channel.Audio,
                    string.Format(c_IgnoredLogFormat, concreteKey.ToString()));
#endif
                audio = default(Audio);
                audioSource = null;
                return RESULT.IGNORED;
            }

            RESULT result = IssueAudioSource(in concreteKey, out AssetInfo clipInfo, out audioSource,
                out CompressedAudioData data, out ManagedAudioData managedData);
            if ((result & RESULT.OK) != RESULT.OK)
            {
                audio = default(Audio);
                return result;
            }
            clipInfo.AddDebugger();

            audio = Instance.m_AudioContainer.GetAudio(audioSource, in clipInfo);
            //AudioSource insAudio = GetAudio(in audioKey, out audio, out _, out _);
            //if (insAudio == null) return null;

            ProcessOnPlay(in audio, audioSource);
            return RESULT.OK | result;
        }

        //////////////////////////////////////////////////////////////////////////////////////////
        /*                                 Audio Internal Methods                               */
        internal static RESULT PlayAudio(ref Audio audio)
        {
            AudioSource audioSource;
            if (audio.RequireSetup())
            {
                if (!audio.audioKey.IsValid())
                {
                    return RESULT.AUDIOKEY | RESULT.NOTVALID;
                }

                AudioKey audioKey = audio.audioKey;
                if (!ProcessPlugin(in audioKey,
                    audio.hasAudioSource ? audio.is3D : false,
                    audio.hasAudioSource ? audio.position : Vector3.zero))
                {
                    return RESULT.IGNORED;
                }

                RESULT result = InternalPlay(audioKey, out audio, out AudioSource insAudio);
                if ((result & RESULT.OK) != RESULT.OK)
                {
                    return result;
                }

                audio.m_AudioClip.AddDebugger();
                insAudio.Play();
                return RESULT.OK | result;
            }
            else
            {
                audioSource = GetAudioSource(in audio);
                if (!ProcessPlugin(audio.audioKey, audioSource.spatialize, audio.position))
                {
                    return RESULT.IGNORED;
                }

                if (!ProcessIsPlayable(audio.audioKey))
                {
                    //"ignored".ToLog();
                    return RESULT.IGNORED;
                }

                ProcessOnPlay(in audio, audioSource);
            }

            audioSource.Play();
            return RESULT.OK;
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
            if (!audio.audioKey.IsValid())
            {
                return;
            }

            ObjectPool<AudioSource> pool = GetPool(audio.audioKey);
            if (pool == null)
            {
                PointHelper.LogError(Channel.Audio,
                    $"Fatal error. Cannot found audio pool.");
                return;
            }

            AudioSource audioSource = GetAudioSource(audio);
            if (audioSource == null)
            {
                PointHelper.LogError(Channel.Audio,
                    $"Fatal error. Cannot found audio source GameObject.");
            }
            pool.Reserve(audioSource);
        }
        /*                                                                                      */
        //////////////////////////////////////////////////////////////////////////////////////////

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
            AudioKey audioKey = GetConcreteKey(audio.audioKey);

            if (Instance.m_CachedManagedDataMap.TryGetValue(audioKey, out ManagedAudioData managedData))
            {
                for (int i = 0; i < managedData.onPlayConstAction.Count; i++)
                {
                    managedData.onPlayConstAction[i].Execute(audioSource);
                }

                managedData.lastPlayedTime = Timer.Start();
            }
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

        private interface IPrefabInfo : IDisposable
        {
            ObjectPool<AudioSource> Pool { get; }
        }
        private sealed class PreloadedPrefabInfo : IPrefabInfo
        {
            private AudioSource m_AudioSource;
            private ObjectPool<AudioSource> m_Pool;

            public ObjectPool<AudioSource> Pool => m_Pool;

            public PreloadedPrefabInfo(AudioSource prefab)
            {
#if UNITY_EDITOR
                $"Fatal error. Prefab is null".ToLogError(LogChannel.Audio);
#endif
                m_AudioSource = prefab;
                m_Pool = new ObjectPool<AudioSource>(
                   Factory,
                   OnGet,
                   OnReserve,
                   null
                   );
            }
             
            private AudioSource Factory()
            {
                AudioSource prefabAudio = m_AudioSource;

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
                t.outputAudioMixerGroup = null;

                t.transform.SetParent(Instance.transform);
            }

            public void Dispose()
            {
                m_Pool.Dispose();

                m_AudioSource = null;
                m_Pool = null;
            }
        }
        private sealed class PrefabInfo : IPrefabInfo
        {
            private AssetInfo<AudioSource> m_Prefab;
            private ObjectPool<AudioSource> m_Pool;

            public ObjectPool<AudioSource> Pool => m_Pool;

            public PrefabInfo(AssetInfo<AudioSource> prefab)
            {
                this.m_Prefab = prefab;
                if (!m_Prefab.IsLoaded)
                {
                    $"not loaded prefab {prefab.Key}".ToLogError();
                }
                if (m_Prefab.Asset == null)
                {
                    $"asset null {prefab.Key}".ToLogError();
                }

                m_Pool = new ObjectPool<AudioSource>(
                    Factory,
                    OnGet,
                    OnReserve,
                    null
                    );
            }

            private AudioSource Factory()
            {
                AudioSource prefabAudio = m_Prefab.Asset;

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
                t.outputAudioMixerGroup = null;

                t.transform.SetParent(Instance.transform);
            }

            public void Dispose()
            {
                m_Prefab.Reserve();
                m_Pool.Dispose();

                m_Prefab = AssetInfo<AudioSource>.Invalid;
                m_Pool = null;
            }
        }

        private sealed unsafe class InternalAudioContainer : IDisposable
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
                if (audio.m_Index < 0) return null;

                return m_Audio[audio.m_Index];
            }
            public Audio GetAudio(AudioSource audioSource, in AssetInfo audioKey)
            {
                int index = Array.IndexOf(m_Audio, audioSource);
                if (index < 0)
                {
                    throw new Exception();
                }

                return new Audio(audioKey, index, audioSource.GetInstanceID(), transformations);
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
            }

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

        #endregion

        private readonly List<IAudioPlugin> m_Plugins = new List<IAudioPlugin>();

        private static bool ProcessPlugin(in AudioKey audioKey, in bool is3D, in Vector3 position)
        {
            IReadOnlyList<IAudioPlugin> plugins = Instance.m_Plugins;
            for (int i = 0; i < plugins.Count; i++)
            {
                IAudioPlugin plugin = plugins[i];

                if (plugin is IAudioOnRequested requested)
                {
#line hidden
                    try
                    {
                        requested.Process(in audioKey, in is3D, in position);
                    }
                    catch (Exception ex)
                    {
                        PointHelper.LogError(Channel.Audio,
                            $"Unhandled error has been raised.");
                        UnityEngine.Debug.LogException(ex);
                    }
#line default
                }

                if (!plugin.CanPlayable())
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////
        /*                                End of Critical Section                               */
        //////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// 모든 오디오의 전체 볼륨입니다.
        /// </summary>
        public static float MasterVolume
        {
            get => Instance.m_MasterVolume;
            set
            {
                float temp = Mathf.Min(0, value);
                if (1 < temp) temp = 1;

                Instance.m_MasterVolume = temp;
                if (!Mute) AudioListener.volume = temp;
            }
        }
        /// <summary>
        /// 오디오가 음소거되었나요?
        /// </summary>
        public static bool Mute
        {
            get => Instance.m_Mute;
            set
            {
                if (Instance.m_Mute == value) return;

                Instance.m_Mute = value;
                if (value) AudioListener.volume = 0;
                else AudioListener.volume = MasterVolume;
            }
        }

        #region User Floats

        public sealed class UserFloatWrapper
        {
            public float this[string key]
            {
                get => GetUserFloat(key);
                set => SetUserFloat(key, value);
            }
        }
        public static UserFloatWrapper UserFloat { get; } = new UserFloatWrapper();

        public static float GetUserFloat(string key)
        {
            Instance.m_UserFloatHashMap.TryGetValue(key, out float value);
            return value;
        }
        public static void SetUserFloat(string key, float value)
        {
            Instance.m_UserFloatHashMap[key] = value;
            AudioSettings.Instance.DefaultMixerGroup.audioMixer.SetFloat(key, value);
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
        public static void Initialize(AssetBundleInfo audioBundle)
        {
            Instance.m_AudioBundle = audioBundle;
            if (!audioBundle.IsLoaded)
            {
                audioBundle.LoadAsync();
            }
#if DEBUG_MODE
            foreach (var item in Instance.m_DataHashMap)
            {
                if (!Instance.m_AudioBundle.HasAsset(item.Key))
                {
                    $"?? {item.Value.AudioKey} does not exist from assetbundle {Path.GetFileName(audioBundle.AssetBundle.name)}".ToLogError();
                }
            }
#endif
        }
        public static void Initialize(string absoluteAssetBundlePath)
        {
            Instance.m_AudioBundle = ResourceManager.RegisterAssetBundleAbsolutePath(absoluteAssetBundlePath);
            Instance.m_AudioBundle.LoadAsync();
        }

        public static void SetListener(AudioListener audioListener)
        {
            Instance.m_MainListener = audioListener;
        }

        public static void ValidateListener()
        {
            if (Instance.m_MainListener != null)
            {
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

            Instance.m_MainListener = listeners[0];
        }

        public static void RegisterPlugin<TPlugin>(in TPlugin audioPlugin)
            where TPlugin : class, IAudioPlugin
        {
            Instance.m_Plugins.Add(audioPlugin);
        }
        public static void UnregisterPlugin<TPlugin>(in TPlugin audioPlugin)
            where TPlugin : class, IAudioPlugin
        {
            Instance.m_Plugins.Remove(audioPlugin);
        }

        public static Audio GetAudio(in AudioKey audioKey)
        {
            AudioKey concreteKey = GetConcreteKey(in audioKey);
            RESULT result = IssueAudioSource(in concreteKey, out AssetInfo clipInfo, 
                out AudioSource audioSource, 
                out CompressedAudioData data, out ManagedAudioData managedData);

            if ((result & RESULT.OK) != RESULT.OK)
            {
                return Audio.Invalid;
            }

            clipInfo.AddDebugger();
            Audio audio = Instance.m_AudioContainer.GetAudio(audioSource, in clipInfo);

            return audio;
        }
        public static bool IsPlaying(in Audio audio)
        {
            AudioSource audioSource = GetAudioSource(in audio);
            if (audioSource == null)
            {
                //PointHelper.LogError(Channel.Audio,
                //    $"This audio is invalid.");

                return false;
            }

            return audioSource.isPlaying;
        }

        public static Audio Play(AudioKey audioKey)
        {
#if DEBUG_MODE
            const string c_LogFormat = "Playing AudioClip({0}) with AudioKey({1})";
#endif
            if (!ProcessPlugin(in audioKey, false, Vector3.zero))
            {
                return Audio.Invalid;
            }

            RESULT result = InternalPlay(audioKey, out Audio audio, out AudioSource insAudio);
            if (result.IsConsiderAsError())
            {
                result.SendLog(in audioKey);
                return Audio.Invalid;
            }
            else if ((result & RESULT.IGNORED) == RESULT.IGNORED) return Audio.Invalid;

            if (result.IsRequireLog())
            {
                result.SendLog(in audioKey);
            }

            insAudio.Play();
#if DEBUG_MODE
            PointHelper.Log(Channel.Audio,
                string.Format(c_LogFormat, insAudio.clip == null ? "UNKNOWN" : insAudio.clip.name, audioKey.ToString()));
#endif
            return audio;
        }
        public static Audio Play(AudioKey audioKey, Vector3 position)
        {
#if DEBUG_MODE
            const string c_LogFormat = "Playing AudioClip({0}) at {1} with AudioKey({2})";
#endif
            if (!ProcessPlugin(in audioKey, true, in position))
            {
                return Audio.Invalid;
            }

            RESULT result = InternalPlay(audioKey, out Audio audio, out AudioSource insAudio);
            if (result.IsConsiderAsError())
            {
                result.SendLog(in audioKey, in position);
                return Audio.Invalid;
            }
            else if ((result & RESULT.IGNORED) == RESULT.IGNORED) return Audio.Invalid;

            if (result.IsRequireLog())
            {
                result.SendLog(in audioKey);
            }

            insAudio.transform.position = position;
            audio.position = position;

            insAudio.Play();
#if DEBUG_MODE
            PointHelper.Log(Channel.Audio,
                string.Format(c_LogFormat, insAudio.clip == null ? "UNKNOWN" : insAudio.clip.name, position.ToString(), audioKey.ToString()));
#endif
            return audio;
        }
    }

    [Obsolete("", true)]
    public static class AudioTestUsages
    {
        [Obsolete("", true)]
        public static void Test()
        {
            // 에셋 번들 등록
            AudioManager.Initialize(String.Empty);

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

    public interface IAudioPlugin
    {
        bool CanPlayable();
    }
    public interface IAudioOnRequested : IAudioPlugin
    {
        void Process(in AudioKey audioKey, in bool is3D, in Vector3 position);
    }
}