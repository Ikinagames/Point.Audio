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
#include "downsampler.h"
#include "fmod.hpp"
#include "fmod_dsp.h"
#include "fmod_studio.hpp"

static FMOD_DSP_PARAMETER_DESC p_downsample_count;
static FMOD_DSP_PARAMETER_DESC p_downsample_gain;
static FMOD_DSP_PARAMETER_DESC p_downsample_input_amplitude;
static FMOD_DSP_PARAMETER_DESC p_downsample_mix;
static FMOD_DSP_PARAMETER_DESC p_downsample_noise;

enum
{
	DSP_PARAM_SAMPLECOUNT = 0,
	DSP_PARAM_NOISE,
	DSP_PARAM_INPUT_AMPLITUDE,
	DSP_PARAM_MIX,
	DSP_PARAM_GAIN,

	DSP_PARAM_NUM_PARAMETERS
};

#pragma region Downsampler Class

	Downsampler::Downsampler()
	{
	}
	Downsampler::~Downsampler()
	{
	}

	void Downsampler::reset() {
		m_current_gain = m_target_gain;
		m_ramp_samples_left = 0;
	}

	int Downsampler::getSampleCount() {
		return current_sampleCount;
	}
	void Downsampler::setSampleCount(int count) {
		current_sampleCount = count;
	}

	float Downsampler::getGain() {
		return m_target_gain;
	}
	void Downsampler::setGain(float level) {
		m_target_gain = DECIBELS_TO_LINEAR(level);
		m_ramp_samples_left = FMOD_NOISE_RAMPCOUNT;
	}

	float Downsampler::getNoise() {
		return m_noiseamplitude;
	}
	void Downsampler::setNoise(float value) {
		m_noiseamplitude = value;
	}

	float Downsampler::getInputAmplitude() {
		return m_inputamplitude;
	}
	void Downsampler::setInputAmplitude(float value) {
		m_inputamplitude = value;
	}

	float Downsampler::getMix() {
		return m_mix;
	}
	void Downsampler::setMix(float value) {
		m_mix = value;
	}

	float Downsampler::processBufferValue(float element) {
		float processed = element + (element * (MINUSONE_TO_ONE * m_noiseamplitude));
		processed = max(-1, min(1, processed));

		//float procMix;
		//if (0 < m_inputamplitude && fabsf(processed) < m_inputamplitude) {
		//	procMix = 0;
		//}
		//else procMix = (processed * .01f * m_mix);

		//float mix = procMix + (element * 0.01f * (100 - m_mix));

		//return mix * gain;

		return processed;
	}
	void Downsampler::process(float* inbuffer, float* outbuffer, unsigned int length, 
		int inchannels, int outchannels) {

		float gain = m_current_gain;
		unsigned int samples = length * inchannels;
		int normalizeCount = current_sampleCount * inchannels;

		if (m_ramp_samples_left) {
			float target = m_target_gain;
			float delta = (target - gain) / m_ramp_samples_left;

			for (unsigned int i = 0; i < samples; i += normalizeCount)
			{
				if (0 < m_ramp_samples_left) {

					gain += delta;

					float processed = processBufferValue(inbuffer[i]);

					outbuffer[i] = MIX(processed, inbuffer[i], m_mix) * gain;
					for (int j = 1; 
						j < normalizeCount && i + j < samples && 0 < m_ramp_samples_left; j++, 
						m_ramp_samples_left--)
					{
						//outbuffer[i + j] = processed;
						outbuffer[i + j] = MIX(processed, inbuffer[i + j], m_mix) * gain;
					}
				}
				else {
					gain = target;
					break;
				}
			}
		}

		for (unsigned int i = 0; i < samples; i += normalizeCount)
		{
			float processed = processBufferValue(inbuffer[i]);
			
			//outbuffer[i] = processed;
			outbuffer[i] = MIX(processed, inbuffer[i], m_mix) * gain;
			for (int j = 1; j < normalizeCount && i + j < samples; j++)
			{
				//outbuffer[i + j] = processed;
				outbuffer[i + j] = MIX(processed, inbuffer[i + j], m_mix) * gain;
			}
		}

		m_current_gain = gain;
	}

#pragma endregion

FMOD_DSP_PARAMETER_DESC* ParameterList[DSP_PARAM_NUM_PARAMETERS] = {
	&p_downsample_count,
	&p_downsample_noise,
	&p_downsample_input_amplitude,
	&p_downsample_mix,
	&p_downsample_gain,
};
FMOD_DSP_DESCRIPTION Point_Downsampler_Desc = {
	FMOD_PLUGIN_SDK_VERSION,
	"Point Downsampler",		//	name
	0x00010000,					//	plug-in version
	1,							//	number of input buffers to process
	1,							//	number of output buffers to process
	DSP_CREATE_CALLBACK,		//	create callback
	DSP_RELEASE_CALLBACK,		//	release callback
	DSP_RESET_CALLBACK,			//
	0/*DSP_READ_CALLBACK*/,			//
	DSP_PROCESS_CALLBACK,		//
	0/*DSP_SETPOSITION_CALLBACK*/,	//

	DSP_PARAM_NUM_PARAMETERS,	//	number of parameters
	ParameterList,				//
	DSP_SETPARAM_FLOAT_CALLBACK,
	DSP_SETPARAM_INT_CALLBACK,
	0,
	0,
	DSP_GETPARAM_FLOAT_CALLBACK,
	DSP_GETPARAM_INT_CALLBACK,
	0,
	0
};

FMOD_DSP_DESCRIPTION* get_downsampler() {
	FMOD_DSP_INIT_PARAMDESC_INT(
		p_downsample_count, "Sample Count", "Sample(s)", "Count for downsampling. 1 to 32. Default = 4", 
		1, 32, 4, false, 0);
	FMOD_DSP_INIT_PARAMDESC_FLOAT(
		p_downsample_noise, "Noise", "", "",
		0, 1, 0
		);
	FMOD_DSP_INIT_PARAMDESC_FLOAT(
		p_downsample_input_amplitude, "Gate", "", "",
		0, 1, .1f
		);
	FMOD_DSP_INIT_PARAMDESC_FLOAT(
		p_downsample_mix, "Mix", "", "",
		0, 1, .5f
		);
	FMOD_DSP_INIT_PARAMDESC_FLOAT(
		p_downsample_gain, "Gain", "dB", "Gain in dB. -80 to 10. Default = 0",
		GAIN_MIN, GAIN_MAX, 0
		);

	return &Point_Downsampler_Desc;
}

#pragma region Callbacks

	#pragma region Inits

	FMOD_RESULT F_CALL DSP_CREATE_CALLBACK(FMOD_DSP_STATE* dsp_state)
	{
		dsp_state->plugindata = (Downsampler*)FMOD_DSP_ALLOC(dsp_state, sizeof(Downsampler));
		if (!dsp_state->plugindata) {
			return FMOD_ERR_MEMORY;
		}

		return FMOD_OK;
	}
	FMOD_RESULT F_CALL DSP_RELEASE_CALLBACK(FMOD_DSP_STATE* dsp_state)
	{
		Downsampler* state = (Downsampler*)dsp_state->plugindata;
		FMOD_DSP_FREE(dsp_state, state);
		return FMOD_OK;
	}

	FMOD_RESULT F_CALL DSP_RESET_CALLBACK(FMOD_DSP_STATE* dsp_state)
	{
		Downsampler* state = (Downsampler*)dsp_state->plugindata;

		state->reset();

		return FMOD_OK;
	}

	FMOD_RESULT F_CALL DSP_READ_CALLBACK(FMOD_DSP_STATE* dsp_state, float* inbuffer, float* outbuffer, unsigned int length, int inchannels, int* outchannels)
	{
		return FMOD_OK;
	}

	FMOD_RESULT F_CALL DSP_SETPOSITION_CALLBACK(FMOD_DSP_STATE* dsp_state, unsigned int pos)
	{
		return FMOD_OK;
	}

	#pragma endregion

	FMOD_RESULT F_CALL DSP_PROCESS_CALLBACK(
		FMOD_DSP_STATE* dsp_state, unsigned int length, 
		const FMOD_DSP_BUFFER_ARRAY* inbufferarray, FMOD_DSP_BUFFER_ARRAY* outbufferarray, 
		FMOD_BOOL inputsidle, FMOD_DSP_PROCESS_OPERATION op)
	{
		Downsampler* state = (Downsampler*)dsp_state->plugindata;

		if (op == FMOD_DSP_PROCESS_QUERY) {

			if (outbufferarray && inbufferarray)
			{
				outbufferarray[0].buffernumchannels[0] = inbufferarray[0].buffernumchannels[0];
				outbufferarray[0].speakermode = inbufferarray[0].speakermode;
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

	FMOD_RESULT F_CALL DSP_SETPARAM_FLOAT_CALLBACK(FMOD_DSP_STATE* dsp_state, int index, float value)
	{
		Downsampler* state = (Downsampler*)dsp_state->plugindata;

		switch (index)
		{
		case DSP_PARAM_NOISE:
			state->setNoise(value);
			break;
		case DSP_PARAM_INPUT_AMPLITUDE:
			state->setInputAmplitude(value);
			break;
		case DSP_PARAM_MIX:
			state->setMix(value);
			break;
		case DSP_PARAM_GAIN:
			state->setGain(value);
			break;
		}

		return FMOD_OK;
	}
	FMOD_RESULT F_CALL DSP_GETPARAM_FLOAT_CALLBACK(FMOD_DSP_STATE* dsp_state, int index, float* value, char* valuestr)
	{
		Downsampler* state = (Downsampler*)dsp_state->plugindata;

		switch (index)
		{
		case DSP_PARAM_NOISE:
			*value = state->getNoise();

			break;
		case DSP_PARAM_INPUT_AMPLITUDE:
			*value = state->getInputAmplitude();

			break;
		case DSP_PARAM_MIX:
			*value = state->getMix();
			break;
		case DSP_PARAM_GAIN:
			*value = state->getGain();
			//if (valuestr) {
			//	sprintf(valuestr, "%.1f dB", state->getGain());
			//}

			break;
		}

		return FMOD_OK;
	}
	FMOD_RESULT F_CALL DSP_SETPARAM_INT_CALLBACK(FMOD_DSP_STATE* dsp_state, int index, int value)
	{
		Downsampler* state = (Downsampler*)dsp_state->plugindata;

		switch (index)
		{
		case DSP_PARAM_SAMPLECOUNT:

			state->setSampleCount(value);
			break;
		}

		return FMOD_OK;
	}
	FMOD_RESULT F_CALL DSP_GETPARAM_INT_CALLBACK(FMOD_DSP_STATE* dsp_state, int index, int* value, char* valuestr)
	{
		Downsampler* state = (Downsampler*)dsp_state->plugindata;

		switch (index)
		{
		case DSP_PARAM_SAMPLECOUNT:

			*value = state->getSampleCount();
			//if (valuestr) sprintf(valuestr, "%s", state->getSampleCount());

			break;
		}

		return FMOD_OK;
	}

#pragma endregion