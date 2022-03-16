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

#include <stdlib.h>
#include <math.h>

#include "pch.h"
#include "doubler.h"
#include "fmod.hpp"
#include "fmod_dsp.h"
#include "fmod_studio.hpp"

static FMOD_DSP_PARAMETER_DESC p_doubler_lTime;
static FMOD_DSP_PARAMETER_DESC p_doubler_rTime;
static FMOD_DSP_PARAMETER_DESC p_doubler_mix;
static FMOD_DSP_PARAMETER_DESC p_doubler_gain;

enum
{
	DSP_PARAM_LTIME = 0,
	DSP_PARAM_RTIME,
	DSP_PARAM_MIX,
	DSP_PARAM_GAIN,

	DSP_PARAM_NUM_PARAMETERS
};

FMOD_DSP_PARAMETER_DESC* Doubler_ParameterList[DSP_PARAM_NUM_PARAMETERS] = {
	&p_doubler_lTime,
	&p_doubler_rTime,
	&p_doubler_mix,
	&p_doubler_gain,
};

FMOD_DSP_DESCRIPTION Point_Doubler_Desc = {
	FMOD_PLUGIN_SDK_VERSION,
	"Point Doubler",		//	name
	0x00010000,					//	plug-in version
	1,							//	number of input buffers to process
	1,							//	number of output buffers to process
	DOUBLER_DSP_CREATE_CALLBACK,		//	create callback
	DOUBLER_DSP_RELEASE_CALLBACK,		//	release callback
	DOUBLER_DSP_RESET_CALLBACK,			//
	0/*DSP_READ_CALLBACK*/,			//
	DOUBLER_DSP_PROCESS_CALLBACK,		//
	0/*DSP_SETPOSITION_CALLBACK*/,	//

	DSP_PARAM_NUM_PARAMETERS,
	Doubler_ParameterList,
	DOUBLER_DSP_SETPARAM_FLOAT_CALLBACK,
	DOUBLER_DSP_SETPARAM_INT_CALLBACK,
	0,
	0,
	DOUBLER_DSP_GETPARAM_FLOAT_CALLBACK,
	DOUBLER_DSP_GETPARAM_INT_CALLBACK,
	0,
	0
};

FMOD_DSP_DESCRIPTION* get_doubler() {
	FMOD_DSP_INIT_PARAMDESC_FLOAT(
		p_doubler_lTime, "Left Time", "ms", "",
		0, 500, 0
	);
	FMOD_DSP_INIT_PARAMDESC_FLOAT(
		p_doubler_rTime, "Right Time", "ms", "",
		50, 500, 0
	);
	FMOD_DSP_INIT_PARAMDESC_FLOAT(
		p_doubler_mix, "Mix", "", "",
		0, 1, .5f
	);
	FMOD_DSP_INIT_PARAMDESC_FLOAT(
		p_doubler_gain, "Gain", "dB", "Gain in dB. -80 to 10. Default = 0",
		GAIN_MIN, GAIN_MAX, 0
	);

	return &Point_Doubler_Desc;
}

/*																									*/

#pragma region Doubler Class

float Doubler::getGain()
{
	return m_target_gain;
}
void Doubler::setGain(float value)
{
	m_target_gain = DECIBELS_TO_LINEAR(value);
	m_ramp_samples_left = FMOD_NOISE_RAMPCOUNT;
}

float Doubler::getLeftTime() {
	return m_left_time;
}
void Doubler::setLeftTime(float value) {
	m_left_time = value;
}
float Doubler::getRightTime() {
	return m_right_time;
}
void Doubler::setRightTime(float value) {
	m_right_time = value;
}

float Doubler::getMix() {
	return m_mix;
}
void Doubler::setMix(float value) {
	m_mix = value;
}

void Doubler::reset()
{
	m_current_gain = m_target_gain;
	m_ramp_samples_left = 0;
}

void Doubler::process(float* inbuffer, float* outbuffer, unsigned int length, int inchannels, int outchannels)
{
	float gain = m_current_gain;
	unsigned int samples = length * inchannels;
	int targetChannel = inchannels > 1 ? 2 : 1;

	if (m_ramp_samples_left) {
		float target = m_target_gain;
		float delta = (target - gain) / m_ramp_samples_left;

		for (unsigned int i = 0; i < samples; i += inchannels)
		{
			if (m_ramp_samples_left--) {
				gain += delta;

				//outbuffer[i] = 

				for (unsigned int j = 0; j < targetChannel && i + j < samples; j++)
				{
					outbuffer[i + j] = inbuffer[i + j] * gain;
				}
			}
			else {
				gain = target;
				break;
			}
		}
	}

	for (unsigned int i = 0; i < samples; i += inchannels)
	{
		for (unsigned int j = 0; j < targetChannel && i + j < samples; j++)
		{
			outbuffer[i + j] = inbuffer[i + j] * gain;
		}
	}
	
	m_current_gain = gain;
}

#pragma endregion

/*																									*/

#pragma region Callbacks

#pragma region Inits

FMOD_RESULT F_CALL DOUBLER_DSP_CREATE_CALLBACK(FMOD_DSP_STATE* dsp_state)
{
	Doubler* data = (Doubler*)FMOD_DSP_ALLOC(dsp_state, sizeof(Doubler));
	data->Initialize(dsp_state);

	dsp_state->plugindata = data;
	if (!dsp_state->plugindata) {
		return FMOD_ERR_MEMORY;
	}

	return FMOD_OK;
}
FMOD_RESULT F_CALL DOUBLER_DSP_RELEASE_CALLBACK(FMOD_DSP_STATE* dsp_state)
{
	Doubler* state = (Doubler*)dsp_state->plugindata;
	state->Reserve(dsp_state);

	FMOD_DSP_FREE(dsp_state, state);
	return FMOD_OK;
}

FMOD_RESULT F_CALL DOUBLER_DSP_RESET_CALLBACK(FMOD_DSP_STATE* dsp_state)
{
	Doubler* state = (Doubler*)dsp_state->plugindata;

	state->reset();

	return FMOD_OK;
}

FMOD_RESULT F_CALL DOUBLER_DSP_READ_CALLBACK(FMOD_DSP_STATE* dsp_state, float* inbuffer, float* outbuffer, unsigned int length, int inchannels, int* outchannels)
{
	return FMOD_OK;
}

FMOD_RESULT F_CALL DOUBLER_DSP_SETPOSITION_CALLBACK(FMOD_DSP_STATE* dsp_state, unsigned int pos)
{
	return FMOD_OK;
}

#pragma endregion

/*																									*/

FMOD_RESULT F_CALL DOUBLER_DSP_PROCESS_CALLBACK(
	FMOD_DSP_STATE* dsp_state, unsigned int length,
	const FMOD_DSP_BUFFER_ARRAY* inbufferarray, FMOD_DSP_BUFFER_ARRAY* outbufferarray,
	FMOD_BOOL inputsidle, FMOD_DSP_PROCESS_OPERATION op)
{
	Doubler* state = (Doubler*)dsp_state->plugindata;

	if (op == FMOD_DSP_PROCESS_QUERY) {

		if (outbufferarray && inbufferarray)
		{
			outbufferarray[0].buffernumchannels[0] = inbufferarray[0].buffernumchannels[0];
			outbufferarray[0].speakermode = FMOD_SPEAKERMODE_STEREO;
		}

		if (inputsidle) {
			return FMOD_ERR_DSP_DONTPROCESS;
		}

		return FMOD_OK;
	}

	//if (inputsidle) {
	//	return FMOD_ERR_DSP_SILENCE;
	//}

	state->process(
		inbufferarray->buffers[0], outbufferarray->buffers[0],
		length,
		inbufferarray->buffernumchannels[0],
		outbufferarray->buffernumchannels[0]);

	return FMOD_OK;
}

/*																									*/

FMOD_RESULT F_CALL DOUBLER_DSP_SETPARAM_FLOAT_CALLBACK(FMOD_DSP_STATE* dsp_state, int index, float value)
{
	Doubler* state = (Doubler*)dsp_state->plugindata;

	switch (index)
	{
	case DSP_PARAM_LTIME:
		state->setLeftTime(value);
		break;
	case DSP_PARAM_RTIME:
		state->setRightTime(value);
		break;
	case DSP_PARAM_MIX:
		state->setMix(value);
		break;
	case DSP_PARAM_GAIN:
		state->setGain(value);
		break;
	default:
		break;
	}

	return FMOD_OK;
}
FMOD_RESULT F_CALL DOUBLER_DSP_GETPARAM_FLOAT_CALLBACK(FMOD_DSP_STATE* dsp_state, int index, float* value, char* valuestr)
{
	Doubler* state = (Doubler*)dsp_state->plugindata;

	switch (index)
	{
	case DSP_PARAM_LTIME:
		*value = state->getLeftTime();
		break;
	case DSP_PARAM_RTIME:
		*value = state->getRightTime();
		break;
	case DSP_PARAM_MIX:
		*value = state->getMix();
	case DSP_PARAM_GAIN:
		*value = state->getGain();
		break;
	default:
		break;
	}

	return FMOD_OK;
}
FMOD_RESULT F_CALL DOUBLER_DSP_SETPARAM_INT_CALLBACK(FMOD_DSP_STATE* dsp_state, int index, int value)
{
	Doubler* state = (Doubler*)dsp_state->plugindata;

	switch (index)
	{
	default:
		break;
	}

	return FMOD_OK;
}
FMOD_RESULT F_CALL DOUBLER_DSP_GETPARAM_INT_CALLBACK(FMOD_DSP_STATE* dsp_state, int index, int* value, char* valuestr)
{
	Doubler* state = (Doubler*)dsp_state->plugindata;

	switch (index)
	{
	default:
		break;
	}

	return FMOD_OK;
}

#pragma endregion