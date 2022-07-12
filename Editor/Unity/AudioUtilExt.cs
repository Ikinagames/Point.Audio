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
using System;
using System.Reflection;
using UnityEngine;

namespace Point.Audio.Editor
{
    // https://github.com/Unity-Technologies/UnityCsReference/blob/e740821767d2290238ea7954457333f06e952bad/Editor/Mono/Audio/Bindings/AudioUtil.bindings.cs
    internal static class AudioUtilExt
    {
        private static MethodInfo 
            s_PlayPreviewClipMethodInfo,
            s_PausePreviewClipMethodInfo,
            s_ResumePreviewClipMethodInfo,
            s_StopAllPreviewClipsMethodInfo,
            s_IsPreviewClipPlayingMethodInfo,

            s_GetPreviewClipSamplePositionMethodInfo,
            s_SetPreviewClipSamplePositionMethodInfo;

        private static Type s_InjectType;
        public static Type InjectType
        {
            get
            {
                if (s_InjectType == null)
                {
                    s_InjectType = Type.GetType("UnityEditor.AudioUtil, UnityEditor.dll", true);
                }
                return s_InjectType;
            }
        }

        public static AudioClip CurrentAudioClip { get; private set; }

        public static bool IsLoop { get; private set; } = false;
        public static bool IsPlaying
        {
            get
            {
                if (s_IsPreviewClipPlayingMethodInfo == null)
                {
                    s_IsPreviewClipPlayingMethodInfo
                        = InjectType.GetMethod("IsPreviewClipPlaying", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                }

                bool result = (bool)s_IsPreviewClipPlayingMethodInfo.Invoke(null, null);
                return result;
            }
        }
        public static bool IsPaused { get; private set; } = false;

        public static void PlayPreviewClip(AudioClip clip, int startSample = 0, bool loop = false)
        {
            if (s_PlayPreviewClipMethodInfo == null)
            {
                s_PlayPreviewClipMethodInfo 
                    = InjectType.GetMethod("PlayPreviewClip", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            }

            CurrentAudioClip = clip;
            s_PlayPreviewClipMethodInfo.Invoke(null,
                new object[] { clip, startSample, loop });

            IsLoop = loop;
            IsPaused = false;
        }
        public static void PausePreviewClip()
        {
            if (s_PausePreviewClipMethodInfo == null)
            {
                s_PausePreviewClipMethodInfo
                    = InjectType.GetMethod("PausePreviewClip", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            }

            s_PausePreviewClipMethodInfo.Invoke(null, null);

            IsPaused = true;
        }
        public static void ResumePreviewClip()
        {
            if (s_ResumePreviewClipMethodInfo == null)
            {
                s_ResumePreviewClipMethodInfo
                    = InjectType.GetMethod("ResumePreviewClip", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            }

            s_ResumePreviewClipMethodInfo.Invoke(null, null);

            IsPaused = false;
        }
        public static void StopAllPreviewClips()
        {
            if (s_StopAllPreviewClipsMethodInfo == null)
            {
                s_StopAllPreviewClipsMethodInfo
                    = InjectType.GetMethod("StopAllPreviewClips", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            }

            s_StopAllPreviewClipsMethodInfo.Invoke(null, null);
        }

        public static int GetPreviewClipSamplePosition()
        {
            if (s_GetPreviewClipSamplePositionMethodInfo == null)
            {
                s_GetPreviewClipSamplePositionMethodInfo
                    = InjectType.GetMethod("GetPreviewClipSamplePosition", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            }

            int result = (int)s_GetPreviewClipSamplePositionMethodInfo.Invoke(null, null);
            return result;
        }
        public static void SetPreviewClipSamplePosition(AudioClip clip, int samplePosition)
        {
            if (s_SetPreviewClipSamplePositionMethodInfo == null)
            {
                s_SetPreviewClipSamplePositionMethodInfo
                    = InjectType.GetMethod("SetPreviewClipSamplePosition", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
                    , null, new Type[] { typeof(AudioClip), typeof(int) }, null);
            }
            s_SetPreviewClipSamplePositionMethodInfo.Invoke(null, new object[] { clip, samplePosition });
        }

        // https://answers.unity.com/questions/1603418/how-to-create-waveform-texture-from-audioclip.html
        // https://forum.unity.com/threads/how-to-create-waveform-texture-from-audioclip.631480/
        // https://answers.unity.com/questions/699595/how-to-generate-waveform-from-audioclip.html
        public static Texture2D PaintWaveformSpectrum(this AudioClip audio, float saturation, int width, int height, Color col)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            // multiply it by channel to account of clips with multiple channel
            float[] samples = new float[audio.samples * audio.channels];
            float[] waveform = new float[width];
            audio.GetData(samples, 0);
            int packSize = (audio.samples / width) + 1;
            int s = 0;
            for (int i = 0; i < audio.channels * audio.samples; i += packSize)
            {
                waveform[s] = Mathf.Abs(samples[i]);
                s++;
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    tex.SetPixel(x, y, Color.black);
                }
            }

            for (int x = 0; x < waveform.Length; x++)
            {
                for (int y = 0; y <= waveform[x] * ((float)height * .75f); y++)
                {
                    tex.SetPixel(x, (height / 2) + y, col);
                    tex.SetPixel(x, (height / 2) - y, col);
                }
            }
            tex.Apply();

            return tex;
        }
    }
}

#endif