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

namespace Point.Audio.LowLevel
{
    [NativeContainer]
    internal unsafe struct AudioHandlerContainer : IDisposable
    {
        [NativeDisableUnsafePtrRestriction]
        private AudioHandler* m_Buffer;
        //private UnsafeRingQueue<int>* m_UnusedIndices;
        private int m_Length;

        private JobHandle m_JobHandle;

        public AudioHandlerContainer(int length)
        {
            m_Buffer = (AudioHandler*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<AudioHandler>() * length,
                UnsafeUtility.AlignOf<AudioHandler>(),
                Allocator.Persistent);
            //m_UnusedIndices = unusedIndices;
            m_Length = length;

            m_JobHandle = default(JobHandle);

            //for (int i = 0; i < length; i++)
            //{
            //    (*unusedIndices).Enqueue(i);
            //    //m_UnusedIndices.AddNoResize(i);
            //}
        }

        public AudioHandler* GetUnusedHandler()
        {
            int index = GetUnusedHandlerIndex();

            return m_Buffer + index;
        }
        private int GetUnusedHandlerIndex()
        {
            for (int i = 0; i < m_Length; i++)
            {
                if (m_Buffer[i].IsEmpty()) return i;
            }

            IncrementHandlerArray();

            return GetUnusedHandlerIndex();
        }
        private void IncrementHandlerArray()
        {
            m_JobHandle.Complete();

            AudioHandler* tempBuffer = (AudioHandler*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<AudioHandler>() * (m_Length * 2),
                UnsafeUtility.AlignOf<AudioHandler>(),
                Allocator.Persistent);

            UnsafeUtility.MemCpy(tempBuffer, m_Buffer, UnsafeUtility.SizeOf<AudioHandler>() * m_Length);

            UnsafeUtility.Free(m_Buffer, Allocator.Persistent);
            m_Buffer = tempBuffer;

            m_Length *= 2;
        }

        public JobHandle Combine(JobHandle a)
        {
            m_JobHandle = JobHandle.CombineDependencies(m_JobHandle, a);
            return m_JobHandle;
        }

        public void Dispose()
        {
            m_JobHandle.Complete();

            UnsafeUtility.Free(m_Buffer, Allocator.Persistent);
            //(*unusedIndices).Enqueue(i);.Dispose();
        }

        public void CompleteAllJobs() => m_JobHandle.Complete();
        public JobHandle ScheduleUpdate()
        {
            CompleteAllJobs();

            TranslationUpdateJob trJob = new TranslationUpdateJob
            {
                handlers = m_Buffer
            };
            AudioCheckJob checkJob = new AudioCheckJob
            {
                handlers = m_Buffer
            };

            JobHandle trJobHandle = trJob.Schedule(m_Length, 64, m_JobHandle);
            Combine(trJobHandle);

            JobHandle checkJobHandle = checkJob.Schedule(m_Length, 64, m_JobHandle);
            Combine(checkJobHandle);

            return m_JobHandle;
        }

        private struct TranslationUpdateJob : IJobParallelFor
        {
            [NativeDisableUnsafePtrRestriction]
            public AudioHandler* handlers;

            public void Execute(int i)
            {
                if (handlers[i].IsEmpty()) return;

                handlers[i].instance.set3DAttributes(handlers[i].Get3DAttributes());
            }
        }
        private struct AudioCheckJob : IJobParallelFor
        {
            [NativeDisableUnsafePtrRestriction]
            public AudioHandler* handlers;

            public void Execute(int i)
            {
                if (handlers[i].IsEmpty()) return;

                handlers[i].instance.getPlaybackState(out var state);
                if (state == FMOD.Studio.PLAYBACK_STATE.STOPPED)
                {
                    (handlers + i)->instance.release();
                    (handlers + i)->instance.clearHandle();
                    (handlers + i)->hash = Hash.Empty;
                }
            }
        }
    }
}
