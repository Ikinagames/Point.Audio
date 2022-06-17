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

using FMODUnity;
using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Point.Audio.Timeline
{
    [TrackColor(0.066f, 0.134f, 0.244f)]
    [TrackClipType(typeof(FMODPointEventPlayable))]
    [TrackBindingType(typeof(GameObject))]
    [DisplayName("FMOD/Point Event Track")]
    public class FMODPointTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            var director = go.GetComponent<PlayableDirector>();
            var trackTargetObject = director.GetGenericBinding(this) as GameObject;

            foreach (var clip in GetClips())
            {
                var playableAsset = clip.asset as FMODPointEventPlayable;

                if (playableAsset)
                {
                    playableAsset.TrackTargetObject = trackTargetObject;
                    playableAsset.OwningClip = clip;
                }
            }

            return base.CreateTrackMixer(graph, go, inputCount);
        }
    }

    [Serializable]
    public abstract class FMODPointEventPlayable : PlayableAsset, ITimelineClipAsset
    {
        public virtual ClipCaps clipCaps => ClipCaps.None;

        public GameObject TrackTargetObject { get; internal set; }
        public TimelineClip OwningClip { get; internal set; }
    }
    [Serializable]
    public abstract class FMODPointEventBehaviour : PlayableBehaviour
    {
    }

    public sealed class FMODPointPlay : FMODPointEventPlayable
    {
        private sealed class PlayBehaviour : FMODPointEventBehaviour
        {
            public PlayBehaviour()
            {

            }
        }

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            ScriptPlayable<PlayBehaviour> playable = ScriptPlayable<PlayBehaviour>.Create(graph);

            return playable;
        }
    }
}
