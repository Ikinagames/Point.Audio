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

#if UNITY_2020_1_OR_NEWER
#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !POINT_DISABLE_CHECKS
#define DEBUG_MODE
#endif
#define UNITYENGINE

using Point.Collections.Editor;
using UnityEditor;

namespace Point.Audio.FMODEditor
{
    [CustomEditor(typeof(FMODAnimationBinder), true)]
    internal sealed class FMODAnimationBinderEditor : InspectorEditorUXML<FMODAnimationBinder>
    {
        //private SerializedProperty 
        //    m_BindReferenceProperty, m_EventsProperty;
        //protected ExposedParameterFieldFactory exposedParameterFactory;

        //private void OnEnable()
        //{
        //    m_BindReferenceProperty = serializedObject.FindProperty("m_BindReference");
        //    m_EventsProperty = serializedObject.FindProperty("m_Events");

        //    //if (exposedParameterFactory == null && 
        //    //    m_BindReferenceProperty.objectReferenceValue != null)
        //    //{
        //    //    exposedParameterFactory = new ExposedParameterFieldFactory(m_BindReferenceProperty.objectReferenceValue as BaseGraph);
        //    //}
                
        //}
        //private void OnDisable()
        //{
        //    exposedParameterFactory?.Dispose();
        //    exposedParameterFactory = null;
        //}

        //protected override void OnInspectorGUIContents()
        //{
        //    EditorGUILayout.PropertyField(m_BindReferenceProperty);
        //    if (m_BindReferenceProperty.objectReferenceValue != null)
        //    {
        //        DrawExposedParameters(m_BindReferenceProperty);
        //    }

        //    EditorGUILayout.PropertyField(m_EventsProperty);
        //}
        private void DrawExposedParameters(SerializedProperty property)
        {
            FMODAnimationBindReference bindRef = property.objectReferenceValue as FMODAnimationBindReference;

            //if (bindRef.exposedParameters.Count == 0) return;

            //SerializedProperty parametersProp = property.FindPropertyRelative("exposedParameters");
            //for (int i = 0; i < bindRef.exposedParameters.Count; i++)
            //{
            //    ExposedParameter param = bindRef.exposedParameters[i];
            //    if (param.settings.isHidden) continue;

            //    EditorGUILayout.PropertyField(parametersProp.GetArrayElementAtIndex(i));
            //}
            //foreach (ExposedParameter param in graph.exposedParameters)
            //{
            //    if (param.settings.isHidden) continue;

                

            //    //var field = exposedParameterFactory.GetParameterValueField(param, (newValue) => {
            //    //    param.value = newValue;
            //    //    serializedObject.ApplyModifiedProperties();
            //    //    graph.NotifyExposedParameterValueChanged(param);
            //    //});
            //}
        }
    }
}

#endif