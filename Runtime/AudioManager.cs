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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEBUG_MODE
#endif

using UnityEngine;
using Point.Collections;
using Point.Collections.ResourceControl;
using Unity.Collections;
using System.Collections.Generic;
using UnityEngine.Jobs;
using Unity.Mathematics;
using Unity.Jobs;

namespace Point.Audio
{
    [AddComponentMenu("")]
    public sealed class AudioManager : StaticMonobehaviour<AudioManager>, IStaticInitializer
    {
#if !POINT_FMOD
        private const int c_InitialCount = 128;

        protected override bool EnableLog => false;
        protected override bool HideInInspector => true;

        private static int s_InstanceCount = 0;
        private static Transform s_Folder = null;

#if DEBUG_MODE
        private HashSet<AssetBundle> m_RegisteredAssetBundles;
#endif

        private NativeList<AssetBundleInfo> m_AudioBundles;

        private JobHandle 
            m_GlobalJobHandle,
            m_UpdateTransformationJobHandle;

        private ObjectPool<Transform> m_AudioTransformPool;
        private NativeArray<Audio> m_Audios;
        private Transform[] m_AudioTransforms;
        private TransformAccessArray m_TransformAccessArray;

        private struct Audio
        {
            public bool beingUsed;

            public float3 translation;
            public quaternion rotation;

            public AssetInfo audioClip;
        }
        private struct UpdateTransformationJob : IJobParallelForTransform
        {
            [ReadOnly] public NativeArray<Audio> m_Audios;

            public void Execute(int i, TransformAccess transform)
            {
                transform.position = m_Audios[i].translation;
                transform.rotation = m_Audios[i].rotation;
            }
        }

        protected override void OnInitialze()
        {
            m_RegisteredAssetBundles = new HashSet<AssetBundle>();

            m_Audios = new NativeArray<Audio>(c_InitialCount, Allocator.Persistent);
            m_AudioTransforms = new Transform[c_InitialCount];
            m_TransformAccessArray = new TransformAccessArray(m_AudioTransforms);

            GameObject audioFolder = new GameObject("Audio");
            s_Folder = audioFolder.transform;
        }
        private static Transform AudioTransformFactory()
        {
            GameObject obj = new GameObject($"Audio_{s_InstanceCount}");
            s_InstanceCount++;
            obj.AddComponent<AudioSource>();

            return obj.transform;
        }

        protected override void OnShutdown()
        {
            if (m_AudioBundles.IsCreated)
            {
                for (int i = 0; i < m_AudioBundles.Length; i++)
                {
                    m_AudioBundles[i].Unload(true);
                }

                m_AudioBundles.Dispose();
            }

            m_TransformAccessArray.Dispose();
            m_Audios.Dispose();
            m_AudioTransforms = null;
        }

        private void FixedUpdate()
        {
            UpdateTransformations();
        }
        private void UpdateTransformations()
        {
            m_UpdateTransformationJobHandle.Complete();

            {
                UpdateTransformationJob updateTransformation
                    = new UpdateTransformationJob()
                    {
                        m_Audios = m_Audios
                    };

                JobHandle job = updateTransformation.Schedule(m_TransformAccessArray);
                m_UpdateTransformationJobHandle
                    = JobHandle.CombineDependencies(m_UpdateTransformationJobHandle, job);

                m_GlobalJobHandle
                    = JobHandle.CombineDependencies(m_GlobalJobHandle, m_UpdateTransformationJobHandle);
            }
        }

        public static void RegisterAudioAssetBundle(params AssetBundle[] assetBundles)
        {
            for (int i = 0; i < assetBundles.Length; i++)
            {
#if DEBUG_MODE
                if (Instance.m_RegisteredAssetBundles.Contains(assetBundles[i]))
                {
                    Collections.Point.LogError(Collections.Point.LogChannel.Audio,
                        $"You\'re trying to register audio AssetBundle that already registered. " +
                        $"This is not allowed.");
                    continue;
                }

                Instance.m_RegisteredAssetBundles.Add(assetBundles[i]);
#endif

                AssetBundleInfo bundleInfo = ResourceManager.RegisterAssetBundle(assetBundles[i]);
                Instance.m_AudioBundles.Add(bundleInfo);
            }
        }

        public void Play()
        {

        }
    }
#endif
}
