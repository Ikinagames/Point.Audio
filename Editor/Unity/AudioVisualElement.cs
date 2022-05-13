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

#if UNITY_2019_1_OR_NEWER
#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !POINT_DISABLE_CHECKS
#define DEBUG_MODE
#endif
#define UNITYENGINE

using Point.Collections;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Point.Audio.Editor
{
    public class AudioVisualElement : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<AudioVisualElement, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_Hash = new UxmlStringAttributeDescription
            {
                name = "hash",
                defaultValue = string.Empty,
            };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                AudioVisualElement e = ve as AudioVisualElement;

                e.m_Hash = m_Hash.GetValueFromBag(bag, cc);
            }
        }

        private string m_Hash;

        public AudioVisualElement()
        {
            m_Hash = string.Empty;

            this.AddToClassList("point-text-element");
        }
        public AudioVisualElement(Audio audio)
        {
            //m_Hash = audio.audioKey;
        }
    }
}

#endif