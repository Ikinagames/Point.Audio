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

using FMOD.Studio;
using System;

namespace Point.Audio
{
    /// <summary>
    /// FMOD snapshot
    /// </summary>
    public readonly struct Snapshot : IDisposable
    {
        private readonly EventDescription m_Description;
        private readonly EventInstance m_Instance;

        internal Snapshot(EventDescription desc)
        {
            m_Description = desc;
            m_Description.createInstance(out m_Instance);
        }
        public void Dispose()
        {
            m_Instance.stop(STOP_MODE.ALLOWFADEOUT);
            m_Instance.release();
            m_Instance.clearHandle();
        }

        public void Start()
        {
            m_Instance.start();
        }
        public void Stop()
        {
            m_Instance.stop(STOP_MODE.ALLOWFADEOUT);
        }
    }
}
