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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEBUG_MODE
#endif

using UnityEngine;
using Point.Collections;
using Point.Collections.ResourceControl;

namespace Point.Audio
{
    [AddComponentMenu("")]
    public sealed class AudioManager : StaticMonobehaviour<AudioManager>, IStaticInitializer
    {
        public override bool HideInInspector => true;

        public override void OnInitialze()
        {
            AudioData audioDatastore = AudioData.Instance;
            ResourceAddresses addresses = ResourceAddresses.Instance;
        }
    }

    public sealed class AudioData : StaticScriptableObject<AudioData>
    {
        //private sealed class AudioDatastore : Datastore<AudioDataProvider>
        //{
        //    public AudioDatastore(AudioDataProvider provider) : base(provider)
        //    {
                    
        //    }
        //}
        //private sealed class AudioDataProvider : DataProvider<AssetBundleStrategy>
        //{
        //    protected override void OnInitialize()
        //    {
        //        base.OnInitialize();
        //    }
        //}

        //private AudioDataProvider m_DataProvider;
        //private AudioDatastore m_Datastore;

        //protected override void OnInitialize()
        //{
        //    m_DataProvider = new AudioDataProvider();
        //    m_Datastore = new AudioDatastore(m_DataProvider);


        //}
        //public void CheckIntegrity()
        //{

        //}
    }
}
