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

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !POINT_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Point.Collections;
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using static Unity.Mathematics.math;

namespace Point.Audio.LowLevel
{
    public struct DSP
    {
        public delegate float AudioSampleProcessDelegate(float value, AudioSample sample);

        [BurstCompile(CompileSynchronously = true)]
        public static class Function
        {
            [BurstCompile]
            internal static unsafe void impl_evaluate(
                int* samples, int* channels,
                AudioSample* audioSamples, int* audioSampleLength,
                AudioSample* result)
            {
                int
                    totalSamples = *samples * *channels,
                    currentTotalSamplePosition = 0;

                AudioSample prevSample = default;
                for (int i = 0; i < *audioSampleLength; i++)
                {
                    for (int c = 0; c < *channels; c++)
                    {
                        AudioSample sample = audioSamples[i];
                        int offset = (sample.position * *channels) - (prevSample.position * *channels);
                        float startValue = prevSample.value;

                        int x = 0;
                        for (int y = 0; y < offset; x++, y += *channels)
                        {
                            float ratio = y / (float)offset;
                            float target = (lerp(startValue, sample.value, ratio));
                            int position = (prevSample.position * *channels) + y + c;

                            AudioSample newSample = new AudioSample(prevSample.position + y, target);
                            *(result + position) = newSample;
                        }
                        currentTotalSamplePosition += offset;
                        prevSample = sample;
                    }

                    currentTotalSamplePosition -= currentTotalSamplePosition % *channels;
                }

                for (int i = currentTotalSamplePosition; i < totalSamples; i += *channels)
                {
                    for (int c = 0; c < *channels; c++)
                    {
                        int targetSamplePosition = i + c;

                        float ratio = (targetSamplePosition - currentTotalSamplePosition) / (float)(totalSamples - currentTotalSamplePosition);
                        float target = (lerp(prevSample.value, 0, ratio));

                        AudioSample newSample = new AudioSample(
                                    targetSamplePosition, target
                                    );
                        *(result + newSample.position) = newSample;
                    }
                }
            }

            public static float Multiply(float value, AudioSample sample) => value * sample.value;
        }

        public static unsafe AudioSample[] Evaluate(AudioClip clip, AudioSample[] samples)
        {
            int
                clipSamples = clip.samples,
                channels = clip.channels;
            AudioSample[] resultSamples = new AudioSample[clipSamples * channels];

            fixed (AudioSample* samplesPtr = samples)
            fixed (AudioSample* result = resultSamples)
            {
                int sampleLength = samples.Length;
                
                Function.impl_evaluate(&clipSamples, &channels, samplesPtr, &sampleLength, result);
            }
            return resultSamples;
        }

        private static void ProcessData(float[] data, int channels, 
            AudioSample[] samples, int sampleChannels,
            // AudioSample array index for start.
            int sampleStart, 
            // each channel's length. same as AudioClip.samples.
            int channelLength,
            params AudioSampleProcessDelegate[] func)
        {
            channelLength *= channels;
            for (int i = 0, count = 0; i < channelLength; i += channels, count++)
            {
                int samplePosition = (sampleChannels * count) + sampleStart;
                int left = channels - 1;
                while (0 <= left)
                {
                    for (int j = sampleChannels - 1; j >= 0; j--, left--)
                    {
                        AudioSample sample = samples[samplePosition + j];

                        float result = data[i + left];

                        for (int xx = 0; xx < func.Length; xx++)
                        {
                            result = func[xx].Invoke(result, sample);
                        }

                        data[i + left] = result;
                    }
                }
            }
        }

        public static void Volume(float[] data, int channels, AudioSample[] samples, int sampleChannels, int sampleStart, int length)
        {
            ProcessData(data, channels, 
                samples, sampleChannels, sampleStart, length,
                Function.Multiply);
        }
    }
}