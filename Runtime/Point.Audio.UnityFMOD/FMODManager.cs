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

using UnityEngine;
using Point.Collections;
using Point.Audio.UnityFMOD;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Linq;
using Unity.Jobs;

namespace Point.Audio
{
    [AddComponentMenu("")]
    public sealed class FMODManager : StaticMonobehaviour<FMODManager>, IStaticInitializer
    {
        internal static FMOD.Studio.System StudioSystem => FMODUnity.RuntimeManager.StudioSystem;
        internal static FMOD.System CoreSystem => FMODUnity.RuntimeManager.CoreSystem;

        private bool m_IsFocusing;

        private JobHandle m_GlobalJobHandle;
        private NativeArray<AudioHandler> m_Handlers;
        private int m_HandlerLength;

        private NativeHashMap<FixedString512Bytes, ParamReference> m_GlobalParameters;

        protected override void OnInitialze()
        {
            m_IsFocusing = true;

            m_Handlers = new NativeArray<AudioHandler>(128, Allocator.Persistent);
            m_HandlerLength = 128;

            m_GlobalParameters = new NativeHashMap<FixedString512Bytes, ParamReference>(1024, AllocatorManager.Persistent);
        }
        protected override void OnShutdown()
        {
            m_Handlers.Dispose();
            m_GlobalParameters.Dispose();
        }

        #region Updates

        private void FixedUpdate()
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPaused) return;
#endif
            m_GlobalJobHandle.Complete();

            TranslationUpdateJob trUpdateJob = new TranslationUpdateJob
            {
                handlers = m_Handlers.AsReadOnly()
            };
            AudioDisposeJob audioDisposeJob = new AudioDisposeJob
            {
                handlers = m_Handlers
            };

            {
                JobHandle trUpdateJobHandle = trUpdateJob.Schedule(m_HandlerLength, 64, m_GlobalJobHandle);
                m_GlobalJobHandle = JobHandle.CombineDependencies(m_GlobalJobHandle, trUpdateJobHandle);
            }
            {
                JobHandle disposeJob = audioDisposeJob.Schedule(m_HandlerLength, m_GlobalJobHandle);
                m_GlobalJobHandle = JobHandle.CombineDependencies(m_GlobalJobHandle, disposeJob);
            }
        }

        private struct AudioDisposeJob : IJobFor
        {
            public NativeArray<AudioHandler> handlers;

            public void Execute(int i)
            {
                handlers[i].instance.getPlaybackState(out var state);
                if (state == FMOD.Studio.PLAYBACK_STATE.STOPPED)
                {
                    handlers[i].instance.release();
                    handlers[i].instance.clearHandle();
                }
            }
        }
        private struct TranslationUpdateJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<AudioHandler>.ReadOnly handlers;

            public void Execute(int i)
            {
                if (handlers[i].IsEmpty()) return;

                handlers[i].instance.set3DAttributes(handlers[i].Get3DAttributes());
            }
        }

        private void OnApplicationFocus(bool focus)
        {
            m_IsFocusing = focus;
        }

        #endregion

        #region Handler

        private unsafe AudioHandler* GetUnusedHandler()
        {
            AudioHandler* buffer = (AudioHandler*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(m_Handlers);
            int index = GetUnusedHandlerIndex(buffer);

            return buffer + index;

            int GetUnusedHandlerIndex(AudioHandler* buffer)
            {
                for (int i = 0; i < m_HandlerLength; i++)
                {
                    if (buffer[i].IsEmpty()) return i;
                }

                IncrementHandlerArray();

                return GetUnusedHandlerIndex(buffer);
            }
            void IncrementHandlerArray()
            {
                m_GlobalJobHandle.Complete();

                m_HandlerLength *= 2;
                NativeArray<AudioHandler> newArr
                    = new NativeArray<AudioHandler>(m_HandlerLength, Allocator.Persistent);
                m_Handlers.CopyTo(newArr);

                m_Handlers.Dispose();
                m_Handlers = newArr;
            }
        }

        #endregion

        public static ParamReference GetGlobalParameter(FixedString512Bytes name)
        {
            if (!Instance.m_GlobalParameters.TryGetValue(name, out var param))
            {
                param = new ParamReference(name.ToString());

                StudioSystem.getParameterByID(param.id, out float value);
                param.value = value;

                Instance.m_GlobalParameters.Add(name, param);
            }

            return param;
        }
        public static void SetGlobalParameter(FixedString512Bytes name, float value)
        {
            if (!Instance.m_GlobalParameters.TryGetValue(name, out var param))
            {
                param = new ParamReference(name.ToString(), value);

                Instance.m_GlobalParameters.Add(name, param);
            }
            else
            {
                param.value = value;
                Instance.m_GlobalParameters[name] = param;
            }

            StudioSystem.setParameterByID(param.id, value);
        }

        public static bool IsBankLoaded(string name) => FMODUnity.RuntimeManager.HasBankLoaded(name);
        public static bool LoadBank(string name, bool loadSamples = false)
        {
            try
            {
                FMODUnity.RuntimeManager.LoadBank(name, loadSamples);
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogException(e);
                return false;
            }

            FMODUnity.RuntimeManager.WaitForAllSampleLoading();
            return true;
        }
        public static void UnloadBank(string name)
        {
            FMODUnity.RuntimeManager.UnloadBank(name);
        }

        public static FMODAudio GetAudio(FMODUnity.EventReference eventRef)
        {
            var result = StudioSystem.getEventByID(eventRef.Guid, out FMOD.Studio.EventDescription ev);
#if DEBUG_MODE
            if (result != FMOD.RESULT.OK)
            {
                throw new System.Exception();
            }
#endif
            FMODAudio audio = new FMODAudio(ev);

            return audio;
        }
        public static FMODAudio GetAudio(string eventPath)
        {
            var result = StudioSystem.getEvent(eventPath, out FMOD.Studio.EventDescription ev);
#if DEBUG_MODE
            if (result != FMOD.RESULT.OK)
            {
                throw new System.Exception();
            }
#endif
            FMODAudio audio = new FMODAudio(ev);

            return audio;
        }

        public static void Play(ref FMODAudio audio)
        {
#if DEBUG_MODE
            if (!audio.IsValidID())
            {
                throw new System.Exception();
            }
#endif
            unsafe
            {
                AudioHandler* handler = Instance.GetUnusedHandler();
                {
                    handler->translation = audio._translation;
                    handler->rotation = audio._rotation;
                }
                audio.eventDescription.createInstance(out handler->instance);

                audio.audioHandler = handler;

                for (int i = 0; i < audio.parameters.Length; i++)
                {
                    handler->instance.setParameterByID(
                        audio.parameters[i].id,
                        audio.parameters[i].value,
                        audio.parameters[i].ignoreSeekSpeed);
                }

                if (audio.Is3D && audio.OverrideAttenuation)
                {
                    handler->instance.setProperty(FMOD.Studio.EVENT_PROPERTY.MINIMUM_DISTANCE, audio.OverrideMinDistance);
                    handler->instance.setProperty(FMOD.Studio.EVENT_PROPERTY.MAXIMUM_DISTANCE, audio.OverrideMaxDistance);
                }

                handler->instance.start();
            }
        }
        public static void Stop(ref FMODAudio audio)
        {
#if DEBUG_MODE
            if (!audio.IsValid())
            {
                throw new System.Exception();
            }
#endif
            unsafe
            {
                //if (audio.audioHandler->instance.isValid())
                //{
                    
                //}
                audio.audioHandler->instance.stop(audio.AllowFadeout ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
            }
        }
    }
}
