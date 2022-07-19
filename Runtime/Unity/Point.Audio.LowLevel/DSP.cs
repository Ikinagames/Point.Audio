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

            AudioSample[] prevSamples = new AudioSample[packSize];
            int currentSamplePosition = 0;
            for (int i = 0; i < samples.Length; i += packSize)
            {
                for (int c = 0; c < packSize; c++)
                {
                    AudioSample sample = samples[i + c];
                    int offset = (sample.position * packSize) - (prevSamples[c].position * packSize);
                    float startValue = prevSamples[c].value;

                    //$"{c} {offset} :: {sample.position * packSize}, {totalSamples}".ToLog();
                    int x = 0;
                    for (int y = 0; y < offset; x++, y += packSize)
                    {
                        float ratio = y / (float)offset;
                        float target = (lerp(startValue, sample.value, ratio));
                
                        AudioSample newSample = new AudioSample(
                            (prevSamples[c].position * packSize) + y, target
                            );
                        resultSamples[newSample.position] = newSample;
                    }

                    //$"{currentSamplePosition} + {sample.position} - {prevSamples[c].position}".ToLog();
                    currentSamplePosition += x;
                    prevSamples[c] = sample;
                }
                
                currentSamplePosition -= currentSamplePosition % packSize;
            }

            //$"{currentSamplePosition}, {resultSamples.Length} => {totalSamples}".ToLog();
            //Assert.AreEqual(currentSamplePosition, resultSamples.Length,
            //    $"{packSize}, {currentSamplePosition} :: {totalSamples}\n" +
            //    $"${currentSamplePosition} != {resultSamples.Length}");

            for (int i = currentSamplePosition; i < totalSamples; i += packSize)
            {
                for (int c = 0; c < packSize; c++)
                {
                    int targetSamplePosition = i + c;

                    float ratio = (targetSamplePosition - currentSamplePosition) / (float)(totalSamples - currentSamplePosition);
                    float target = (lerp(prevSamples[c].value, 0, ratio));

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
            AudioSample[] samples, int sampleChannels, 
            params AudioSampleProcessDelegate[] func)
        {
            //$"{data.Length} :: {samples.Length} , {channels} :: {sampleChannels}".ToLog();

            for (int i = 0, count = 0; i < data.Length; i += channels, count++)
            {
                int samplePosition = sampleChannels * count;
                int left = channels;
                while (0 < left)
                {
                    for (int j = 0; j < sampleChannels; j++, left--)
                    {
                        AudioSample sample = samples[samplePosition + j];

                        float result = data[i + j];
                        for (int xx = 0; xx < func.Length; xx++)
                        {
                            result = func[xx].Invoke(result, sample);
                        }

                        data[i + j] = result;
                    }
                }
            }
        }

        public static void Volume(float[] data, int channels, AudioSample[] samples, int sampleChannels)
        {
            ProcessData(data, channels, samples, sampleChannels, Function.Multiply);
        }
    }
}