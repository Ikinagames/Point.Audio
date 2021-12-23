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

namespace Point.Audio
{
    [AddComponentMenu("")]
    public sealed class FMODManager : StaticMonobehaviour<FMODManager>, IStaticInitializer
    {
        internal static FMOD.Studio.System StudioSystem => FMODUnity.RuntimeManager.StudioSystem;
        internal static FMOD.System CoreSystem => FMODUnity.RuntimeManager.CoreSystem;

        private NativeArray<AudioHandler> m_Handlers;

        protected override void OnInitialze()
        {
            m_Handlers = new NativeArray<AudioHandler>(128, Allocator.Persistent);
        }
        protected override void OnShutdown()
        {
            m_Handlers.Dispose();
        }

        #region Handler

        private int GetUnusedHandlerIndex()
        {
            for (int i = 0; i < m_Handlers.Length; i++)
            {
                if (m_Handlers[i].IsEmpty()) return i;
            }

            IncrementHandlerArray();

            return GetUnusedHandlerIndex();
        }
        private void IncrementHandlerArray()
        {
            NativeArray<AudioHandler> newArr 
                = new NativeArray<AudioHandler>(m_Handlers.Length * 2, Allocator.Persistent);
            m_Handlers.CopyTo(newArr);

            m_Handlers.Dispose();
            m_Handlers = newArr;
        }
        private unsafe AudioHandler* GetUnusedHandler()
        {
            int index = GetUnusedHandlerIndex();

            AudioHandler* buffer = (AudioHandler*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(m_Handlers);

            return buffer + index;
        }

        #endregion

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
                if (audio.audioHandler->instance.isValid())
                {
                    audio.audioHandler->instance.stop(audio.AllowFadeout ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
                    audio.audioHandler->instance.release();
                    audio.audioHandler->instance.clearHandle();
                }
            }
        }
    }
}
