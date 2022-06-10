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

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Point.Audio.Timeline
{
    [CreateAssetMenu(menuName = "Point/Create Test Playable Asset")]
    // https://docs.unity3d.com/ScriptReference/Playables.PlayableAsset.html
    public class TestFMODPlayableAsset : TimelineAsset
    {
        // https://docs.unity3d.com/ScriptReference/ExposedReference_1.html
        //This allows you to use GameObjects in your Scene
        public ExposedReference<GameObject> m_MySceneObject;
        //This variable allows you to decide the velocity of your GameObject
        public Vector3 m_SceneObjectVelocity;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            Playable playable = base.CreatePlayable(graph, owner);
            return playable;

            //Get access to the Playable Behaviour script
            TestExample playableBehaviour = new TestExample();
            //Resolve the exposed reference on the Scene GameObject by returning the table used by the graph
            playableBehaviour.m_MySceneObject = m_MySceneObject.Resolve(graph.GetResolver());

            //Make the PlayableBehaviour velocity variable the same as the variable you set
            playableBehaviour.m_SceneObjectVelocity = m_SceneObjectVelocity;

            //Create a custom playable from this script using the Player Behaviour script
            return ScriptPlayable<TestExample>.Create(graph, playableBehaviour);
        }

        // https://docs.unity3d.com/ScriptReference/Playables.PlayableBehaviour.html
        public class TestExample : PlayableBehaviour
        {
            public GameObject m_MySceneObject;
            public Vector3 m_SceneObjectVelocity;

            public override void PrepareFrame(Playable playable, FrameData frameData)
            {
                //If the Scene GameObject exists, move it continuously until the Playable pauses
                if (m_MySceneObject != null)
                    //Move the GameObject using the velocity you set in your Playable Track's inspector
                    m_MySceneObject.transform.Translate(m_SceneObjectVelocity);
            }
        }
    }
}
