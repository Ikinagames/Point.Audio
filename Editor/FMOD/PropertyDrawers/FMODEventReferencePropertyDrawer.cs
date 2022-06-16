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

using log4net.Filter;
using Point.Collections.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace Point.Audio.FMODEditor
{
    [CustomPropertyDrawer(typeof(FMODEventReference))]
    public sealed class FMODEventReferencePropertyDrawer : PropertyDrawerUXML<FMODEventReference>
    {
        private sealed class Helper
        {
            const string
                c_Asset = "m_Asset",

                c_Event = "m_Event",
                c_Parameters = "m_Parameters",

                c_AllowFadeOut = "m_AllowFadeOut",
                c_OverrideAttenuation = "m_OverrideAttenuation",
                c_OverrideMinDistance = "m_OverrideMinDistance", c_OverrideMaxDistance = "m_OverrideMaxDistance",

                c_ExposeGlobalEvent = "m_ExposeGlobalEvent",
                c_ExposedName = "m_ExposedName";

            public static GUIContent
                EventContent = new GUIContent("Event",
                    ""),
                ParametersContent = new GUIContent("Parameters",
                    ""),

                AllowFadeOutContent = new GUIContent("AllowFadeOut"),
                OverrideAttenuationContent = new GUIContent("OverrideAttenuation"),
                OverrideDistanceContent = new GUIContent("OverrideDistance"),

                ExposeGlobalEventContent = new GUIContent("ExposeGlobalEvent"),
                ExposedNameContent = new GUIContent("ExposedName",
                    "");

            public static SerializedProperty GetAssetField(SerializedProperty property)
                => property.FindPropertyRelative(c_Asset);

            public static SerializedProperty GetEventField(SerializedProperty property)
                => property.FindPropertyRelative(c_Event);
            public static SerializedProperty GetParametersField(SerializedProperty property)
                => property.FindPropertyRelative(c_Parameters);

            public static SerializedProperty GetOverrideAttenField(SerializedProperty property)
                => property.FindPropertyRelative(c_OverrideAttenuation);
            public static SerializedProperty GetOverrideAttenMinField(SerializedProperty property)
                => property.FindPropertyRelative(c_OverrideMinDistance);
            public static SerializedProperty GetOverrideAttenMaxField(SerializedProperty property)
                => property.FindPropertyRelative(c_OverrideMaxDistance);

            public static SerializedProperty GetExposeGlobalEventField(SerializedProperty property)
                => property.FindPropertyRelative(c_ExposeGlobalEvent);
            public static SerializedProperty GetExposedNameField(SerializedProperty property)
                => property.FindPropertyRelative(c_ExposedName);

            const string c_Snapshot = "snapshot:/", c_Path = "Path";

            public static bool IsEventEmpty(SerializedProperty property)
            {
                return string.IsNullOrEmpty(GetEventField(property).FindPropertyRelative(c_Path).stringValue);
            }
            public static bool IsSnapshot(SerializedProperty property)
            {
                string path = GetEventField(property).FindPropertyRelative(c_Path).stringValue;
                return path.StartsWith(c_Snapshot);
            }
        }

        protected override VisualElement CreateVisualElement(SerializedProperty property)
        {
            VisualElement root = new VisualElement();
            root.styleSheets.Add(CoreGUI.VisualElement.DefaultStyleSheet);
            root.AddToClassList("content-container");

            SerializedProperty assetProp = Helper.GetAssetField(property);
            bool useAsset = assetProp.isExpanded;

            VisualElement headerContainer = new VisualElement();
            headerContainer.style.flexDirection = FlexDirection.Row;
            {
                Label headerLabel = new Label(property.displayName);
                headerLabel.name = "H3-Label";
                headerLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
                headerContainer.Add(headerLabel);

                ToolbarToggle toggle = new ToolbarToggle();
                toggle.name = "UseAssetToogle";
                toggle.value = useAsset;
                toggle.label = useAsset ? "Use Data" : "Use Asset";

                headerContainer.Add(toggle);
            }
            root.Add(headerContainer);

            PropertyField evField = CoreGUI.VisualElement.PropertyField(Helper.GetEventField(property));
            evField.name = "EventField";
            evField.style.paddingLeft = 12;
            evField.style.Hide(useAsset);
            root.Add(evField);

            PropertyField assetField = CoreGUI.VisualElement.PropertyField(assetProp);
            assetField.name = "AssetField";
            assetField.style.paddingTop = 8;
            assetField.style.Hide(!useAsset);
            root.Add(assetField);

            VisualElement contentContainer = new VisualElement();
            contentContainer.name = "ContentContainer";
            contentContainer.AddToClassList("content-container");
            contentContainer.AddToClassList("inner-container");
            root.Add(contentContainer);

            VisualElement dataContainer = new VisualElement();
            dataContainer.name = "DataContainer";
            dataContainer.style.Hide(useAsset);
            contentContainer.Add(dataContainer);

            AddContents(property, dataContainer);

            return root;
        }
        protected override void SetupVisualElement(SerializedProperty property, VisualElement root)
        {
            SerializedProperty assetProp = Helper.GetAssetField(property);
            bool useAsset = assetProp.isExpanded;

            VisualElement contentContainer = root.Q("ContentContainer");
            if (!useAsset)
            {
                contentContainer.style.Hide(!property.isExpanded);
            }
            else contentContainer.style.Hide(true);

            root[0].RegisterCallback<MouseDownEvent>(t =>
            {
                property.isExpanded = !property.isExpanded;
                property.serializedObject.ApplyModifiedProperties();

                bool useAsset = assetProp.isExpanded;
                if (!useAsset)
                {
                    contentContainer.style.Hide(!property.isExpanded);
                }
            });

            ToolbarToggle toggle = root.Q<ToolbarToggle>("UseAssetToogle");
            toggle.RegisterValueChangedCallback(t =>
            {
                assetProp.isExpanded = t.newValue;
                toggle.label = t.newValue ? "Use Data" : "Use Asset";

                root.Q<PropertyField>("AssetField").style.Hide(!t.newValue);
                root.Q<PropertyField>("EventField").style.Hide(t.newValue);

                // use asset
                if (t.newValue)
                {
                    root.Q("ContentContainer").style.Hide(true);
                }
                else
                {
                    root.Q("ContentContainer").style.Hide(!property.isExpanded);
                }

                assetProp.serializedObject.ApplyModifiedProperties();
                toggle.MarkDirtyRepaint();
            });
        }
        private void AddContents(SerializedProperty property, VisualElement contentContainer)
        {
            contentContainer.Add(
                CoreGUI.VisualElement.PropertyField(Helper.GetParametersField(property)));

            contentContainer.Add(CoreGUI.VisualElement.Space());
            contentContainer.Add(
                CoreGUI.VisualElement.PropertyField(Helper.GetOverrideAttenField(property)));
            contentContainer.Add(
                CoreGUI.VisualElement.PropertyField(Helper.GetOverrideAttenMinField(property)));
            contentContainer.Add(
                CoreGUI.VisualElement.PropertyField(Helper.GetOverrideAttenMaxField(property)));

            contentContainer.Add(CoreGUI.VisualElement.Space());
            contentContainer.Add(
                CoreGUI.VisualElement.PropertyField(Helper.GetExposeGlobalEventField(property)));
            contentContainer.Add(
                CoreGUI.VisualElement.PropertyField(Helper.GetExposedNameField(property)));
        }
    }
}
