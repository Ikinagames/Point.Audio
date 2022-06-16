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

using Point.Collections.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Point.Audio.FMODEditor
{
    [CustomPropertyDrawer(typeof(FMODAnimationEvent))]
    internal sealed class FMODAnimationEventPropertyDrawer : PropertyDrawerUXML<FMODAnimationEvent>
    {
        protected override VisualElement CreateVisualElement(SerializedProperty property)
        {
            VisualElement root = new VisualElement();
            root.styleSheets.Add(CoreGUI.VisualElement.DefaultStyleSheet);
            root.AddToClassList("content-border");

            Label label = new Label(property.displayName);
            label.name = "H3-Label";
            label.style.marginTop = 2;
            label.style.marginBottom = 2;
            root.Add(label);

            VisualElement contentContainer = new VisualElement();
            contentContainer.AddToClassList("content-container");
            contentContainer.AddToClassList("inner-container");
            root.Add(contentContainer);

            PropertyField nameField = CoreGUI.VisualElement.PropertyField(property.FindPropertyRelative("m_Name"));
            PropertyField evField = CoreGUI.VisualElement.PropertyField(property.FindPropertyRelative("m_AudioReference"));

            contentContainer.Add(nameField);
            contentContainer.Add(evField);

            label.RegisterCallback<MouseDownEvent>(t =>
            {
                property.isExpanded = !property.isExpanded;
                contentContainer.style.Hide(!property.isExpanded);

                property.serializedObject.ApplyModifiedProperties();
            });
            contentContainer.style.Hide(!property.isExpanded);

            nameField.RegisterValueChangeCallback(t =>
            {
                string changedName = t.changedProperty.stringValue;
                label.text = changedName;
            });

            return root;
        }
    }
}
