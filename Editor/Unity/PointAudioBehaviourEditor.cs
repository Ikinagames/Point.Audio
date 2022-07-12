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

using Point.Collections.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Point.Audio.Editor
{
    [CustomEditor(typeof(PointAudioBehaviour))]
    internal sealed class PointAudioBehaviourEditor : InspectorEditorUXML<PointAudioBehaviour>
    {
        SerializedProperty m_ClipPathProperty;
        AudioClip m_TargetClip;
        Texture2D m_TargetClipTexture;

        AnimationToolbarView toolbarView;
        RulerView rulerView;
        private int samplePosition;

        float samplePerFrame => m_TargetClip.frequency / rulerView.frameRate;

        protected override VisualElement CreateVisualElement()
        {
            m_ClipPathProperty = serializedObject.FindProperty("m_Clip");

            m_TargetClip = SerializedPropertyHelper.GetAssetPathField<AudioClip>(m_ClipPathProperty);

            var root = base.CreateVisualElement();

            toolbarView = new AnimationToolbarView();
            rulerView = new RulerView();
            IMGUIContainer rulerViewGUI = new IMGUIContainer();
            rulerViewGUI.onGUIHandler += OnGUIControl;
            rulerView.Add(rulerViewGUI);
            if (m_TargetClip != null)
            {
                rulerView.stopTime = m_TargetClip.length;
                rulerView.rangeStartFrame = 0;
                rulerView.rangeStopFrame = rulerView.stopFrame;

                m_TargetClipTexture = m_TargetClip.PaintWaveformSpectrum(.5f, 500, 100, Color.gray);
            }

            toolbarView.OnFirstKeyButton += ToolbarView_OnFirstKeyButton;
            toolbarView.OnPrevKeyButton += ToolbarView_OnPrevKeyButton;
            toolbarView.OnPlayKeyButton += ToolbarView_OnPlayKeyButton;
            toolbarView.OnNextKeyButton += ToolbarView_OnNextKeyButton;
            toolbarView.OnLastKeyButton += ToolbarView_OnLastKeyButton;

            toolbarView.OnFrameChanged += ToolbarView_OnFrameChanged;

            root.Add(toolbarView);
            root.Add(rulerView);

            VisualElement tex = new VisualElement();
            tex.style.width = 500;
            tex.style.height = 100;
            tex.style.backgroundImage = m_TargetClipTexture;
            root.Add(tex);

            return root;
        }

        private void ToolbarView_OnFirstKeyButton()
        {
            if (AudioUtilExt.IsPlaying)
            {
                AudioUtilExt.PausePreviewClip();
            }

            samplePosition = 0;
            rulerView.cursorTime = 0;
            toolbarView.SetFrameWithoutNotify(0);

            AudioUtilExt.SetPreviewClipSamplePosition(m_TargetClip, 0);
        }
        private void ToolbarView_OnLastKeyButton()
        {
            if (AudioUtilExt.IsPlaying)
            {
                AudioUtilExt.PausePreviewClip();
            }

            samplePosition = m_TargetClip.samples - 1;
            rulerView.cursorTime = m_TargetClip.length;
            toolbarView.SetFrameWithoutNotify(rulerView.cursorFrame);

            AudioUtilExt.SetPreviewClipSamplePosition(m_TargetClip, samplePosition);
        }
        private void ToolbarView_OnPrevKeyButton()
        {
            if (AudioUtilExt.IsPlaying)
            {
                AudioUtilExt.PausePreviewClip();
            }

            rulerView.cursorFrame--;
            toolbarView.SetFrameWithoutNotify(rulerView.cursorFrame);

            samplePosition = Mathf.RoundToInt(rulerView.cursorFrame * samplePerFrame);
            AudioUtilExt.SetPreviewClipSamplePosition(m_TargetClip, samplePosition);
        }
        private void ToolbarView_OnNextKeyButton()
        {
            if (AudioUtilExt.IsPlaying)
            {
                AudioUtilExt.PausePreviewClip();
            }

            rulerView.cursorFrame++;
            toolbarView.SetFrameWithoutNotify(rulerView.cursorFrame);

            samplePosition = Mathf.RoundToInt(rulerView.cursorFrame * samplePerFrame);
            AudioUtilExt.SetPreviewClipSamplePosition(m_TargetClip, samplePosition);
        }
        private void ToolbarView_OnPlayKeyButton()
        {
            if (m_TargetClip == null) return;
            else if (AudioUtilExt.IsPaused)
            {
                AudioUtilExt.ResumePreviewClip();
                return;
            }
            else if (AudioUtilExt.IsPlaying)
            {
                AudioUtilExt.PausePreviewClip();
                return;
            }

            Debug.Log($"{samplePosition} >= {m_TargetClip.samples}");
            if (samplePosition >= m_TargetClip.samples)
            {
                samplePosition = 0;
                toolbarView.SetFrameWithoutNotify(0);
                rulerView.cursorFrame = 0;
            }

            AudioUtilExt.PlayPreviewClip(m_TargetClip, samplePosition, false);
        }

        private void ToolbarView_OnFrameChanged(ChangeEvent<float> evt)
        {
            if (m_TargetClip == null) return;

            rulerView.cursorFrame = evt.newValue;

            samplePosition = Mathf.RoundToInt(rulerView.cursorFrame * samplePerFrame);
            AudioUtilExt.SetPreviewClipSamplePosition(m_TargetClip, samplePosition);
        }

        private void OnGUIControl()
        {
            if (m_TargetClip == null || !AudioUtilExt.IsPlaying) return;

            samplePosition = AudioUtilExt.GetPreviewClipSamplePosition();
            float frame = samplePosition / samplePerFrame;
            toolbarView.SetFrameWithoutNotify(frame);
            rulerView.cursorFrame = frame;
        }
    }
}

#endif