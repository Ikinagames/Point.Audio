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

#pragma once

#ifndef  __DOWNSAMPLER_H__
#define __DOWNSAMPLER_H__

#include <stdlib.h>

#include "pch.h"
#include "fmod.hpp"
#include "fmod_dsp.h"
#include "fmod_studio.hpp"

#endif // ! __DOWNSAMPLER_H__

FMOD_RESULT F_CALL DOWNSAMPLER_DSP_CREATE_CALLBACK(FMOD_DSP_STATE* dsp_state);
FMOD_RESULT F_CALL DOWNSAMPLER_DSP_RELEASE_CALLBACK(FMOD_DSP_STATE* dsp_state);
FMOD_RESULT F_CALL DOWNSAMPLER_DSP_RESET_CALLBACK(FMOD_DSP_STATE* dsp_state);
FMOD_RESULT F_CALL DOWNSAMPLER_DSP_READ_CALLBACK(FMOD_DSP_STATE* dsp_state, float* inbuffer, float* outbuffer, unsigned int length, int inchannels, int* outchannels);
FMOD_RESULT F_CALL DOWNSAMPLER_DSP_PROCESS_CALLBACK(FMOD_DSP_STATE* dsp_state, unsigned int length, const FMOD_DSP_BUFFER_ARRAY* inbufferarray, FMOD_DSP_BUFFER_ARRAY* outbufferarray, FMOD_BOOL inputsidle, FMOD_DSP_PROCESS_OPERATION op);
FMOD_RESULT F_CALL DOWNSAMPLER_DSP_SETPOSITION_CALLBACK(FMOD_DSP_STATE* dsp_state, unsigned int pos);

FMOD_RESULT F_CALL DOWNSAMPLER_DSP_SETPARAM_FLOAT_CALLBACK(FMOD_DSP_STATE* dsp_state, int index, float value);
FMOD_RESULT F_CALL DOWNSAMPLER_DSP_GETPARAM_FLOAT_CALLBACK(FMOD_DSP_STATE* dsp_state, int index, float* value, char* valuestr);
FMOD_RESULT F_CALL DOWNSAMPLER_DSP_SETPARAM_INT_CALLBACK(FMOD_DSP_STATE* dsp_state, int index, int value);
FMOD_RESULT F_CALL DOWNSAMPLER_DSP_GETPARAM_INT_CALLBACK(FMOD_DSP_STATE* dsp_state, int index, int* value, char* valuestr);

FMOD_DSP_DESCRIPTION* get_downsampler();

class Downsampler
{
public:
	Downsampler();
	~Downsampler();

	int getSampleCount();
	void setSampleCount(int);

	float getGain();
	void setGain(float);

	float getNoise();
	void setNoise(float);

	float getInputAmplitude();
	void setInputAmplitude(float);

	float getMix();
	void setMix(float);

	float processBufferValue(float element);

	void reset();
	void process(float* inbuffer, float* outbuffer, unsigned int length, int inchannels, int outchannels);

private:
	int current_sampleCount;
	float m_noiseamplitude;
	float m_inputamplitude;

	float m_mix;

	float m_target_gain;
	float m_current_gain;

	int m_ramp_samples_left;
};