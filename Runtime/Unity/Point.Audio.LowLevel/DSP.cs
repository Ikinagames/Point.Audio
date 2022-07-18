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
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using static Unity.Mathematics.math;

namespace Point.Audio.LowLevel
{
    public struct DSP
    {
        public static AudioSample[] Evaluate(AudioClip clip, AudioSample[] samples)
        {
            int
                packSize = clip.channels,
                totalSamples = clip.samples * packSize;
            List<AudioSample> resultSamples = new List<AudioSample>();

            AudioSample[] prevSamples = new AudioSample[packSize];
            int currentSamplePosition = 0;
            for (int i = 0; i < samples.Length; i += packSize)
            {
                for (int c = 0; c < packSize; c++)
                {
                    AudioSample sample = samples[i + c];
                    int offset = sample.position - prevSamples[c].position;
                    float startValue = prevSamples[c].value;

                    //$"{c} {offset}".ToLog();
                    for (int x = 0, y = 0; x < offset; x++, y += packSize)
                    {
                        float ratio = x / (float)offset;
                        float target = (lerp(startValue, sample.value, ratio));
                
                        AudioSample newSample = new AudioSample(
                            sample.position + y, target
                            );
                        resultSamples.Add(newSample);
                    }

                    //$"{currentSamplePosition} + {sample.position} - {prevSamples[c].position}".ToLog();
                    currentSamplePosition += offset;
                    prevSamples[c] = sample;
                }
                
                currentSamplePosition -= currentSamplePosition % packSize;
            }

            //$"{currentSamplePosition}, {resultSamples.Count} => {totalSamples}".ToLog();
            Assert.AreEqual(currentSamplePosition, resultSamples.Count,
                $"{packSize}, {currentSamplePosition} :: {totalSamples}\n" +
                $"${currentSamplePosition} != {resultSamples.Count}");

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
                    resultSamples.Add(newSample);
                }
            }

            Assert.AreEqual(totalSamples, resultSamples.Count, 
                $"{clip.samples} :: {packSize} : {samples.Length}" +
                $"\n" +
                $"totalSample: {totalSamples}, result: {resultSamples.Count}");

            return resultSamples.ToArray();
        }

        public static void Volume(float[] data, int channels, AudioSample[] samples, int sampleChannels)
        {
            //$"{data.Length} :: {samples.Length} , {channels} :: {sampleChannels}".ToLog();
            for (int i = 0, x = 0; i < data.Length; i += channels, x += sampleChannels)
            {
                for (int j = 0, y = 0; j < channels; j++, y = clamp(y + 1, 0, sampleChannels - 1))
                {
                    float target = data[i + j] * samples[x + y].value;
                    //data[i + j] = lerp(0, data[i + j], samples[x + y].value);

                    data[i + j] = target;
                }
            }
        }
    }
}