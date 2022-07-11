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

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using System;
using Point.Collections;
using Point.Collections.Buffer;
using Unity.Burst;
using Point.Collections.Buffer.LowLevel;
using System.Collections.Generic;
using System.Collections;

namespace Point.Audio.LowLevel
{
    //////////////////////////////////////////////////////////////////////////////////////////
    /*                                   Critical Section                                   */
    /*                                       수정금지                                        */
    /*                                                                                      */
    /*                          Unsafe pointer를 포함하는 코드입니다                          */
    //////////////////////////////////////////////////////////////////////////////////////////

    [BurstCompatible]
    internal unsafe struct UnsafeAudioHandlerContainer : IDisposable
    {
        #region Inner Classes

        [BurstCompatible]
        internal unsafe struct Buffer : IValidation, IDisposable
        {
            [NativeDisableUnsafePtrRestriction]
            private UnsafeAllocator<UnsafeAudioHandler> m_Buffer;
            private UnsafeParallelHashMap<Hash, int> m_HandlerHashMap;

            private JobHandle m_JobHandle;
            private bool m_Disposed;

            public Buffer(int length)
            {
                m_Buffer = new UnsafeAllocator<UnsafeAudioHandler>(length, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                m_HandlerHashMap = new UnsafeParallelHashMap<Hash, int>(length * 2, Allocator.Persistent);
                for (int i = 0; i < length; i++)
                {
                    m_Buffer[i] = new UnsafeAudioHandler(Hash.NewHash());
                    m_HandlerHashMap.Add(m_Buffer[i].hash, i);
                }

                m_JobHandle = default(JobHandle);
                m_Disposed = false;
            }
            public bool IsValid() => !m_Disposed;
            private bool LogMessageIfNotValid()
            {
                if (IsValid()) return true;

                PointHelper.LogError(Channel.Audio,
                        $"You are accessing {nameof(UnsafeAudioHandlerContainer)} that already disposed. This is not allowed.");
                return false;
            }

            public UnsafeReference<UnsafeAudioHandler> GetAudioHandler(Hash hash)
            {
                if (!LogMessageIfNotValid())
                {
                    return default(UnsafeReference<UnsafeAudioHandler>);
                }
                
                var temp = m_Buffer.ElementAt(m_HandlerHashMap[hash]);
                if (!temp.Value.hash.Equals(hash))
                {
                    "??".ToLog();
                    for (int i = 0; i < m_Buffer.Length; i++)
                    {
                        if (m_Buffer[i].hash.Equals(hash))
                        {
                            "rtn for".ToLog();
                            return m_Buffer.ElementAt(i);
                        }
                    }
                }

                return temp;
            }
            public UnsafeReference<UnsafeAudioHandler> GetUnusedHandler()
            {
                if (!LogMessageIfNotValid())
                {
                    return default(UnsafeReference<UnsafeAudioHandler>);
                }

                int index = GetUnusedHandlerIndex();

                return m_Buffer.ElementAt(index);
            }
            private int GetUnusedHandlerIndex()
            {
                for (int i = 0; i < m_Buffer.Length; i++)
                {
                    if (m_Buffer[i].IsEmpty()) return i;
                }

                IncrementHandlerArray();

                return GetUnusedHandlerIndex();
            }
            private void IncrementHandlerArray()
            {
                m_JobHandle.Complete();

                "resizing container".ToLog();

                int prevLength = m_Buffer.Length;
                m_Buffer.Resize(prevLength * 2, NativeArrayOptions.ClearMemory);

                for (int i = prevLength; i < m_Buffer.Length; i++)
                {
                    m_Buffer[i] = new UnsafeAudioHandler(Hash.NewHash());
                }

                m_HandlerHashMap.Clear();
                for (int i = 0; i < m_Buffer.Length; i++)
                {
                    m_HandlerHashMap.Add(m_Buffer[i].hash, i);
                }
            }
            
            /// <summary>
            /// 현재 재생 중인 모든 인스턴스에서 탐색됩니다.
            /// </summary>
            /// <param name="desc"></param>
            /// <returns></returns>
            public FindEventEnumerator FindEventInstancesOf(FMOD.Studio.EventDescription desc)
            {
                if (!LogMessageIfNotValid())
                {
                    return default(FindEventEnumerator);
                }

                return new FindEventEnumerator(m_Buffer, desc);
            }

            public void Dispose()
            {
                m_JobHandle.Complete();

                m_Buffer.Dispose();
                m_HandlerHashMap.Dispose();

                m_Disposed = true;
            }

            #region Jobs

            public JobHandle Combine(JobHandle a)
            {
                m_JobHandle = JobHandle.CombineDependencies(m_JobHandle, a);
                return m_JobHandle;
            }

            public void CompleteAllJobs() => m_JobHandle.Complete();
            public JobHandle ScheduleUpdate()
            {
                if (!LogMessageIfNotValid())
                {
                    return default(JobHandle);
                }

                CompleteAllJobs();

                TranslationUpdateJob trJob = new TranslationUpdateJob
                {
                    handlers = m_Buffer.Ptr
                };
                AudioCheckJob checkJob = new AudioCheckJob
                {
                    handlers = m_Buffer.Ptr
                };

                JobHandle trJobHandle = trJob.Schedule(m_Buffer.Length, 64, m_JobHandle);
                Combine(trJobHandle);

                JobHandle checkJobHandle = checkJob.Schedule(m_Buffer.Length, 64, m_JobHandle);
                Combine(checkJobHandle);

                return m_JobHandle;
            }

            [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
            private struct TranslationUpdateJob : IJobParallelFor
            {
                [NativeDisableUnsafePtrRestriction]
                public UnsafeAudioHandler* handlers;

                public void Execute(int i)
                {
                    if (handlers[i].IsEmpty()) return;

                    (handlers + i)->Set3DAttributes();
                }
            }
            [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
            private struct AudioCheckJob : IJobParallelFor
            {
                [NativeDisableUnsafePtrRestriction]
                public UnsafeAudioHandler* handlers;

                public void Execute(int i)
                {
                    if (handlers[i].IsEmpty()) return;

                    if (handlers[i].playbackState == FMOD.Studio.PLAYBACK_STATE.STOPPED)
                    {
                        (handlers + i)->Clear();
                    }
                }
            }

            #endregion

            [UnityEngine.Scripting.Preserve]
            static void AOTCodeGenerator()
            {
                TypeHelper.AOTCodeGenerator<UnsafeAudioHandler>();
            }
        }
        public struct FindEventEnumerator : IEnumerable<UnsafeReference<UnsafeAudioHandler>>
        {
            UnsafeAllocator<UnsafeAudioHandler> m_Buffer;
            FMOD.Studio.EventDescription m_Description;

            public FindEventEnumerator(
                UnsafeAllocator<UnsafeAudioHandler> buffer,
                FMOD.Studio.EventDescription desc)
            {
                m_Buffer = buffer;
                m_Description = desc;
            }

            public IEnumerator<UnsafeReference<UnsafeAudioHandler>> GetEnumerator()
            {
                for (int i = 0; i < m_Buffer.Length; i++)
                {
                    if (m_Buffer[i].IsEmpty() ||
                        m_Buffer[i].instance.getDescription(out var desc) != FMOD.RESULT.OK)
                    {
                        continue;
                    }

                    if (m_Description.handle.Equals(desc.handle))
                    {
                        yield return m_Buffer.ElementAt(i);
                    }
                }
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        #endregion

        private UnsafeAllocator<Buffer> m_Buffer;
        private bool m_IsCreated;

        public ref Buffer Data => ref m_Buffer[0];
        public bool IsCreated => m_IsCreated;

        public UnsafeAudioHandlerContainer(int length)
        {
            m_Buffer = new UnsafeAllocator<Buffer>(1, Allocator.Persistent);
            m_Buffer[0] = new Buffer(length);

            m_IsCreated = true;
        }
        public void Dispose()
        {
            m_Buffer[0].Dispose();
            m_Buffer.Dispose();
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////
    /*                                End of Critical Section                               */
    //////////////////////////////////////////////////////////////////////////////////////////
}
