﻿// Copyright 2021 Ikina Games
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

using UnityEngine;
using Point.Collections;
using Point.Collections.ResourceControl;
using Unity.Collections;
using System.Collections.Generic;
using UnityEngine.Jobs;
using Unity.Jobs;
using Unity.Burst;
using Point.Collections.Buffer;
using UnityEngine.SceneManagement;
using Point.Collections.SceneManagement;
using UnityEngine.Audio;
using Point.Collections.Buffer.LowLevel;
using System;

namespace Point.Audio
{
    [AddComponentMenu("")]
    public sealed class AudioManager : StaticMonobehaviour<AudioManager>
        , IStaticInitializer
    {
        private const int c_InitialCount = 128;

        protected override bool EnableLog => false;
        protected override bool HideInInspector => true;

        private static int s_InstanceCount = 0;
        private static Transform s_Folder = null;

//#if DEBUG_MODE
//        private HashSet<AssetBundle> m_RegisteredAssetBundles;
//#endif

        private NativeList<AssetBundleInfo> m_AudioBundles;

        private JobHandle 
            m_GlobalJobHandle,
            m_UpdateTransformationJobHandle;

        private TransformScene<AudioSceneHandler> m_AudioScene;
        private Dictionary<AudioKey, AudioList.AudioSetting> m_RuntimeAudioSettings;

        private ObjectPool<Transform> m_AudioTransformPool;

        //private NativeArray<Audio> m_Audios;
        private UnsafeLinearHashMap<RuntimeAudioKey, UnsafeAudio> m_Audios;
        private Transform[] m_AudioTransforms;
        private TransformAccessArray m_TransformAccessArray;

        private struct AudioSceneHandler : ITransformSceneHandler
        {
            public void OnInitialize()
            {
            }

            public void OnTransformAdded(in NativeTransform transform)
            {
            }

            public void OnTransformRemove(in NativeTransform transform)
            {
            }

            public void Dispose()
            {
            }
        }
        private struct UpdateTransformationJob : IJobParallelForTransform
        {
            [ReadOnly] public NativeArray<UnsafeAudio> m_Audios;

            public void Execute(int i, TransformAccess transform)
            {
                transform.position = m_Audios[i].translation;
                transform.rotation = m_Audios[i].rotation;
            }
        }

        protected override void OnInitialze()
        {
//#if DEBUG_MODE
//            m_RegisteredAssetBundles = new HashSet<AssetBundle>();
//#endif

            //m_Audios = new NativeArray<Audio>(c_InitialCount, Allocator.Persistent);
            m_AudioTransforms = new Transform[c_InitialCount];
            m_TransformAccessArray = new TransformAccessArray(m_AudioTransforms);
            //FMODUnity.EventReference
            GameObject audioFolder = new GameObject("Audio");
            s_Folder = audioFolder.transform;

            m_AudioScene = new TransformScene<AudioSceneHandler>();
        }
        private static Transform AudioTransformFactory()
        {
            GameObject obj = new GameObject($"Audio_{s_InstanceCount}");
            s_InstanceCount++;
            obj.AddComponent<AudioSource>();

            return obj.transform;
        }
        private void Awake()
        {
            m_RuntimeAudioSettings = new Dictionary<AudioKey, AudioList.AudioSetting>();

            var list = PointAudioSettings.Instance.m_AudioLists;
            for (int i = 0; i < list.Length; i++)
            {
                list[i].Initialize(m_RuntimeAudioSettings);
            }
        }

        protected override void OnShutdown()
        {
            m_GlobalJobHandle.Complete();

            //if (m_AudioBundles.IsCreated)
            //{
            //    for (int i = 0; i < m_AudioBundles.Length; i++)
            //    {
            //        m_AudioBundles[i].Unload(true);
            //    }

            //    m_AudioBundles.Dispose();
            //}

            m_TransformAccessArray.Dispose();
            //m_Audios.Dispose();
            m_AudioTransforms = null;
        }

        private void FixedUpdate()
        {
            UpdateTransformations();
        }
        private void UpdateTransformations()
        {
            m_UpdateTransformationJobHandle.Complete();

            //{
            //    UpdateTransformationJob updateTransformation
            //        = new UpdateTransformationJob()
            //        {
            //            m_Audios = m_Audios
            //        };

            //    JobHandle job = updateTransformation.Schedule(m_TransformAccessArray);
            //    m_UpdateTransformationJobHandle
            //        = JobHandle.CombineDependencies(m_UpdateTransformationJobHandle, job);

            //    m_GlobalJobHandle
            //        = JobHandle.CombineDependencies(m_GlobalJobHandle, m_UpdateTransformationJobHandle);
            //}
        }

        internal AudioList.AudioSetting GetAudioSetting(AudioKey audioKey)
        {
#if DEBUG_MODE
            if (!m_RuntimeAudioSettings.ContainsKey(audioKey))
            {
                PointHelper.LogError(Channel.Audio,
                    $"You are trying to get an invalid audio setting(clipHash: \"{audioKey}\"). This is not allowed.");

                return null;
            }
#endif
            var setting = m_RuntimeAudioSettings[audioKey];
            setting.IncrementIndex();

            if (setting.CurrentIndex > 0 && setting.Keys.Length > 1)
            {
                AudioKey newKey = setting.Keys[setting.CurrentIndex];
                setting = m_RuntimeAudioSettings[newKey];
            }

            return setting;
        }

//        public static void RegisterAudioAssetBundle(params AssetBundle[] assetBundles)
//        {
//            if (!Instance.m_AudioBundles.IsCreated)
//            {
//                Instance.m_AudioBundles = new NativeList<AssetBundleInfo>(assetBundles.Length, AllocatorManager.Persistent);
//            }

//            for (int i = 0; i < assetBundles.Length; i++)
//            {
//#if DEBUG_MODE
//                if (Instance.m_RegisteredAssetBundles.Contains(assetBundles[i]))
//                {
//                    PointHelper.LogError(LogChannel.Audio,
//                        $"You\'re trying to register audio AssetBundle that already registered. " +
//                        $"This is not allowed.");
//                    continue;
//                }

//                Instance.m_RegisteredAssetBundles.Add(assetBundles[i]);
//#endif

//                AssetBundleInfo bundleInfo = ResourceManager.RegisterAssetBundle(assetBundles[i]);
//                Instance.m_AudioBundles.Add(bundleInfo);
//            }
//        }

        private UnsafeReference<KeyValue<RuntimeAudioKey, UnsafeAudio>> AddKey(in RuntimeAudioKey key)
        {
            int index = m_Audios.Add(key, new UnsafeAudio());
            UnsafeReference<KeyValue<RuntimeAudioKey, UnsafeAudio>> p = m_Audios.PointerAt(index);

            return p;
        }
        public static Audio GetAudio(in string key)
        {
            AssetInfo assetInfo = ResourceManager.LoadAsset(key);
            RuntimeAudioKey runtimeKey = RuntimeAudioKey.NewKey();
            RuntimeAudioSetting runtimeSetting = new RuntimeAudioSetting(key);

            UnsafeReference<KeyValue<RuntimeAudioKey, UnsafeAudio>> p = Instance.AddKey(runtimeKey);

            p.Value.Value = new UnsafeAudio(/*runtimeKey, */runtimeSetting, assetInfo);

            return new Audio(p);
        }
        public void Play()
        {

        }
    }

    [BurstCompatible]
    public struct RuntimeAudioKey : IEmpty, IEquatable<RuntimeAudioKey>
    {
        internal static RuntimeAudioKey NewKey() => new RuntimeAudioKey() { m_Hash = CollectionUtility.CreateHashCode2() };

        internal short m_Hash;

        public bool IsEmpty() => m_Hash == 0;
        public bool Equals(RuntimeAudioKey other) => m_Hash == other.m_Hash;
    }
    [BurstCompatible]
    public struct RuntimeAudioSetting
    {
        public AudioKey AudioKey;
        public AudioList.AudioOptions AudioOptions;
        public float 
            Volume,
            
            MinPitch, MaxPitch;
        public int MaximumPlayCount;
        public FixedList4096Bytes<AudioKey> VariationKeys;
        public int CurrentIndex;

        [NotBurstCompatible]
        public AudioSource Prefab
        {
            get
            {
                var setting = AudioManager.Instance.GetAudioSetting(AudioKey);
                return setting.Prefab;
            }
        }
        [NotBurstCompatible]
        public AudioMixerGroup AudioMixerGroup
        {
            get
            {
                var setting = AudioManager.Instance.GetAudioSetting(AudioKey);
                return setting.Group;
            }
        }

        [NotBurstCompatible]
        public RuntimeAudioSetting(AudioKey audioKey)
        {
            var setting = AudioManager.Instance.GetAudioSetting(audioKey);

            this.AudioKey = audioKey;
            this.AudioOptions = setting.Options;
            this.Volume = setting.Volume;
            this.MinPitch = setting.m_MinPitch;
            this.MaxPitch = setting.m_MaxPitch;
            this.MaximumPlayCount = setting.m_MaximumPlayCount;
            this.VariationKeys = new FixedList4096Bytes<AudioKey>();
            for (int i = 0; i < setting.Keys.Length; i++)
            {
                this.VariationKeys.Add(setting.Keys[i]);
            }
            this.CurrentIndex = setting.CurrentIndex;
        }
    }
}
