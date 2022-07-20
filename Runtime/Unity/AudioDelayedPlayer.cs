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

namespace Point.Audio
{
    [AddComponentMenu("")]
    internal sealed class AudioDelayedPlayer : StaticMonobehaviour<AudioDelayedPlayer>
    {
        private static void Play(Audio audio) => audio.Play();

        protected override bool EnableLog => false;
        protected override bool HideInInspector => true;

        private readonly List<Payload> m_Payloads = new List<Payload>();

        public void Register(Audio audio, float delay, AudioCallback callback)
        {
            Payload payload = new Payload
            {
                audio = audio,
                delay = delay,
                startTime = Timer.Start(),
                callback = callback
            };
            m_Payloads.Add(payload);
        }
        public void DelayedPlay(Audio audio, float delay)
        {
            Register(audio, delay, Play);
        }

        private void LateUpdate()
        {
            for (int i = m_Payloads.Count - 1; i >= 0; i--)
            {
                Payload payload = m_Payloads[i];
                if (!payload.startTime.IsExceeded(payload.delay)) continue;

                payload.callback?.Invoke(payload.audio);

                m_Payloads.RemoveAt(i);
            }
        }
        protected override void OnShutdown()
        {
            for (int i = 0; i < m_Payloads.Count; i++)
            {
                m_Payloads[i].audio.Reserve();
            }
            m_Payloads.Clear();
        }

        private struct Payload
        {
            public Audio audio;
            public float delay;
            public Timer startTime;
            public AudioCallback callback;
        }
    }
}