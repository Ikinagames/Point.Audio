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
using Unity.Mathematics;

namespace Point.Audio.UnityFMOD
{
    public struct FMODAudio : IValidation
    {
        internal unsafe UnityFMOD.AudioHandler* audioHandler;
        internal unsafe ref UnityFMOD.AudioHandler refHandler => ref *audioHandler;

        internal readonly FMOD.Studio.EventDescription eventDescription;
        internal FixedList4096Bytes<ParamReference> parameters;

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

        #endregion

        internal unsafe FMODAudio(FMOD.Studio.EventDescription desc)
        {
            audioHandler = null;

            eventDescription = desc;
            parameters = new FixedList4096Bytes<ParamReference>();

            allowFadeout = true;
            overrideAttenuation = false;
            overrideMinDistance = -1;
            overrideMaxDistance = -1;

            _translation = 0;
            _rotation = quaternion.identity;
        }

        #region Parameter

        public bool HasParameter(string name)
        {
            ParamReference temp = new ParamReference(name);
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].Equals(temp)) return true;
            }

            return false;
        }
        public void AddParameter(string name, float value)
        {
#if DEBUG_MODE
            if (HasParameter(name))
            {
                Collections.Point.LogError(Collections.Point.LogChannel.Audio,
                    $"Parameter({name}) already added in this audio.");

                return;
            }
#endif
            ParamReference param = new ParamReference(name, value);
            parameters.Add(param);
        }
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
        public ref ParamReference GetParameter(string name)
        {
            int index = -1;

            ParamReference temp = new ParamReference(name);
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].Equals(temp))
                {
                    index = i;
                    break;
                }
            }
#if DEBUG_MODE
            if (index < 0)
            {
                throw new System.Exception();
            }
#endif
            return ref parameters.ElementAt(index);
        }

        #endregion

        #region Validation

        /// <summary>
        /// 유효한 ID 를 가진 이벤트인지 반환합니다.
        /// </summary>
        /// <remarks>
        /// <seealso cref="FMODManager.GetAudio(FMODUnity.EventReference)"/> 로 받아올 수 있습니다.
        /// </remarks>
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
    }
}
