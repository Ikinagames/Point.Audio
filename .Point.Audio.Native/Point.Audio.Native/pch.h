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

// pch.h: This is a precompiled header file.
// Files listed below are compiled only once, improving build performance for future builds.
// This also affects IntelliSense performance, including code completion and many code browsing features.
// However, files listed here are ALL re-compiled if any one of them is updated between builds.
// Do not add files here that you will be updating frequently as this negates the performance advantage.

#ifndef PCH_H
#define PCH_H

// add headers that you want to pre-compile here
#include "framework.h"
#include "downsampler.h"
#include <stdlib.h>
#include <stdio.h>
#include <math.h>
#include <string.h>

#endif //PCH_H

#define _CRT_SECURE_NO_WARNINGS

#define DLLEXPORT extern "C" _declspec(dllexport)
#define TYPECAST(type, value) reinterpret_cast<type>(value)

#define FMOD_NOISE_RAMPCOUNT 256
#define GAIN_MIN -80.0f
#define GAIN_MAX 10.0f

#define DECIBELS_TO_LINEAR(__dbval__)  ((__dbval__ <= -80.0f) ? 0.0f : powf(10.0f, __dbval__ / 20.0f))
#define LINEAR_TO_DECIBELS(__linval__) ((__linval__ <= 0.0f) ? -80.0f : 20.0f * log10f((float)__linval__))