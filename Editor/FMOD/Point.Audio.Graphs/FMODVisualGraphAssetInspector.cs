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

using GraphProcessor;
using Point.Audio.Graphs;
using UnityEditor;
using UnityEngine.UIElements;

namespace Point.Audio.FMODEditor
{
    [CustomEditor(typeof(FMODVisualGraph), true)]
    public class FMODVisualGraphAssetInspector : GraphInspector
    {
        // protected override void CreateInspector()
        // {
        // }

        protected override void CreateInspector()
        {
            base.CreateInspector();

            root.Add(new Button(() => EditorWindow.GetWindow<FMODVisualGraphWindow>().InitializeGraph(target as BaseGraph))
            {
                text = "Open base graph window"
            });
            //root.Add(new Button(() => EditorWindow.GetWindow<CustomContextMenuGraphWindow>().InitializeGraph(target as BaseGraph))
            //{
            //    text = "Open custom context menu graph window"
            //});
            //root.Add(new Button(() => EditorWindow.GetWindow<CustomToolbarGraphWindow>().InitializeGraph(target as BaseGraph))
            //{
            //    text = "Open custom toolbar graph window"
            //});
            //root.Add(new Button(() => EditorWindow.GetWindow<ExposedPropertiesGraphWindow>().InitializeGraph(target as BaseGraph))
            //{
            //    text = "Open exposed properties graph window"
            //});
        }
    }
}
