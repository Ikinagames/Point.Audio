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

//using GraphProcessor;
//using UnityEditor;
//using UnityEditor.Experimental.GraphView;
//using UnityEngine;
//using UnityEngine.UIElements;

//namespace Point.Audio.FMODEditor
//{
//    public class FMODVisualGraphWindow : BaseGraphWindow
//    {
//        protected override void InitializeWindow(BaseGraph graph)
//        {
//            // Set the window title
//            titleContent = new GUIContent("Default Graph");

//            // Here you can use the default BaseGraphView or a custom one (see section below)
//            var graphView = new FMODVisualGraphView(this);

//            GridBackground background = new GridBackground();
//            graphView.Insert(0, background);
//            background.StretchToParentSize();

//            graphView.Add(new MiniMapView(graphView));

//            rootView.Add(graphView);
//        }
//    }
//    public class FMODVisualGraphView : BaseGraphView
//    {
//        public FMODVisualGraphView(EditorWindow window) : base(window)
//        {
//        }

//        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
//        {
//            //evt.menu.AppendSeparator();

//            //evt.menu.AppendAction("To Json",
//            //    e =>
//            //    {

//            //    },
//            //    status: DropdownMenuAction.Status.Normal);
//            //evt.menu.AppendSeparator();

//            BuildStackNodeContextualMenu(evt);

//            base.BuildContextualMenu(evt);
//        }
//        /// <summary>
//        /// Add the New Stack entry to the context menu
//        /// </summary>
//        /// <param name="evt"></param>
//        protected void BuildStackNodeContextualMenu(ContextualMenuPopulateEvent evt)
//        {
//            Vector2 position = (evt.currentTarget as VisualElement).ChangeCoordinatesTo(contentViewContainer, evt.localMousePosition);
//            evt.menu.AppendAction("Create Stack", (e) => AddStackNode(new BaseStackNode(position)), DropdownMenuAction.AlwaysEnabled);
//        }
//    }
//}
