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

using Point.Collections;
using Point.Collections.Events;
using UnityEngine;

namespace Point.Audio
{
    public sealed class PlayAudioEvent : SynchronousEvent<PlayAudioEvent>
    {
        private Vector3 INIT_POSITION = new Vector3(-9999999, 0, -99999999);

        protected override bool EnableLog => false;

        public const string Unhandled = "UNHANDLED";

        public AudioKey Key { get; private set; }
        public Vector3 Position { get; private set; }
        public bool HasPosition => !Position.Equals(INIT_POSITION);

        public static PlayAudioEvent GetEvent(AudioKey key)
        {
            var ev = Dequeue();

            ev.Key = key;

            return ev;
        }
        public static PlayAudioEvent GetEvent(AudioKey key, Vector3 position)
        {
            var ev = Dequeue();

            ev.Key = key;
            ev.Position = position;

            return ev;
        }
        protected override void OnReserve()
        {
            Key = Hash.Empty;
            Position = INIT_POSITION;
        }
    }
}