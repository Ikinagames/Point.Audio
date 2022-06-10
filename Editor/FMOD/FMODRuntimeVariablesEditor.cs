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

using UnityEditor;
using Point.Collections.Editor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Point.Audio.FMODEditor
{
    [CustomEditor(typeof(FMODRuntimeVariables))]
    public sealed class FMODRuntimeVariablesEditor : InspectorEditorUXML<FMODRuntimeVariables>
    {
        private SerializedProperty
            m_PlayOnStart,
            m_SceneDependencies;

        private void OnEnable()
        {
            m_PlayOnStart = GetSerializedProperty("m_PlayOnStart");
            m_SceneDependencies = GetSerializedProperty("m_SceneDependencies");
        }
        protected override bool ShouldHideOpenButton() => true;
    }
}
