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
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

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
            if (instance.isValid())
            {
                instance.release();
                instance.clearHandle();
            }

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
#if DEBUG_MODE
                if (result != FMOD.RESULT.OK)
                {
                    PointHelper.LogError(Channel.Audio,
                        $"Parameter({(string)audio.parameters[i].description.name}) set failed with {result}");
                }
#endif
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
            //instance.setCallback(Callback);
            instance.start();
        }

        [AOT.MonoPInvokeCallback(typeof(FMOD.Studio.EVENT_CALLBACK))]
        private static FMOD.RESULT Callback(EVENT_CALLBACK_TYPE type, IntPtr _event, IntPtr parameterPtr)
        {
            FMOD.Studio.EventInstance instance = new FMOD.Studio.EventInstance(_event);

            // Retrieve the user data
            IntPtr stringPtr;
            instance.getUserData(out stringPtr);

            // Get the string object
            GCHandle stringHandle = GCHandle.FromIntPtr(stringPtr);
            string key = stringHandle.Target as string;

            PROGRAMMER_SOUND_PROPERTIES parameter;
            switch (type)
            {
                case EVENT_CALLBACK_TYPE.CREATED:
                    break;
                case EVENT_CALLBACK_TYPE.DESTROYED:
                    // Now the event has been destroyed, unpin the string memory so it can be garbage collected
                    stringHandle.Free();
                    break;
                case EVENT_CALLBACK_TYPE.STARTING:
                    break;
                case EVENT_CALLBACK_TYPE.STARTED:
                    break;
                case EVENT_CALLBACK_TYPE.RESTARTED:
                    break;
                case EVENT_CALLBACK_TYPE.STOPPED:
                    break;
                case EVENT_CALLBACK_TYPE.START_FAILED:
                    break;
                case EVENT_CALLBACK_TYPE.CREATE_PROGRAMMER_SOUND:
                    FMOD.MODE soundMode = FMOD.MODE.LOOP_NORMAL | FMOD.MODE.CREATECOMPRESSEDSAMPLE | FMOD.MODE.NONBLOCKING;
                    parameter = (FMOD.Studio.PROGRAMMER_SOUND_PROPERTIES)Marshal.PtrToStructure(parameterPtr, typeof(FMOD.Studio.PROGRAMMER_SOUND_PROPERTIES));

                    if (key.Contains("."))
                    {
                        FMOD.Sound dialogueSound;
                        FMOD.RESULT soundResult = FMODUnity.RuntimeManager.CoreSystem.createSound(Application.streamingAssetsPath + "/" + key, soundMode, out dialogueSound);
                        if (soundResult == FMOD.RESULT.OK)
                        {
                            parameter.sound = dialogueSound.handle;
                            parameter.subsoundIndex = -1;
                            Marshal.StructureToPtr(parameter, parameterPtr, false);
                        }
                    }
                    else
                    {
                        FMOD.Studio.SOUND_INFO dialogueSoundInfo;
                        var keyResult = FMODUnity.RuntimeManager.StudioSystem.getSoundInfo(key, out dialogueSoundInfo);
                        if (keyResult != FMOD.RESULT.OK)
                        {
                            break;
                        }
                        FMOD.Sound dialogueSound;
                        var soundResult = FMODUnity.RuntimeManager.CoreSystem.createSound(dialogueSoundInfo.name_or_data, soundMode | dialogueSoundInfo.mode, ref dialogueSoundInfo.exinfo, out dialogueSound);
                        if (soundResult == FMOD.RESULT.OK)
                        {
                            parameter.sound = dialogueSound.handle;
                            parameter.subsoundIndex = dialogueSoundInfo.subsoundindex;
                            Marshal.StructureToPtr(parameter, parameterPtr, false);
                        }
                    }

                    break;
                case EVENT_CALLBACK_TYPE.DESTROY_PROGRAMMER_SOUND:
                    parameter = (PROGRAMMER_SOUND_PROPERTIES)Marshal.PtrToStructure(parameterPtr, TypeHelper.TypeOf<PROGRAMMER_SOUND_PROPERTIES>.Type);
                    var sound = new FMOD.Sound(parameter.sound);
                    sound.release();

                    break;
                case EVENT_CALLBACK_TYPE.PLUGIN_CREATED:
                    break;
                case EVENT_CALLBACK_TYPE.PLUGIN_DESTROYED:
                    break;
                case EVENT_CALLBACK_TYPE.TIMELINE_MARKER:
                    break;
                case EVENT_CALLBACK_TYPE.TIMELINE_BEAT:
                    break;
                case EVENT_CALLBACK_TYPE.SOUND_PLAYED:
                    break;
                case EVENT_CALLBACK_TYPE.SOUND_STOPPED:
                    break;
                case EVENT_CALLBACK_TYPE.REAL_TO_VIRTUAL:
                    break;
                case EVENT_CALLBACK_TYPE.VIRTUAL_TO_REAL:
                    break;
                case EVENT_CALLBACK_TYPE.START_EVENT_COMMAND:
                    break;
                case EVENT_CALLBACK_TYPE.NESTED_TIMELINE_BEAT:
                    break;
                case EVENT_CALLBACK_TYPE.ALL:
                    break;
                default:
                    break;
            }

            //instanceHash = Hash.Empty;

            return FMOD.RESULT.OK;
        }
        public void StopInstance(bool allowFadeOut)
        {
            instance.stop(allowFadeOut ? STOP_MODE.ALLOWFADEOUT : STOP_MODE.IMMEDIATE);
        }

        public bool Equals(UnsafeAudioHandler other) => hash.Equals(other.hash);
    }
}
