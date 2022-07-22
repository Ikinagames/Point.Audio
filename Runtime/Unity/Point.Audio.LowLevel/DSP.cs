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
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using static Unity.Mathematics.math;

namespace Point.Audio.LowLevel
{
    public struct DSP
    {
        public delegate float AudioSampleProcessDelegate(float value, AudioSample sample);

        public static class Function
        {
            public static float Multiply(float value, AudioSample sample) => value * sample.value;
        }

        public static AudioSample[] Evaluate(AudioClip clip, AudioSample[] samples)
        {
            int
                packSize = clip.channels,
                totalSamples = clip.samples * packSize;
            AudioSample[] resultSamples = new AudioSample[totalSamples];

            AudioSample prevSample = default;
            int currentTotalSamplePosition = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                for (int c = 0; c < packSize; c++)
                {
                    AudioSample sample = samples[i];
                    int offset = (sample.position * packSize) - (prevSample.position * packSize);
                    float startValue = prevSample.value;

                    //$"{c} {offset} :: {sample.position * packSize}, {totalSamples}".ToLog();
                    int x = 0;
                    for (int y = 0; y < offset; x++, y += packSize)
                    {
                        float ratio = y / (float)offset;
                        float target = (lerp(startValue, sample.value, ratio));
                        int position = (prevSample.position * packSize) + y + c;

                        AudioSample newSample = new AudioSample(prevSample.position + y, target);
                        resultSamples[position] = newSample;
                    }

                    //$"{currentSamplePosition} + {sample.position} - {prevSamples[c].position}".ToLog();
                    currentTotalSamplePosition += offset;
                    prevSample = sample;
                }
                
                currentTotalSamplePosition -= currentTotalSamplePosition % packSize;
            }

            //$"{currentSamplePosition}, {resultSamples.Length} => {totalSamples}".ToLog();
            //Assert.AreEqual(currentSamplePosition, resultSamples.Length,
            //    $"{packSize}, {currentSamplePosition} :: {totalSamples}\n" +
            //    $"${currentSamplePosition} != {resultSamples.Length}");

            for (int i = currentTotalSamplePosition; i < totalSamples; i += packSize)
            {
                for (int c = 0; c < packSize; c++)
                {
                    int targetSamplePosition = i + c;

                    float ratio = (targetSamplePosition - currentTotalSamplePosition) / (float)(totalSamples - currentTotalSamplePosition);
                    float target = (lerp(prevSample.value, 0, ratio));

                    AudioSample newSample = new AudioSample(
                                targetSamplePosition, target
                                );
                    resultSamples[newSample.position] = newSample;
                }
            }

            Assert.AreEqual(totalSamples, resultSamples.Length, 
                $"{clip.samples} :: {packSize} : {samples.Length}" +
                $"\n" +
                $"totalSample: {totalSamples}, result: {resultSamples.Length}");

            return resultSamples;
        }

        private static void ProcessData(float[] data, int channels, 
            AudioSample[] samples, int sampleChannels, int sampleStart, int channelLength,
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

        public static void Volume(float[] data, int channels, AudioSample[] samples, int sampleChannels, int sampleStart = 0)
        {
            ProcessData(data, channels, 
                samples, sampleChannels, sampleStart, data.Length,
                Function.Multiply);
        }
        public static void Volume(float[] data, int channels, AudioSample[] samples, int sampleChannels, int sampleStart, int length)
        {
            ProcessData(data, channels, 
                samples, sampleChannels, sampleStart, length,
                Function.Multiply);
        }
    }
}