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

using UnityEngine;
using Point.Collections;
using System.Collections.Generic;
using static Unity.Mathematics.math;

namespace Point.Audio
{
    [AddComponentMenu("")]
    internal sealed class AudioFadeController : StaticMonobehaviour<AudioFadeController>
    {
        protected override bool EnableLog => false;
        protected override bool HideInInspector => true;

        private readonly List<Payload> m_VolumePayloads = new List<Payload>();

        private struct Payload
        {
            public IFadeable audio;
            public object current, target;
            
            public Timer startTime;
            public float time;
        }

        public void Volume(in IFadeable audio, in float target, in float time)
        {
            Payload payload = new Payload
            {
                audio = audio,
                current = audio.value,
                target = target,

                startTime = Timer.Start(),
                time = time,
            };
            m_VolumePayloads.Add(payload);
        }

        private void LateUpdate()
        {
            for (int i = m_VolumePayloads.Count - 1; i >= 0; i--)
            {
                Payload payload = m_VolumePayloads[i];
                if (payload.startTime.ElapsedTime >= payload.time)
                {
                    payload.audio.SetValue(payload.current, payload.target, 1);

                    m_VolumePayloads.RemoveAt(i);
                    continue;
                }

                float t = payload.startTime.ElapsedTime / payload.time;
                //float value = lerp(payload.current, payload.target, payload.startTime.ElapsedTime / payload.time);

                //payload.audio.volume = value;
                payload.audio.SetValue(payload.current, payload.target, t);
            }
        }
    }
}