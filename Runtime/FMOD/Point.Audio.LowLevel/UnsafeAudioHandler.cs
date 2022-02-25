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

using FMOD.Studio;
using Point.Collections;
using Point.Collections.Buffer.LowLevel;
using System;
using Unity.Collections;
using Unity.Mathematics;

namespace Point.Audio.LowLevel
{
    [BurstCompatible]
    internal unsafe struct UnsafeAudioHandler : IEmpty, IEquatable<UnsafeAudioHandler>
    {
        public readonly Hash hash;

        public FMOD.Studio.EventInstance instance;
        public uint generation;
        /// <summary>
        /// 재생시 연결된 <see cref="Audio.hash"/> 과 동일한 값
        /// </summary>
        public Hash instanceHash;

        public float3 translation;
        public quaternion rotation;

        public PLAYBACK_STATE playbackState
        {
            get
            {
                instance.getPlaybackState(out var state);
                return state;
            }
        }

        public UnsafeAudioHandler(Hash hash)
        {
            this.hash = hash;

            instance = default(EventInstance);
            generation = 0;

            instanceHash = Hash.Empty;

            translation = 0;
            rotation = quaternion.identity;
        }

        public bool IsEmpty() => !instance.isValid();
        public void Clear()
        {
            instance.release();
            instance.clearHandle();

            instanceHash = Hash.Empty;
        }

        public bool ValidateAudio(in Audio audio)
        {
            return audio.hash.Equals(instanceHash);
        }

        public void CreateInstance(ref Audio audio)
        {
            audio.eventDescription.createInstance(out instance);

            if (audio.Is3D && audio.OverrideAttenuation)
            {
                instance.setProperty(FMOD.Studio.EVENT_PROPERTY.MINIMUM_DISTANCE, audio.OverrideMinDistance);
                instance.setProperty(FMOD.Studio.EVENT_PROPERTY.MAXIMUM_DISTANCE, audio.OverrideMaxDistance);
            }

            SetParameters(ref audio);
        }
        public void SetParameters(ref Audio audio)
        {
            for (int i = 0; i < audio.parameters.Length; i++)
            {
                var result = instance.setParameterByID(
                    audio.parameters[i].description.id,
                    audio.parameters[i].value,
                    audio.parameters[i].ignoreSeekSpeed);

                if (result != FMOD.RESULT.OK)
                {
                    PointHelper.LogError(Channel.Audio,
                        $"Parameter({(string)audio.parameters[i].description.name}) set failed with {result}");
                }

                //paramsString += audio.parameters[i].ToString() + " ";
            }

            if (audio.Is3D && audio.OverrideAttenuation)
            {
                instance.setProperty(FMOD.Studio.EVENT_PROPERTY.MINIMUM_DISTANCE, audio.OverrideMinDistance);
                instance.setProperty(FMOD.Studio.EVENT_PROPERTY.MAXIMUM_DISTANCE, audio.OverrideMaxDistance);
            }
        }
        public void Set3DAttributes()
        {
            instance.set3DAttributes(Get3DAttributes());
        }
        public FMOD.ATTRIBUTES_3D Get3DAttributes()
        {
            FMOD.ATTRIBUTES_3D att = new FMOD.ATTRIBUTES_3D();

            float3
                forward = math.mul(rotation, math.forward()),
                up = math.mul(rotation, math.up());

            att.forward = new FMOD.VECTOR
            {
                x = forward.x,
                y = forward.y,
                z = forward.z
            };
            att.up = new FMOD.VECTOR
            {
                x = up.x,
                y = up.y,
                z = up.z
            };
            att.position = new FMOD.VECTOR
            {
                x = translation.x,
                y = translation.y,
                z = translation.z
            };

            return att;
        }

        public void StartInstance()
        {
            instance.setCallback(Callback, EVENT_CALLBACK_TYPE.STOPPED);
            instance.start();
        }
        private FMOD.RESULT Callback(EVENT_CALLBACK_TYPE type, IntPtr _event, IntPtr parameters)
        {
            //UnsafeReference<FMOD.Studio.EventInstance> ev = new UnsafeReference<EventInstance>(_event);
            //ev.Value.release();

            instance.release();
            instance.clearHandle();

            instanceHash = Hash.Empty;

            return FMOD.RESULT.OK;
        }
        public void StopInstance(bool allowFadeOut)
        {
            instance.stop(allowFadeOut ? STOP_MODE.ALLOWFADEOUT : STOP_MODE.IMMEDIATE);
        }

        public bool Equals(UnsafeAudioHandler other) => hash.Equals(other.hash);
    }
}
