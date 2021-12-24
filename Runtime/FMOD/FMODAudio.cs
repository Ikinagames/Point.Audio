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
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Point.Audio
{
    [BurstCompatible]
    public struct FMODAudio : IValidation
    {
        [NativeDisableUnsafePtrRestriction]
        internal unsafe LowLevel.AudioHandler* audioHandler;
        internal unsafe ref LowLevel.AudioHandler refHandler => ref *audioHandler;

        internal readonly FMOD.Studio.EventDescription eventDescription;
        internal FixedList4096Bytes<ParamReference> parameters;
        internal bool playQueued;

        private bool allowFadeout;
        private bool overrideAttenuation;
        private float overrideMinDistance, overrideMaxDistance;

        internal float3 _translation;
        internal quaternion _rotation;

        #region Readonly

        public bool HasInitialized
        {
            get
            {
                unsafe
                {
                    return audioHandler != null;
                }
            }
        }
        public FMOD.Studio.PLAYBACK_STATE PlaybackState
        {
            get
            {
                if (!HasInitialized)
                {
                    return FMOD.Studio.PLAYBACK_STATE.STOPPED;
                }

                refHandler.instance.getPlaybackState(out var state);
                return state;
            }
        }
        public bool Is3D
        {
            get
            {
#if DEBUG_MODE
                if (!IsValidID())
                {
                    throw new System.Exception();
                }
#endif
                eventDescription.is3D(out bool value);
                return value;
            }
        }

        #endregion

        #region Properties

        public bool AllowFadeout
        {
            get => allowFadeout;
            set => allowFadeout = value;
        }
        public bool OverrideAttenuation
        {
            get => overrideAttenuation;
            set => overrideAttenuation = value;
        }
        public float OverrideMinDistance
        {
            get => overrideMinDistance;
            set => OverrideMinDistance = value;
        }
        public float OverrideMaxDistance
        {
            get => overrideMaxDistance;
            set => overrideMaxDistance = value;
        }

        public float3 position
        {
            get
            {
                if (HasInitialized) return refHandler.translation;

                return _translation;
            }
        }
        public quaternion rotation
        {
            get
            {
                if (HasInitialized) return refHandler.rotation;

                return _rotation;
            }
        }

        public float volume
        {
            get
            {
#if DEBUG_MODE
                if (!IsValid())
                {
                    Collections.Point.LogError(Collections.Point.LogChannel.Audio,
                        $"This audio has an invalid but trying access. " +
                        $"This is not allowed.");

                    return -1;
                }
#endif
                refHandler.instance.getVolume(out float vol);
                return vol;
            }
            set
            {
#if DEBUG_MODE
                if (!IsValid())
                {
                    Collections.Point.LogError(Collections.Point.LogChannel.Audio,
                        $"This audio has an invalid but trying access. " +
                        $"This is not allowed.");

                    return;
                }
#endif
                refHandler.instance.setVolume(value);
            }
        }

        #endregion

        #region Constructors

        [BurstCompatible]
        internal unsafe FMODAudio(FMOD.Studio.EventDescription desc)
        {
            audioHandler = null;

            eventDescription = desc;
            parameters = new FixedList4096Bytes<ParamReference>();
            playQueued = false;

            allowFadeout = true;
            overrideAttenuation = false;
            overrideMinDistance = -1;
            overrideMaxDistance = -1;

            _translation = 0;
            _rotation = quaternion.identity;
        }
        [BurstCompatible]
        public FMODAudio(FMODUnity.EventReference eventRef)
        {
            this = FMODManager.GetAudio(eventRef);
        }
        [NotBurstCompatible]
        public FMODAudio(string eventPath)
        {
            this = FMODManager.GetAudio(eventPath);
        }

        #endregion

        #region Parameter

        [NotBurstCompatible]
        public bool HasParameter(string name)
        {
            ParamReference temp = new ParamReference(name);
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].Equals(temp)) return true;
            }

            return false;
        }
        public bool HasParameter(ParamReference parameter)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].Equals(parameter))
                {
                    if (parameters[i].value == parameter.value &&
                        parameters[i].ignoreSeekSpeed == parameter.ignoreSeekSpeed)
                    {
                        return true;
                    }

                    return false;
                }
            }

            return false;
        }
        [NotBurstCompatible]
        public void SetParameter(string name, float value)
        {
#if DEBUG_MODE
            if (!IsValid())
            {
                Collections.Point.LogError(Collections.Point.LogChannel.Audio,
                    $"This audio has an invalid but trying access. " +
                    $"This is not allowed.");

                return;
            }
#endif
            int index = -1;

            ParamReference parameter = new ParamReference(name, value);
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].Equals(parameter))
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
            {
                parameters.Add(parameter);
            }
            else
            {
                ref ParamReference temp = ref parameters.ElementAt(index);
                temp = parameter;
            }

            if (IsValid())
            {
                refHandler.instance.setParameterByID(parameter.id, parameter.value, parameter.ignoreSeekSpeed);
            }
        }
        public void SetParameter(ParamReference parameter)
        {
#if DEBUG_MODE
            if (!IsValid())
            {
                Collections.Point.LogError(Collections.Point.LogChannel.Audio,
                    $"This audio has an invalid but trying access. " +
                    $"This is not allowed.");

                return;
            }
#endif
            int index = -1;

            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].Equals(parameter))
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
            {
                parameters.Add(parameter);
            }
            else
            {
                ref ParamReference temp = ref parameters.ElementAt(index);
                temp = parameter;
            }

            if (IsValid())
            {
                refHandler.instance.setParameterByID(parameter.id, parameter.value, parameter.ignoreSeekSpeed);
            }
        }
        [NotBurstCompatible]
        public void RemoveParameter(string name)
        {
            ParamReference temp = new ParamReference(name);
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].Equals(temp))
                {
                    parameters.RemoveAt(i);
                    return;
                }
            }
        }
        public void RemoveParameter(ParamReference parameter)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].Equals(parameter))
                {
                    parameters.RemoveAt(i);
                    return;
                }
            }
        }

        #endregion

        #region Validation

        /// <summary>
        /// 유효한 ID 를 가진 이벤트인지 반환합니다.
        /// </summary>
        /// <returns></returns>
        public bool IsValidID() => eventDescription.isValid();
        public bool IsValid()
        {
            unsafe
            {
                return eventDescription.isValid() && audioHandler != null;
            }
        }

        #endregion

        public void Play()
        {
#if DEBUG_MODE
            if (!IsValidID())
            {
                Collections.Point.LogError(Collections.Point.LogChannel.Audio,
                    $"This audio has an invalid FMOD id but trying to play. " +
                    $"This is not allowed.");

                return;
            }
#endif
            FMODManager.Play(ref this);
        }
        public void Stop()
        {
#if DEBUG_MODE
            if (!IsValid())
            {
                Collections.Point.LogError(Collections.Point.LogChannel.Audio,
                    $"This audio has an invalid but trying to play. " +
                    $"This is not allowed.");

                return;
            }
#endif
            FMODManager.Stop(ref this);
        }
    }
}
