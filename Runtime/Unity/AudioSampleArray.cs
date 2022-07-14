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

using Point.Collections.Buffer.LowLevel;
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Point.Audio
{
    public struct AudioSampleArray : IDisposable
    {
        private UnsafeAllocator<AudioSample> m_Buffer;
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
        private AtomicSafetyHandle m_SafetyHandle;
        [NativeSetClassTypeToNullOnSchedule]
        private DisposeSentinel m_DisposeSentinel;
#endif

        public AudioSampleArray(AudioSample[] samples, Allocator allocator)
        {
            m_Buffer = new UnsafeAllocator<AudioSample>(samples, allocator);
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Create(out m_SafetyHandle, out m_DisposeSentinel, 1, allocator);
#endif
        }

        public void Dispose()
        {
            m_Buffer.Dispose();
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(ref m_SafetyHandle, ref m_DisposeSentinel);
            m_DisposeSentinel = null;
#endif
        }
    }
}