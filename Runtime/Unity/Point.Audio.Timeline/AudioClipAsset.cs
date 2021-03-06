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

#if UNITY_EDITOR
#endif

using System;
using UnityEngine;
using UnityEngine.Playables;
using Point.Collections;
using UnityEngine.Timeline;

namespace Point.Audio
{
    [Serializable]
    public sealed class AudioClipAsset : PlayableAsset, ITimelineClipAsset
    {
        [SerializeField] private string m_DisplayName;
        [SerializeField] private AssetPathField<AudioClip> m_Clip = new AssetPathField<AudioClip>();
        [Decibel]
        [SerializeField] private float m_Volume = 1;

        [Space]
        [SerializeField] private bool m_FixedDuration;
        [SerializeField] private double m_Start, m_Duration;

        public override double duration => m_Start + m_Duration;

        public ClipCaps clipCaps => ClipCaps.Extrapolation | ClipCaps.Blending;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            AudioBehavior behavior = new AudioBehavior(this);
            ScriptPlayable<AudioBehavior> playable = ScriptPlayable<AudioBehavior>.Create(graph, behavior);

            return playable;
        }

        public sealed class AudioBehavior : PlayableBehaviour
        {
            private AssetPathField<AudioClip> m_Clip;
            private float m_Volume;

            private Audio m_Audio;

            public AudioBehavior() : this(null) { }
            public AudioBehavior(AudioClipAsset asset)
            {
                m_Clip = asset.m_Clip;
                m_Volume = asset.m_Volume;
            }

            public override void OnBehaviourPlay(Playable playable, FrameData info)
            {
                m_Audio = AudioManager.Play(m_Clip);
            }
        }
    }
}