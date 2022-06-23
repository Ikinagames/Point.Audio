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
using Point.Collections.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditor.IMGUI.Controls;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;

namespace Point.Audio.Editor
{
    [CustomEditor(typeof(AudioList))]
    internal sealed class AudioListEditor : InspectorEditorUXML<AudioList>
    {
        SerializedProperty
            m_FriendlyNamesProperty, m_DataProperty;
        FriendlyNameCollection m_FriendlyNames;
        List<Data> m_Data;

        #region Inner Classes

        private sealed class FriendlyName
        {
            public static GUIContent Header = new GUIContent("Friendly Names");
            public static string SearchText = string.Empty;

            private readonly SerializedProperty m_Property,
                m_FriendlyNameProperty, m_ClipProperty;

            public FriendlyName(SerializedProperty property)
            {
                m_Property = property;

                m_FriendlyNameProperty = property.FindPropertyRelative("m_FriendlyName");
                m_ClipProperty = property.FindPropertyRelative("m_AudioClip");
            }

            public string Name
            {
                get => m_FriendlyNameProperty.stringValue;
                set => m_FriendlyNameProperty.stringValue = value;
            }
            public AudioClip AudioClip
            {
                get
                {
                    return SerializedPropertyHelper.GetAssetPathField<AudioClip>(m_ClipProperty);
                }
                set
                {
                    SerializedPropertyHelper.SetAssetPathField(m_ClipProperty, value);
                }
            }

            public bool OnGUI()
            {
                if (!SearchText.IsNullOrEmpty())
                {
                    if (!Name.ToLower().Contains(SearchText.ToLower())) return false;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    Name = EditorGUILayout.TextField(GUIContent.none, Name);

                    AudioClip = (AudioClip)EditorGUILayout.ObjectField(GUIContent.none, AudioClip, TypeHelper.TypeOf<AudioClip>.Type, false, 
                        GUILayout.MinWidth(50), GUILayout.MaxWidth(180));
                }

                return true;
            }
        }
        private sealed class Data : TreeViewItem
        {
            public static GUIContent Header = new GUIContent("Data");
            private static int s_Index = 0;

            private readonly SerializedProperty m_Property,
                m_AudioClipProperty,
                
                m_PrefabProperty,
                m_GroupProperty,
                
                m_IgnoreTimeProperty,
                
                m_OnPlayConstActionProperty,
                m_ChildsProperty,
                m_PlayOptionProperty,
                
                m_MasterVolumeProperty,
                m_VolumeProperty,
                m_PitchProperty;

            public VisualElement m_VisualElement;

            public Data(SerializedProperty property) : base(s_Index++, 0)
            {
                m_Property = property;

                m_AudioClipProperty = property.FindPropertyRelative("m_AudioClip");

                m_PrefabProperty = property.FindPropertyRelative("m_Prefab");
                m_GroupProperty = property.FindPropertyRelative("m_Group");

                m_IgnoreTimeProperty = property.FindPropertyRelative("m_IgnoreTime");

                m_OnPlayConstActionProperty = property.FindPropertyRelative("m_OnPlayConstAction");
                m_ChildsProperty = property.FindPropertyRelative("m_Childs");
                m_PlayOptionProperty = property.FindPropertyRelative("m_PlayOption");

                m_MasterVolumeProperty = property.FindPropertyRelative("m_MasterVolume");
                m_VolumeProperty = property.FindPropertyRelative("m_Volume");
                m_PitchProperty = property.FindPropertyRelative("m_Pitch");

                VisualElement root = new VisualElement();
                root.style.flexGrow = 1;
                root.styleSheets.Add(CoreGUI.VisualElement.DefaultStyleSheet);
                root.AddToClassList("content-container");
                root.AddToClassList("inner-container");

                Label label = new Label(displayName);
                root.Add(label);

                foreach (var item in m_Property.ForEachChild())
                {
                    PropertyField field = new PropertyField(item);
                    root.Add(field);

                    field.BindProperty(item);
                }

                m_VisualElement = root;
            }

            public override string displayName => AudioClip != null ? AudioClip.name : "Unknown";

            public string AudioClipPath
            {
                get => SerializedPropertyHelper.GetAssetPathFieldPath(m_AudioClipProperty);
                set => SerializedPropertyHelper.SetAssetPathFieldPath(m_AudioClipProperty, value);
            }
            public AudioClip AudioClip
            {
                get => SerializedPropertyHelper.GetAssetPathField<AudioClip>(m_AudioClipProperty);
                set => SerializedPropertyHelper.SetAssetPathField(m_AudioClipProperty, value);
            }

            public AudioSource Prefab
            {
                get => SerializedPropertyHelper.GetAssetPathField<AudioSource>(m_PrefabProperty);
                set => SerializedPropertyHelper.SetAssetPathField(m_PrefabProperty, value);
            }
            public AudioMixerGroup Group
            {
                get => m_GroupProperty.objectReferenceValue as AudioMixerGroup;
                set => m_GroupProperty.objectReferenceValue = value;
            }

            public float IgnoreTime
            {
                get => m_IgnoreTimeProperty.floatValue;
                set => m_IgnoreTimeProperty.floatValue = value;
            }

            //public bool OnGUI()
            //{
            //    int index = m_Property.GetArrayIndex();

            //    CoreGUI.Line();
            //    using (new CoreGUI.BoxBlock(Color.white))
            //    {
            //        CoreGUI.Label(AudioClipPath.IsNullOrEmpty() ? "Unknown" : AudioClipPath, 14, TextAnchor.MiddleCenter);

            //        using (new EditorGUILayout.HorizontalScope())
            //        {
            //            if (GUILayout.Button("Copy"))
            //            {
            //                $"{index}".ToLog();


            //            }
            //            if (GUILayout.Button("Paste"))
            //            {

            //            }
            //        }

            //        EditorGUILayout.Space();

            //        foreach (var item in m_Property.ForEachChild())
            //        {
            //            EditorGUILayout.PropertyField(item);
            //        }
            //    }
            //    CoreGUI.Line();

            //    return true;
            //}

            public void Reset()
            {
                AudioClip = null;

                Prefab = null;
                Group = null;

                IgnoreTime = .2f;

                m_OnPlayConstActionProperty.ClearArray();
                m_ChildsProperty.ClearArray();
                m_PlayOptionProperty.enumValueIndex = (int)AudioPlayOption.Sequential;

                m_MasterVolumeProperty.floatValue = 1;
                SerializedPropertyHelper.SetMinMaxField(m_VolumeProperty, Vector2.one);
                SerializedPropertyHelper.SetMinMaxField(m_PitchProperty, Vector2.one);
            }
        }

        private sealed class FriendlyNameCollection : List<FriendlyName>
        {
            SerializedProperty m_Property;

            public FriendlyNameCollection(SerializedProperty property)
            {
                m_Property = property;

                for (int i = 0; i < m_Property.arraySize; i++)
                {
                    base.Add(new FriendlyName(m_Property.GetArrayElementAtIndex(i)));
                }
            }

            public FriendlyName Add()
            {
                m_Property.InsertArrayElementAtIndex(m_Property.arraySize);

                var temp = new FriendlyName(m_Property.GetArrayElementAtIndex(m_Property.arraySize - 1));
                temp.Name = string.Empty;
                temp.AudioClip = null;

                base.Add(temp);

                return temp;
            }
            public new bool Remove(FriendlyName item)
            {
                int index = base.IndexOf(item);
                if (index < 0) return false;

                RemoveAt(index);

                return true;
            }
            public new void RemoveAt(int index)
            {
                base.RemoveAt(index);
                m_Property.DeleteArrayElementAtIndex(index);
            }
        }

        private sealed class DataTreeView : TreeView
        {
            private SerializedProperty m_Property;
            private FriendlyNameCollection m_FriendlyNames;

            List<Data> m_Data;
            private SearchField m_SearchField;

            public event Action<IEnumerable<Data>> OnSelection;
            public IEnumerable<Data> CurrentSelection { get; private set; }

            public DataTreeView(SerializedProperty property, FriendlyNameCollection friendlyNames,
                TreeViewState state, List<Data> data) : base(state)
            {
                m_Property = property;
                m_FriendlyNames = friendlyNames;
                m_Data = data;
                m_SearchField = new SearchField();

                showAlternatingRowBackgrounds = true;
                showBorder = true;
            }
            protected override bool DoesItemMatchSearch(TreeViewItem item, string search)
            {
                Data data = (Data)item;
                if (!search.IsNullOrEmpty())
                {
                    if (!data.AudioClipPath.ToLower().Contains(search.ToLower())) return false;
                }

                return base.DoesItemMatchSearch(item, search);
            }
            protected override void SelectionChanged(IList<int> selectedIds)
            {
                CurrentSelection = FindRows(selectedIds).Select(t => (Data)t);
                
                OnSelection?.Invoke(CurrentSelection);
            }
            protected override void ContextClickedItem(int id)
            {
                Data data = (Data)FindItem(id, rootItem);
                GenericMenu ctx = new GenericMenu();

                ctx.AddDisabledItem(new GUIContent(data.displayName));
                ctx.AddSeparator(string.Empty);

                ctx.AddItem(new GUIContent("Remove"), false, () =>
                {
                    if (CurrentSelection != null &&
                        CurrentSelection.Contains(data))
                    {
                        SetSelection(Array.Empty<int>(), TreeViewSelectionOptions.FireSelectionChanged);
                    }

                    int index = m_Data.IndexOf(data);
                    m_Data.RemoveAt(index);
                    m_Property.DeleteArrayElementAtIndex(index);

                    Reload();
                });
                ctx.AddSeparator(string.Empty);

                ctx.AddItem(new GUIContent("Make Friendly Name"), false, () =>
                {
                    var item = m_FriendlyNames.Add();
                    item.Name = data.displayName;
                    item.AudioClip = data.AudioClip;
                });

                ctx.ShowAsContext();
            }

            public override void OnGUI(Rect rect)
            {
                AutoRect autoRect = new AutoRect(rect);

                Rect searchFieldRect = autoRect.Pop(rowHeight);

                searchString = m_SearchField.OnGUI(searchFieldRect, searchString);
                 
                Rect bttRowRect = autoRect.Pop(rowHeight);
                Rect[] bttRects = AutoRect.DivideWithRatio(bttRowRect, .5f, .5f);

                if (GUI.Button(bttRects[0], "+"))
                {
                    m_Property.InsertArrayElementAtIndex(m_Property.arraySize);

                    var temp = new Data(m_Property.GetArrayElementAtIndex(m_Property.arraySize - 1));
                    temp.Reset();
                    m_Data.Add(temp);

                    Reload();

                    m_Property.serializedObject.ApplyModifiedProperties();
                }
                if (GUI.Button(bttRects[1], "-"))
                {
                    if (CurrentSelection != null &&
                        CurrentSelection.Contains(m_Data[m_Data.Count - 1]))
                    {
                        SetSelection(Array.Empty<int>(), TreeViewSelectionOptions.FireSelectionChanged);
                    }

                    m_Data.RemoveAt(m_Property.arraySize - 1);
                    m_Property.DeleteArrayElementAtIndex(m_Property.arraySize - 1);

                    if (m_Data.Count > 0) Reload();

                    m_Property.serializedObject.ApplyModifiedProperties();
                }

                if (m_Data.Count == 0) return;

                base.OnGUI(autoRect.Current);
            }

            protected override TreeViewItem BuildRoot()
            {
                TreeViewItem root = new TreeViewItem(-1, -1);

                for (int i = 0; i < m_Data.Count; i++)
                {
                    root.AddChild(m_Data[i]);
                }

                SetupDepthsFromParentsAndChildren(root);

                return root;
            }
            protected override void RowGUI(RowGUIArgs args)
            {
                Color origin = GUI.color;

                Data data = args.item as Data;
                if (data.AudioClipPath.IsNullOrEmpty())
                {
                    GUI.color = new Color(1, 0, 0, .5f);
                }

                base.RowGUI(args);

                GUI.color = origin;
            }
        }

        #endregion

        private TreeViewState
            m_DataTreeViewState;
        private DataTreeView
            m_DataTreeView;

        private List<VisualElement> m_VisibleElements = new List<VisualElement>();

        private void OnEnable()
        {
            m_FriendlyNamesProperty = GetSerializedProperty("m_FriendlyNames");
            m_DataProperty = GetSerializedProperty("m_Data");

            //
            m_FriendlyNames = new FriendlyNameCollection(m_FriendlyNamesProperty);
            //
            m_Data = new List<Data>();
            for (int i = 0; i < m_DataProperty.arraySize; i++)
            {
                m_Data.Add(new Data(m_DataProperty.GetArrayElementAtIndex(i)));
            }

            m_DataTreeViewState = new TreeViewState();
            m_DataTreeView = new DataTreeView(m_DataProperty, m_FriendlyNames, m_DataTreeViewState, m_Data);
            
            if (m_Data.Count > 0) m_DataTreeView.Reload();
        }

        protected override bool ShouldHideOpenButton() => true;

        private static VisualElement FriendlyNameElementFactory(
            out TextField nameField, out AssetPathFieldView assetPathField)
        {
            VisualElement root = new VisualElement();
            root.style.flexGrow = 1;
            root.style.flexDirection = FlexDirection.Row;
            {
                nameField = new TextField();
                nameField.style.flexGrow = 1;
                nameField.style.maxWidth = new StyleLength(new Length(40, LengthUnit.Percent));
                root.Add(nameField);

                assetPathField = new AssetPathFieldView();
                assetPathField.objectType = TypeHelper.TypeOf<AudioClip>.Type;
                root.Add(assetPathField);
            }

            return root;
        }
        protected override VisualElement CreateVisualElement()
        {
            VisualElement root = new VisualElement();
            root.styleSheets.Add(CoreGUI.VisualElement.DefaultStyleSheet);

            Label header = new Label("Audio List");
            header.AddToClassList("header-label");

            VisualElement contentContainer = new VisualElement();
            {
                SearchableListContainerView friendlyNameList 
                    = new SearchableListContainerView(FriendlyName.Header.text);
                for (int i = 0; i < m_FriendlyNamesProperty.arraySize; i++)
                {
                    SerializedProperty elementProp = m_FriendlyNamesProperty.GetArrayElementAtIndex(i);

                    VisualElement elementRoot = new VisualElement();
                    elementRoot.style.flexGrow = 1;
                    elementRoot.style.flexDirection = FlexDirection.Row;
                    {
                        TextField nameField = new TextField();
                        nameField.style.flexGrow = 1;
                        nameField.style.maxWidth = new StyleLength(new Length(40, LengthUnit.Percent));
                        nameField.BindProperty(elementProp.FindPropertyRelative("m_FriendlyName"));
                        elementRoot.Add(nameField);

                        AssetPathFieldView assetPathFieldView = new AssetPathFieldView(
                            elementProp.FindPropertyRelative("m_AudioClip")
                            );
                        assetPathFieldView.objectType = TypeHelper.TypeOf<AudioClip>.Type;
                        elementRoot.Add(assetPathFieldView);
                    }
                    friendlyNameList.Add(elementRoot);
                }
                friendlyNameList.isExpanded = m_FriendlyNamesProperty.isExpanded;
                contentContainer.Add(friendlyNameList);

                friendlyNameList.onExpand += delegate (bool expand)
                {
                    m_FriendlyNamesProperty.isExpanded = expand;
                    m_FriendlyNamesProperty.serializedObject.ApplyModifiedProperties();
                };

                friendlyNameList.onAddButtonClicked += delegate(int index)
                {
                    m_FriendlyNamesProperty.InsertArrayElementAtIndex(index);
                    var prop = m_FriendlyNamesProperty.GetArrayElementAtIndex(index);
                    prop.SetDefaultValue();
                    m_FriendlyNamesProperty.serializedObject.ApplyModifiedProperties();

                    var ve = FriendlyNameElementFactory(out var nameField, out var assetPathField);
                    {
                        nameField.BindProperty(prop.FindPropertyRelative("m_FriendlyName"));
                        assetPathField.BindProperty(prop.FindPropertyRelative("m_AudioClip"));
                        assetPathField.label = String.Empty;
                    }

                    return ve;
                };
                friendlyNameList.onRemoveButtonClicked += delegate (int index)
                {
                    m_FriendlyNamesProperty.DeleteArrayElementAtIndex(index);
                    m_FriendlyNamesProperty.serializedObject.ApplyModifiedProperties();
                };

                VisualElement dataContainer = new VisualElement();
                dataContainer.AddToClassList("content-container");
                {
                    IMGUIContainer imgui = new IMGUIContainer(DataGUI);
                    dataContainer.Add(imgui);
                    m_DataTreeView.OnSelection += t =>
                    {
                        foreach (var item in m_VisibleElements)
                        {
                            item.RemoveFromHierarchy();
                        }
                        m_VisibleElements.Clear();

                        foreach (var item in t)
                        {
                            dataContainer.Add(item.m_VisualElement);

                            m_VisibleElements.Add(item.m_VisualElement);
                        }
                    };
                }
                contentContainer.Add(dataContainer);
            }
            root.Add(contentContainer);

            return root;
        }

        private void DataGUI()
        {
            m_DataProperty.isExpanded
                = CoreGUI.LabelToggle(m_DataProperty.isExpanded, Data.Header, 15, TextAnchor.MiddleCenter);

            if (!m_DataProperty.isExpanded) return;

            m_DataTreeView.OnGUI(GUILayoutUtility.GetRect(Screen.width, 200));
        }
    }
}

#endif