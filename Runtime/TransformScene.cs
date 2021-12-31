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
using Point.Collections.Buffer.LowLevel;
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.SceneManagement;

namespace Point.Audio
{
    [NativeContainer]
    public struct TransformScene : IDisposable
    {
        private readonly int m_SceneBuildIndex;
        private UnsafeLinearHashMap<SceneID, Transformation> m_HashMap;

        public Scene Scene => SceneManager.GetSceneByBuildIndex(m_SceneBuildIndex);

        public TransformScene(Scene scene, int initialCount = 128)
        {
            m_SceneBuildIndex = scene.buildIndex;
            m_HashMap = new UnsafeLinearHashMap<SceneID, Transformation>(initialCount, Allocator.Persistent);
        }

        public void Dispose()
        {
            m_HashMap.Dispose();
        }
    }

    [BurstCompatible]
    public readonly struct SceneID : IEquatable<SceneID>, IEmpty
    {
        private readonly Hash m_Hash;

        public Hash Hash => m_Hash;

        public bool IsEmpty() => m_Hash.IsEmpty();

        public bool Equals(SceneID other) => m_Hash.Equals(other.m_Hash);
    }
    [BurstCompatible]
    public readonly struct NativeTransform : IEquatable<NativeTransform>, IEmpty
    {
        private readonly UnsafeAllocator<Collections.KeyValue<SceneID, Transformation>>.ReadOnly m_Buffer;
        private readonly SceneID m_ID;

        internal NativeTransform(UnsafeLinearHashMap<SceneID, Transformation> hashMap, SceneID id)
        {
            m_Buffer = hashMap.Buffer;
            m_ID = id;
        }
        public bool IsEmpty() => m_ID.IsEmpty();

        public bool Equals(NativeTransform other) => m_ID.Equals(other.m_ID);
    }
}
