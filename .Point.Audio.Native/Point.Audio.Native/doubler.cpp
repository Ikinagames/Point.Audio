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
		0, 500, 50
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

void GetOutChannelCount(FMOD_DSP_STATE* dsp_state, unsigned int* channelcount) {
	FMOD_SPEAKERMODE in_speakermode, out_speakermode;
	FMOD_DSP_GETSPEAKERMODE(dsp_state, &in_speakermode, &out_speakermode);
	
	switch (out_speakermode)
	{
	case FMOD_SPEAKERMODE_MONO:
		*channelcount = 1;
		break;
	case FMOD_SPEAKERMODE_STEREO:
		*channelcount = 2;
		break;
	case FMOD_SPEAKERMODE_QUAD:
		*channelcount = 4;
		break;
	default:
		*channelcount = 2;
		break;
	}
}

void Doubler::Initialize(FMOD_DSP_STATE* dsp_state) {
	FMOD_DSP_GETSAMPLERATE(dsp_state, &m_samplerate);
	FMOD_SPEAKERMODE in_speakermode, out_speakermode;
	FMOD_DSP_GETSPEAKERMODE(dsp_state, &in_speakermode, &out_speakermode);
	GetOutChannelCount(dsp_state, &m_channel_count);

	m_time_parameter = (float*)FMOD_DSP_ALLOC(dsp_state, sizeof(float) * m_channel_count);

	size_t ptr_size = sizeof(float*) * m_channel_count;

	m_buffer_size = m_samplerate;

	m_buffer = (float**)FMOD_DSP_ALLOC(dsp_state, ptr_size);
	m_rd_ptr = (float**)FMOD_DSP_ALLOC(dsp_state, ptr_size);
	m_wr_ptr = (float**)FMOD_DSP_ALLOC(dsp_state, ptr_size);

	for (unsigned int channel = 0; channel < m_channel_count; channel++)
	{
		m_buffer[channel] = (float*)FMOD_DSP_ALLOC(dsp_state, sizeof(float) * m_buffer_size);

		m_rd_ptr[channel] = m_buffer[channel];
		
		int pos = m_time_parameter[channel] * m_samplerate;
		m_wr_ptr[channel] = m_rd_ptr[channel] + pos;
		if (m_buffer[channel] + m_buffer_size <= m_wr_ptr[channel]) {
			m_wr_ptr[channel] -= m_buffer_size;
		}
	}
}
void Doubler::Reserve(FMOD_DSP_STATE* dsp_state) {
	FMOD_DSP_FREE(dsp_state, m_time_parameter);

	for (unsigned int i = 0; i < m_channel_count; i++)
	{
		FMOD_DSP_FREE(dsp_state, m_buffer[i]);
	}

	FMOD_DSP_FREE(dsp_state, m_buffer);
	FMOD_DSP_FREE(dsp_state, m_rd_ptr);
	FMOD_DSP_FREE(dsp_state, m_wr_ptr);
}

void Doubler::rdPtrCheck() {
	for (unsigned int i = 0; i < m_channel_count; i++)
	{
		if (m_buffer[i] + m_buffer_size <= m_rd_ptr[i]) {
			m_rd_ptr[i] = m_buffer[i];
		}
	}
}
void Doubler::setBuffer(int channel, float value) {
	*m_wr_ptr[channel] = value;
	m_wr_ptr[channel]++;

	if (m_buffer[channel] + m_buffer_size <= m_wr_ptr[channel]) {
		m_wr_ptr[channel] = m_buffer[channel];
	}
}

float Doubler::getGain()
{
	return LINEAR_TO_DECIBELS(m_target_gain);
}
void Doubler::setGain(float value)
{
	m_target_gain = DECIBELS_TO_LINEAR(value);
	m_ramp_samples_left = FMOD_NOISE_RAMPCOUNT;
}

float Doubler::getTime(int channel) {
	return m_time_parameter[channel] * 1000;
}
void Doubler::setTime(int channel, float value) {
	m_time_parameter[channel] = value * .001f;

	int pos = m_time_parameter[channel] * m_samplerate;

	m_wr_ptr[channel] += pos;
	if (m_buffer[channel] + m_buffer_size <= m_wr_ptr[channel]) {
		m_wr_ptr[channel] -= m_buffer_size;
	}
}

float Doubler::getMix() {
	return m_mix;
}
void Doubler::setMix(float value) {
	m_mix = value;

	clear();
}

void Doubler::clear() {
	for (unsigned int channel = 0; channel < m_channel_count; channel++)
	{
		memset(m_buffer[channel], 0, sizeof(float) * m_buffer_size);

		m_rd_ptr[channel] = m_buffer[channel];

		int pos = m_time_parameter[channel] * m_samplerate;
		m_wr_ptr[channel] = m_rd_ptr[channel] + pos;
		if (m_buffer[channel] + m_buffer_size <= m_wr_ptr[channel]) {
			m_wr_ptr[channel] -= m_buffer_size;
		}
	}
}
void Doubler::reset()
{
	m_current_gain = m_target_gain;
	m_ramp_samples_left = 0;

	clear();
}

void Doubler::process(float* inbuffer, float* outbuffer, unsigned int length, int inchannels, int outchannels)
{
	float gain = m_current_gain;
	unsigned int samples = length * inchannels;

	if (m_ramp_samples_left) {
		float target = m_target_gain;
		float delta = (target - gain) / m_ramp_samples_left;

		for (unsigned int i = 0; i < samples; i += inchannels)
		{
			if (m_ramp_samples_left--) {
				gain += delta;

				for (unsigned int channel = 0; channel < inchannels; channel++)
				{
					setBuffer(channel, inbuffer[i + channel]);
					outbuffer[i + channel] = MIX(*m_rd_ptr[channel], inbuffer[i + channel], m_mix) * gain;

					m_rd_ptr[channel]++;
					if (m_buffer[channel] + m_buffer_size <= m_rd_ptr[channel]) {
						m_rd_ptr[channel] = m_buffer[channel];
					}
				}

				//rdPtrCheck();
			}
			else {
				gain = target;
				break;
			}
		}
	}

	for (unsigned int i = 0; i < samples; i += inchannels)
	{
		for (unsigned int channel = 0; channel < inchannels; channel++)
		{
			setBuffer(channel, inbuffer[i + channel]);
			outbuffer[i + channel] = MIX(*m_rd_ptr[channel], inbuffer[i + channel], m_mix) * gain;

			m_rd_ptr[channel]++;
			if (m_buffer[channel] + m_buffer_size <= m_rd_ptr[channel]) {
				m_rd_ptr[channel] = m_buffer[channel];
			}
		}

		//rdPtrCheck();
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
			outbufferarray[0].speakermode = inbufferarray[0].speakermode;
		}

		if (inputsidle) {
			state->clear();

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
		state->setTime(0, value);
		break;
	case DSP_PARAM_RTIME:
		state->setTime(1, value);
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
		*value = state->getTime(0);
		break;
	case DSP_PARAM_RTIME:
		*value = state->getTime(1);
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