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

using Point.Audio.LowLevel;
using Point.Collections;
using Point.Collections.Buffer.LowLevel;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Point.Audio
{
    [BurstCompatible]
    public struct Audio : IFMODEvent, IValidation
    {
        private readonly UnsafeAudioHandlerContainer audioHandlerBuffer;
        private Hash audioHandlerHash;
        internal UnsafeReference<UnsafeAudioHandler> audioHandler
        {
            get
            {
                if (audioHandlerHash.IsEmpty())
                {
                    return default(UnsafeReference<UnsafeAudioHandler>);
                }
                UnsafeReference<UnsafeAudioHandler> handler 
                    = audioHandlerBuffer.Data.GetAudioHandler(audioHandlerHash);
#if DEBUG_MODE
                if (!handler.Value.hash.Equals(audioHandlerHash))
                {
                    PointHelper.LogError(Channel.Audio,
                        $"Assertion faild. Handler is not same. {handler.Value.hash.Value} != {audioHandlerHash.Value}");
                }
#endif
                return handler;
            }
        }


        internal FMOD.Studio.EventDescription eventDescription;
        internal FixedList4096Bytes<ParamReference> parameters;
        internal readonly Hash hash;

        private bool allowFadeout;
        private bool overrideAttenuation;
        private float overrideMinDistance, overrideMaxDistance;

        internal float3 _translation;
        internal quaternion _rotation;

        #region Readonly

        public bool HasInitialized => audioHandler.IsCreated;
        public FMOD.Studio.PLAYBACK_STATE PlaybackState
        {
            get
            {
                if (!HasInitialized)
                {
                    return FMOD.Studio.PLAYBACK_STATE.STOPPED;
                }

                audioHandler.Value.instance.getPlaybackState(out var state);
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

        /// <summary>
        /// <see cref="GetUserPropertyByIndex"/>
        /// </summary>
        public int UserPropertyCount
        {
            get
            {
                FMOD.RESULT result = eventDescription.getUserPropertyCount(out int count);
#if DEBUG_MODE
                if (result != FMOD.RESULT.OK)
                {
                    PointHelper.LogError(Channel.Audio,
                        $"Err. {result}");
                }
#endif
                return count;
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
            set => overrideMinDistance = value;
        }
        public float OverrideMaxDistance
        {
            get => overrideMaxDistance;
            set => overrideMaxDistance = value;
        }

        public FMOD.Studio.EventDescription EventDescription => eventDescription;
        public float3 position
        {
            get
            {
                if (HasInitialized) return audioHandler.Value.translation;

                return _translation;
            }
            set
            {
                _translation = value;

                if (HasInitialized) audioHandler.Value.translation = value;
            }
        }
        public quaternion rotation
        {
            get
            {
                if (HasInitialized) return audioHandler.Value.rotation;

                return _rotation;
            }
            set
            {
                _rotation = value;

                if (HasInitialized) audioHandler.Value.rotation = value;
            }
        }
        public UnityEngine.Transform bindTransform
        {
            set
            {
                if (!IsValid(true))
                {
                    PointHelper.LogError(Channel.Audio,
                        $"This audio is invalid. " +
                        $"Transform binding is not possible before Play() method executed.");
                    return;
                }
                FMODUnity.RuntimeManager.AttachInstanceToGameObject(audioHandler.Value.instance, value);
            }
        }

        public float volume
        {
            get
            {
#if DEBUG_MODE
                if (!IsValid())
                {
                    PointHelper.LogError(Channel.Audio,
                        $"This audio has an invalid but trying access. " +
                        $"This is not allowed.");

                    return -1;
                }
#endif
                audioHandler.Value.instance.getVolume(out float vol);
                return vol;
            }
            set
            {
#if DEBUG_MODE
                if (!IsValid())
                {
                    PointHelper.LogError(Channel.Audio,
                        $"This audio has an invalid but trying access. " +
                        $"This is not allowed.");

                    return;
                }
#endif
                audioHandler.Value.instance.setVolume(value);
            }
        }

        #endregion

        #region Constructors

        [BurstCompatible]
        internal unsafe Audio(UnsafeAudioHandlerContainer handlerBuffer, FMOD.Studio.EventDescription desc)
        {
            audioHandlerBuffer = handlerBuffer;
            audioHandlerHash = Hash.Empty;

            eventDescription = desc;
            parameters = new FixedList4096Bytes<ParamReference>();
            hash = Hash.NewHash();

            allowFadeout = true;
            overrideAttenuation = false;
            overrideMinDistance = -1;
            overrideMaxDistance = -1;

            _translation = 0;
            _rotation = quaternion.identity;
        }
        [BurstCompatible]
        public Audio(FMODUnity.EventReference eventRef)
        {
            this = FMODManager.GetAudio(eventRef);
        }
        [NotBurstCompatible]
        public Audio(string eventPath)
        {
            this = FMODManager.GetAudio(eventPath);
        }

        internal unsafe void SetEvent(in FMOD.Studio.EventDescription desc)
        {
            if (!audioHandlerHash.IsEmpty())
            {
                throw new Exception();
            }

            audioHandlerHash = Hash.Empty;

            eventDescription = desc;
            parameters = new FixedList4096Bytes<ParamReference>();
        }

        #endregion

        #region Parameter

        [NotBurstCompatible]
        public bool HasParameter(string name)
        {
            ParamReference temp = new ParamReference(eventDescription, name);
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
            if (!IsValidID())
            {
                PointHelper.LogError(Channel.Audio,
                    $"This audio has an invalid but trying access. " +
                    $"This is not allowed.");

                return;
            }
#endif
            int index = -1;

            ParamReference parameter = new ParamReference(eventDescription, name, value);
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
                audioHandler.Value.instance.setParameterByID(parameter.description.id, parameter.value, parameter.ignoreSeekSpeed);
            }
        }
        public void SetParameter(ParamReference parameter)
        {
#if DEBUG_MODE
            if (!IsValidID())
            {
                PointHelper.LogError(Channel.Audio,
                    $"This audio has an invalid but trying access. " +
                    $"This is not allowed.");

                return;
            }
            else if (parameter.isGlobal)
            {
                PointHelper.LogError(Channel.Audio,
                    $"This parameter({(string)parameter.description.name}) is global parameter. " +
                    $"Cannot be setted to an instance event.");
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
                var result = audioHandler.Value.instance
                    .setParameterByID(
                    parameter.description.id, 
                    parameter.value, 
                    parameter.ignoreSeekSpeed);

                if (result != FMOD.RESULT.OK)
                {
                    eventDescription.getPath(out string evPath);
                    PointHelper.LogError(Channel.Audio,
                        $"Set parameter({parameter.description.name} : {parameter.value}) faild with {result} at Audio({evPath}).");
                }
            }
        }
        [NotBurstCompatible]
        public void RemoveParameter(string name)
        {
            //ParamReference temp = new ParamReference(eventDescription, name);
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].Equals(name))
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

        [NotBurstCompatible]
        public ParamReference GetParameter(string name)
        {
            if (!IsValidID())
            {
                PointHelper.LogError(Channel.Audio,
                    $"This audio is invalid.");

                return default(ParamReference);
            }

            return new ParamReference(eventDescription, name);
        }

        public PropertyEnumerator GetPropertyEnumerator() => new PropertyEnumerator(eventDescription);
        public FMOD.Studio.USER_PROPERTY GetUserProperty(string name)
        {
            eventDescription.getUserProperty(name, out FMOD.Studio.USER_PROPERTY property);

            return property;
        }
        public FMOD.Studio.USER_PROPERTY GetUserPropertyByIndex(int index)
        {
            eventDescription.getUserPropertyByIndex(index, out FMOD.Studio.USER_PROPERTY property);
            return property;
        }

        #endregion

        #region Validation

        /// <summary>
        /// 유효한 ID 를 가진 이벤트인지 반환합니다.
        /// </summary>
        /// <returns></returns>
        public bool IsValidID() => eventDescription.isValid();
        public bool IsValid() => IsValid(false);
        /// <summary>
        /// 이 오디오가 유효한 ID를 가지고있고, 핸들러에 의해 관리되고 있는지 반환합니다.
        /// </summary>
        /// <returns></returns>
        public bool IsValid(bool log)
        {
            unsafe
            {
                if (!eventDescription.isValid())
                {
                    if (log) "desc not valid".ToLog();
                    return false;
                }
                else if (!audioHandler.IsCreated)
                {
                    if (log) "handler is not valid".ToLog();
                    return false;
                }

                bool result = audioHandler.Value.ValidateAudio(in this);
                if (!result && log)
                {
                    $"not valid handler {audioHandler.Value.instanceHash.Value} != {hash.Value}".ToLog();
                    $"{audioHandler.Value.hash.Value} :: {audioHandlerHash.Value}".ToLog();
                }
                return result;
            }
        }

        #endregion

        #region Internal

        internal void SetAudioHandler(UnsafeReference<UnsafeAudioHandler> handler)
        {
            audioHandlerHash = handler.Value.hash;
        }

        #endregion

        /// <inheritdoc cref="FMODManager.CreateInstance(ref Audio)"/>
        public void CreateInstance()
        {
#if DEBUG_MODE
            if (!IsValidID())
            {
                PointHelper.LogError(Channel.Audio,
                    $"This audio has an invalid FMOD id but trying to play. " +
                    $"This is not allowed.");

                return;
            }
#endif
            FMODManager.CreateInstance(ref this);
        }
        /// <inheritdoc cref="FMODManager.Play(ref Audio)"/>
        public void Play()
        {
#if DEBUG_MODE
            if (!IsValidID())
            {
                PointHelper.LogError(Channel.Audio,
                    $"This audio has an invalid FMOD id but trying to play. " +
                    $"This is not allowed.");

                return;
            }
#endif
            FMODManager.Play(ref this);
        }
        /// <inheritdoc cref="FMODManager.Stop(ref Audio)"/>
        public void Stop()
        {
#if DEBUG_MODE
            if (!IsValid())
            {
                PointHelper.LogError(Channel.Audio,
                    $"This audio has an invalid but trying to play. " +
                    $"This is not allowed.");

                return;
            }
#endif
            FMODManager.Stop(ref this);
        }

        public struct PropertyEnumerator : IEnumerable<FMOD.Studio.USER_PROPERTY>, IEnumerator<FMOD.Studio.USER_PROPERTY>
        {
            private FMOD.Studio.EventDescription eventDescription;
            private readonly int m_Count;
            private int m_Current;

            public PropertyEnumerator(FMOD.Studio.EventDescription desc)
            {
                eventDescription = desc;
                desc.getUserPropertyCount(out m_Count);
                m_Current = 0;
            }

            public FMOD.Studio.USER_PROPERTY Current
            {
                get
                {
                    if (m_Count <= m_Current) return default(FMOD.Studio.USER_PROPERTY);

                    eventDescription.getUserPropertyByIndex(m_Current, out var property);
                    return property;
                }
            }
            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }
            public bool MoveNext()
            {
                m_Current++;

                if (m_Count <= m_Current)
                {
                    m_Current = 0;
                    return false;
                }
                return true;
            }
            void IEnumerator.Reset()
            {
                m_Current = 0;
            }

            IEnumerator<FMOD.Studio.USER_PROPERTY> IEnumerable<FMOD.Studio.USER_PROPERTY>.GetEnumerator() => this;
            IEnumerator IEnumerable.GetEnumerator() => this;
        }
    }
}
